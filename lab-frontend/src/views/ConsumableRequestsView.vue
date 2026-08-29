<template>
  <div class="asset-requests-container">
    <div class="toolbar">
      <h2>{{ isManager ? 'Duyệt cấp phát vật tư' : 'Yêu cầu cấp phát vật tư của tôi' }}</h2>
      <p>{{ isManager ? 'Quản lý các yêu cầu cấp phát vật tư từ người dùng.' : 'Theo dõi trạng thái các yêu cầu vật tư đã gửi.' }}</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1400 }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="consumable" />
          </template>
          <template v-else-if="column.key === 'requestDate'">
            {{ new Date(record.requestDate).toLocaleString('vi-VN') }}
          </template>
          <template v-else-if="column.key === 'action'">
            <div v-if="statusMatches(record.status, STATUS.CONSUMABLE_PENDING) && isManagerRole(role)" class="action-cell">
              <a-button type="primary" size="small" @click="handleApprove(record.id)">Duyệt & cấp</a-button>
              <a-button danger size="small" @click="handleReject(record.id)">Từ chối</a-button>
            </div>
            <span v-else class="muted">Không có hành động</span>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))

const dataSource = ref([])
const loading = ref(false)

const columns = [
  { title: 'Tên vật tư', dataIndex: 'consumableName', key: 'consumableName', fixed: 'left', width: 220 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 140 },
  { title: 'Người yêu cầu', dataIndex: 'username', key: 'username', width: 150 },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', width: 90, align: 'center' },
  { title: 'Lý do', dataIndex: 'reason', key: 'reason', width: 280 },
  { title: 'Trạng thái', key: 'status', width: 150, align: 'center' },
  { title: 'Ngày gửi', dataIndex: 'requestDate', key: 'requestDate', width: 170 },
  { title: 'Hành động', key: 'action', fixed: 'right', width: 190, align: 'center' }
]

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


