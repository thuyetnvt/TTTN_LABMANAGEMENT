<template>
  <div class="asset-requests-container">
    <div class="toolbar">
      <h2>{{ isManager ? 'Duyệt cấp phát vật tư' : 'Yêu cầu cấp phát vật tư của tôi' }}</h2>
      <p>{{ isManager ? 'Quản lý các yêu cầu cấp phát vật tư từ người dùng.' : 'Theo dõi trạng thái các yêu cầu vật tư đã gửi.' }}</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1400 }" :pagination="tablePagination">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="consumable" />
          </template>
          <template v-else-if="column.key === 'requestDate'">
            {{ formatRequestDate(record.requestDate) }}
          </template>
          <template v-else-if="column.key === 'action'">
            <div class="action-cell">
              <a-tooltip v-if="!statusMatches(record.status, STATUS.CONSUMABLE_PENDING)" title="Xem chi tiết">
                <a-button type="text" class="view-action-button" aria-label="Xem chi tiết yêu cầu" @click="showDetails(record)">
                  <template #icon><EyeOutlined /></template>
                </a-button>
              </a-tooltip>
              <template v-if="statusMatches(record.status, STATUS.CONSUMABLE_PENDING) && isManagerRole(role)">
                <a-button type="primary" size="small" @click="handleApprove(record.id)">Duyệt & cấp</a-button>
                <a-button danger size="small" @click="handleReject(record.id)">Từ chối</a-button>
              </template>
            </div>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết yêu cầu cấp phát" :footer="null" width="620px">
      <a-descriptions v-if="selectedRequest" bordered :column="1" size="small">
        <a-descriptions-item label="Tên vật tư">{{ selectedRequest.consumableName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Danh mục">{{ selectedRequest.categoryName || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Người yêu cầu">{{ selectedRequest.username || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Số lượng">{{ selectedRequest.quantity }}</a-descriptions-item>
        <a-descriptions-item label="Lý do">{{ selectedRequest.reason || '—' }}</a-descriptions-item>
        <a-descriptions-item label="Trạng thái">
          <StatusBadge :status="selectedRequest.status" type="consumable" />
        </a-descriptions-item>
        <a-descriptions-item label="Ngày gửi">{{ formatRequestDate(selectedRequest.requestDate) }}</a-descriptions-item>
      </a-descriptions>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { EyeOutlined } from '@ant-design/icons-vue'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination } from '../utils/tablePagination'

const tablePagination = createTablePagination()

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))

const dataSource = ref([])
const loading = ref(false)
const detailsVisible = ref(false)
const selectedRequest = ref(null)

const columns = [
  { title: 'Tên vật tư', dataIndex: 'consumableName', key: 'consumableName', fixed: 'left', width: 220 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 140 },
  { title: 'Người yêu cầu', dataIndex: 'username', key: 'username', width: 150 },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', width: 90, align: 'center' },
  { title: 'Lý do', dataIndex: 'reason', key: 'reason', width: 280 },
  { title: 'Trạng thái', key: 'status', width: 150, align: 'center' },
  { title: 'Ngày gửi', dataIndex: 'requestDate', key: 'requestDate', width: 170 },
  { title: 'Hành động', key: 'action', fixed: 'right', width: 220, align: 'center' }
]

const formatRequestDate = value => value ? new Date(value).toLocaleString('vi-VN') : '—'

const showDetails = record => {
  selectedRequest.value = record
  detailsVisible.value = true
}

onMounted(() => fetchData())

const fetchData = async () => {
  loading.value = true
  try {
    dataSource.value = await consumableRequestApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh sách yêu cầu!')
  } finally {
    loading.value = false
  }
}

const handleApprove = async (id) => {
  try {
    await consumableRequestApi.approve(id)
    message.success('Đã duyệt và trừ số lượng vật tư!')
    fetchData()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không đủ số lượng vật tư trong kho!'))
  }
}

const handleReject = async (id) => {
  try {
    await consumableRequestApi.reject(id)
    message.success('Đã từ chối yêu cầu!')
    fetchData()
  } catch {
    message.error('Lỗi khi từ chối!')
  }
}
</script>

<style scoped>
.asset-requests-container {
  padding: 0;
}

.action-cell {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  flex-wrap: nowrap;
  white-space: nowrap;
}

.view-action-button {
  color: var(--color-primary, #e27755);
}

.view-action-button:hover {
  background: rgba(226, 119, 85, 0.1);
}

.asset-requests-container :deep(.ant-table-cell-fix-right) {
  z-index: 2;
  background: #fff !important;
  box-shadow: -6px 0 12px -10px rgba(16, 35, 63, 0.55);
}

.asset-requests-container :deep(.ant-table-thead > tr > th.ant-table-cell-fix-right) {
  z-index: 3;
  background: #fafafa !important;
}



.toolbar h2 {
  margin: 0 0 8px 0;
  font-weight: 600;
  color: #1f1f1f;
}

.toolbar p,
.muted {
  color: #6b7280;
}
</style>


