<template>
  <div class="asset-requests-container">
    <div class="toolbar">
      <h2>{{ isManager ? 'Duyệt cấp phát vật tư' : 'Yêu cầu cấp phát vật tư của tôi' }}</h2>
      <p>{{ isManager ? 'Quản lý các yêu cầu cấp phát vật tư từ người dùng.' : 'Theo dõi trạng thái các yêu cầu vật tư đã gửi.' }}</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <StatusBadge :status="record.status" />
          </template>
          <template v-else-if="column.key === 'requestDate'">
            {{ new Date(record.requestDate).toLocaleString('vi-VN') }}
          </template>
          <template v-else-if="column.key === 'action'">
            <div v-if="statusMatches(record.status, STATUS.CONSUMABLE_PENDING) && isManagerRole(role)">
              <a-button type="primary" size="small" style="margin-right: 8px;" @click="handleApprove(record.id)">Duyệt & cấp</a-button>
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

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))

const dataSource = ref([])
const loading = ref(false)

const columns = [
  { title: 'Tên vật tư', dataIndex: 'consumableName', key: 'consumableName' },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName' },
  { title: 'Người yêu cầu', dataIndex: 'username', key: 'username' },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'center' },
  { title: 'Lý do', dataIndex: 'reason', key: 'reason' },
  { title: 'Trạng thái', key: 'status', align: 'center' },
  { title: 'Ngày gửi', dataIndex: 'requestDate', key: 'requestDate' },
  { title: 'Hành động', key: 'action', align: 'center' }
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
    message.error(error?.response?.data || 'Không đủ số lượng vật tư trong kho!')
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


