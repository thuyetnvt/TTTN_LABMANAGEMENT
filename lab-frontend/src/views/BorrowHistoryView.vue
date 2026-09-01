<template>
  <div class="borrow-history-container">
    <div class="toolbar">
      <h2>Lịch sử mượn/trả</h2>
      <div class="toolbar-filters">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Người mượn, thiết bị..." style="width: 260px" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" style="width: 180px" @change="applyFilters">
          <a-select-option :value="STATUS.APPROVED">Chờ nhận</a-select-option>
          <a-select-option :value="STATUS.BORROWED">Đang mượn</a-select-option>
          <a-select-option :value="STATUS.RETURNED">Đã trả</a-select-option>
          <a-select-option :value="STATUS.REJECTED">Từ chối</a-select-option>
        </a-select>
      </div>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table class="desktop-table" :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1200 }" :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'returnCondition'">
            <StatusBadge v-if="record.returnCondition" :status="record.returnCondition" type="returnCondition" />
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" />
          </template>
          <template v-else-if="column.key === 'compensationAmount'">
            {{ record.compensationAmount ? record.compensationAmount.toLocaleString('vi-VN') + ' VNĐ' : '' }}
          </template>
          <template v-else-if="column.key === 'action'">
            <a-button
              v-if="record.canConfirmHandover"
              type="primary"
              size="small"
              @click="openHandover(record)"
            >
              Xem & xác nhận nhận
            </a-button>
            <span v-else-if="statusMatches(record.status, STATUS.APPROVED)" class="muted">
              {{ record.handover ? 'Chờ người nhận xác nhận' : 'Chờ lập biên bản' }}
            </span>
            <span v-else class="muted">—</span>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="dataSource" :loading="loading" :pagination="tablePagination" empty-description="Chưa có lịch sử mượn/trả" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-card-heading">
            <strong>{{ item.device }}</strong>
            <StatusBadge :status="item.status" type="borrow" />
          </div>
          <div class="mobile-card-subtitle">{{ item.student }} · {{ item.serial || 'Không có số seri' }}</div>
          <dl class="mobile-card-details">
            <div><dt>Ngày đăng ký</dt><dd>{{ formatDate(item.requestDate) }}</dd></div>
            <div><dt>Hạn/ngày trả</dt><dd>{{ formatDate(item.returnDate) }}</dd></div>
            <div v-if="item.returnCondition"><dt>Tình trạng trả</dt><dd><StatusBadge :status="item.returnCondition" type="returnCondition" /></dd></div>
            <div v-if="item.compensationAmount"><dt>Bồi thường</dt><dd>{{ item.compensationAmount.toLocaleString('vi-VN') }} VNĐ</dd></div>
          </dl>
          <a-button v-if="item.canConfirmHandover" type="primary" block @click="openHandover(item)">Xem & xác nhận nhận</a-button>
          <div v-else-if="statusMatches(item.status, STATUS.APPROVED)" class="mobile-card-note">
            {{ item.handover ? 'Chờ người nhận xác nhận' : 'Chờ lập biên bản' }}
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal
      v-model:open="isHandoverVisible"
      title="Kiểm tra biên bản bàn giao"
      :confirm-loading="confirming"
      ok-text="Xác nhận đã nhận đủ"
      cancel-text="Đóng"
      width="720px"
      @ok="confirmReceipt"
    >
      <a-spin :spinning="handoverLoading">
        <a-alert
          type="warning"
          show-icon
          message="Chỉ xác nhận sau khi đã nhận và kiểm tra thực tế"
          description="Khi xác nhận, phiếu sẽ chuyển sang Đang mượn và tài sản được ghi nhận đang do bạn quản lý."
          style="margin-bottom: 16px"
        />
        <a-descriptions v-if="selectedHandover" bordered size="small" :column="1">
          <a-descriptions-item label="Mã biên bản">{{ selectedHandover.code }}</a-descriptions-item>
          <a-descriptions-item label="Thời gian lập">{{ formatDateTime(selectedHandover.handoverAt) }}</a-descriptions-item>
          <a-descriptions-item label="Ghi chú">{{ selectedHandover.notes || 'Không có' }}</a-descriptions-item>
        </a-descriptions>
        <div v-if="selectedHandover?.items?.length" class="handover-items">
          <a-card v-for="item in selectedHandover.items" :key="item.equipmentId" size="small">
            <strong>{{ item.equipmentName }}</strong>
            <div>Số seri: {{ item.serial || '—' }}</div>
            <div>Tình trạng: <StatusBadge :status="item.condition" type="returnCondition" /></div>
            <div>Phụ kiện: {{ item.accessories || 'Không ghi nhận' }}</div>
            <div>Ghi chú: {{ item.note || 'Không có' }}</div>
          </a-card>
        </div>
      </a-spin>
    </a-modal>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import { handoverApi } from '../api/handoverApi'
import StatusBadge from '../components/StatusBadge.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { STATUS, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDate, formatVietnamDateTime } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const dataSource = ref([])
const loading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const isHandoverVisible = ref(false)
const handoverLoading = ref(false)
const confirming = ref(false)
const selectedRecord = ref(null)
const selectedHandover = ref(null)

const columns = [
  { title: 'Người mượn', dataIndex: 'student', key: 'student', fixed: 'left', width: 130 },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', fixed: 'left', width: 160 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 130 },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', width: 120 },
  { title: 'Ngày trả/hạn trả', dataIndex: 'returnDate', key: 'returnDate', width: 130 },
  { title: 'Tình trạng trả', dataIndex: 'returnCondition', key: 'returnCondition', width: 130 },
  { title: 'Ghi chú kiểm tra', dataIndex: 'returnInspectionNote', key: 'returnInspectionNote', width: 200 },
  { title: 'Xử lý bảo hành', dataIndex: 'warrantyAction', key: 'warrantyAction', width: 180 },
  { title: 'Bồi thường', dataIndex: 'compensationAmount', key: 'compensationAmount', width: 130 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center', width: 140 },
  { title: 'Hành động', key: 'action', align: 'center', fixed: 'right', width: 190 }
]

onMounted(() => fetchHistory())


const openHandover = async record => {
  selectedRecord.value = record
  selectedHandover.value = record.handover || null
  isHandoverVisible.value = true
  handoverLoading.value = true
  try {
    selectedHandover.value = await handoverApi.getByBorrowRecord(record.id)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể tải biên bản bàn giao.'))
    isHandoverVisible.value = false
  } finally {
    handoverLoading.value = false
  }
}

const confirmReceipt = async () => {
  if (!selectedRecord.value || handoverLoading.value) return
  confirming.value = true
  try {
    await handoverApi.confirmReceipt(selectedRecord.value.id)
    message.success('Đã xác nhận nhận tài sản. Phiếu đã chuyển sang đang mượn.')
    isHandoverVisible.value = false
    await fetchHistory()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể xác nhận nhận tài sản.'))
  } finally {
    confirming.value = false
  }
}

const fetchHistory = async () => {
  loading.value = true
  try {
    const response = await borrowApi.getHistoryPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    dataSource.value = response.items || []
    tablePagination.total = response.total || 0
  } catch {
    message.error('Lỗi khi tải lịch sử!')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchHistory()
}

const handleTableChange = (pager) => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchHistory()
}
</script>

<style scoped>
.borrow-history-container {
  padding: 0;
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 24px;
}

.toolbar-filters { display: flex; flex-wrap: wrap; gap: 10px; }

@media (max-width: 767px) {
  .toolbar { align-items: stretch; flex-direction: column; }
  .toolbar-filters > * { width: 100% !important; }
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.muted { color: #8c8c8c; font-size: 13px; }
.handover-items { display: grid; gap: 10px; margin-top: 16px; }
.handover-items :deep(.ant-card-body) { display: grid; gap: 6px; }
.mobile-card-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-card-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-card-subtitle, .mobile-card-note { margin-top: 6px; color: var(--color-text-secondary); font-size: 13px; }
.mobile-card-details { display: grid; gap: 7px; margin: 12px 0; }
.mobile-card-details div { display: flex; justify-content: space-between; gap: 12px; }
.mobile-card-details dt { color: var(--color-text-secondary); }
.mobile-card-details dd { margin: 0; text-align: right; }
@media (max-width: 767px) { .desktop-table { display: none; } }
</style>
