<template>
  <div class="penalty-container">
    <div class="toolbar">
      <h2>Quản lý Đền bù & Phạt</h2>
      <p style="color: #6b7280; margin-bottom: 24px;">Danh sách các biên bản bồi thường liên quan đến người mượn và thiết bị.</p>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Người dùng, thiết bị..." class="filter-search" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" class="status-filter" @change="applyFilters">
          <a-select-option value="">Tất cả</a-select-option>
          <a-select-option :value="STATUS.UNPAID">Chưa thanh toán</a-select-option>
          <a-select-option :value="STATUS.PAID">Đã thanh toán</a-select-option>
        </a-select>
      </div>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'amount'">
            <span style="color: #ef4444; font-weight: 600;">{{ record.amount.toLocaleString('vi-VN') }} ₫</span>
          </template>
          <template v-else-if="column.key === 'createdAt'">
            {{ formatVietnamDate(record.createdAt) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="penalty" />
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-button v-if="statusMatches(record.status, STATUS.UNPAID) && isManagerRole(role)" type="primary" size="small" @click="handlePay(record)">Xác nhận thu tiền</a-button>
              <a-tooltip title="Xem biên bản"><a-button type="text" class="view-action" aria-label="Xem biên bản bồi thường" @click="showDetails(record)"><template #icon><EyeOutlined /></template></a-button></a-tooltip>
            </a-space>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="dataSource" :loading="loading" :pagination="tablePagination" empty-description="Chưa có biên bản bồi thường" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-penalty-heading"><strong>{{ item.equipmentName }}</strong><StatusBadge :status="item.status" type="penalty" /></div>
          <div class="mobile-penalty-user">{{ penaltyUserLabel(item) }} · {{ formatVietnamDate(item.createdAt) }}</div>
          <p>{{ item.reason }}</p>
          <div class="mobile-penalty-amount">{{ item.amount.toLocaleString('vi-VN') }} ₫</div>
          <a-button v-if="statusMatches(item.status, STATUS.UNPAID) && isManagerRole(role)" type="primary" block @click="handlePay(item)">Xác nhận thu tiền</a-button>
          <a-button block @click="showDetails(item)"><EyeOutlined /> Xem biên bản</a-button>
        </template>
      </ResponsiveDataList>
    </a-card>
    <a-modal v-model:open="detailsVisible" title="Biên bản bồi thường" :footer="null" width="620px">
      <a-descriptions v-if="selectedPenalty" bordered :column="1" size="small">
        <a-descriptions-item label="Người bồi thường">{{ penaltyUserLabel(selectedPenalty) }}</a-descriptions-item>
        <a-descriptions-item label="Thiết bị">{{ selectedPenalty.equipmentName }}</a-descriptions-item>
        <a-descriptions-item label="Lý do / tình trạng">{{ selectedPenalty.reason }}</a-descriptions-item>
        <a-descriptions-item label="Số tiền">{{ selectedPenalty.amount.toLocaleString('vi-VN') }} ₫</a-descriptions-item>
        <a-descriptions-item label="Ngày lập">{{ formatVietnamDate(selectedPenalty.createdAt) }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái"><StatusBadge :status="selectedPenalty.status" type="penalty" /></a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted, computed } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { penaltyApi } from '../api/penaltyApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { formatVietnamDate } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const detailsVisible = ref(false)
const selectedPenalty = ref(null)
const penaltyUserLabel = record => record?.fullName?.trim() || record?.username || 'Không xác định'
const showDetails = record => {
  selectedPenalty.value = record
  detailsVisible.value = true
}

const columns = [
  { title: 'Người bồi thường', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Thiết bị', dataIndex: 'equipmentName', key: 'equipmentName' },
  { title: 'Lý do / Tình trạng', dataIndex: 'reason', key: 'reason' },
  { title: 'Số tiền phạt', dataIndex: 'amount', key: 'amount', align: 'right' },
  { title: 'Ngày lập', dataIndex: 'createdAt', key: 'createdAt', align: 'center' },
  { title: 'Trạng thái', key: 'status', align: 'center' },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 190, align: 'center' }
]

onMounted(() => {
  fetchPenalties()
})

const fetchPenalties = async () => {
  loading.value = true
  try {
    const res = await penaltyApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    dataSource.value = res.items || []
    tablePagination.total = res.total || 0
  } catch (error) {
    message.error('Lỗi khi tải danh sách đền bù!')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchPenalties()
}

const handleTableChange = pager => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchPenalties()
}

const handlePay = (record) => {
  Modal.confirm({
    title: 'Xác nhận thu tiền',
    content: `${penaltyUserLabel(record)} đã thanh toán số tiền bồi thường ${record.amount.toLocaleString('vi-VN')} ₫?`,
    okText: 'Xác nhận',
    onOk: async () => {
      try {
        await penaltyApi.pay(record.id)
        message.success('Đã xác nhận thanh toán!')
        fetchPenalties()
      } catch (error) {
        message.error('Lỗi khi cập nhật thanh toán!')
      }
    }
  })
}
</script>

<style scoped>
.penalty-container {
  padding: 0;
}
.toolbar h2 {
  margin: 0 0 8px 0;
  font-weight: 600;
  color: #1f1f1f;
}
.toolbar-filters { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 18px; }
.mobile-penalty-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-penalty-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-penalty-user { margin-top: 5px; color: var(--color-text-secondary); font-size: 12px; }
.mobile-penalty-amount { margin: 10px 0; color: #dc2626; font-size: 18px; font-weight: 700; }
.view-action { color: var(--color-primary); }
@media (max-width: 767px) { .desktop-table { display: none; } .toolbar-filters > * { width: 100% !important; } }
</style>


