<template>
  <div class="penalty-container">
    <div class="toolbar">
      <h2>Quản lý Đền bù & Phạt</h2>
      <p style="color: #6b7280; margin-bottom: 24px;">Danh sách các biên bản bồi thường liên quan đến người mượn và thiết bị.</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'amount'">
            <span style="color: #ef4444; font-weight: 600;">{{ record.amount.toLocaleString('vi-VN') }} ₫</span>
          </template>
          <template v-else-if="column.key === 'createdAt'">
            {{ new Date(record.createdAt).toLocaleDateString('vi-VN') }}
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="penalty" />
          </template>
          <template v-else-if="column.key === 'action'">
            <a-button v-if="statusMatches(record.status, STATUS.UNPAID) && isManagerRole(role)"
                      type="primary" size="small" @click="handlePay(record)">
              Xác nhận Thu tiền
            </a-button>
            <span v-else style="color: #9ca3af; font-size: 13px;">Không có</span>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { penaltyApi } from '../api/penaltyApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import { STATUS, isManagerRole, statusMatches } from '../constants/business'
import { createTablePagination } from '../utils/tablePagination'

const tablePagination = createTablePagination()

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)

const columns = [
  { title: 'Người bồi thường', dataIndex: 'username', key: 'username' },
  { title: 'Thiết bị', dataIndex: 'equipmentName', key: 'equipmentName' },
  { title: 'Lý do / Tình trạng', dataIndex: 'reason', key: 'reason' },
  { title: 'Số tiền phạt', dataIndex: 'amount', key: 'amount', align: 'right' },
  { title: 'Ngày lập', dataIndex: 'createdAt', key: 'createdAt', align: 'center' },
  { title: 'Trạng thái', key: 'status', align: 'center' },
  { title: 'Hành động', key: 'action', align: 'center' }
]

onMounted(() => {
  fetchPenalties()
})

const fetchPenalties = async () => {
  loading.value = true
  try {
    const res = await penaltyApi.getAll()
    dataSource.value = res.data || res || []
  } catch (error) {
    message.error('Lỗi khi tải danh sách đền bù!')
  } finally {
    loading.value = false
  }
}

const handlePay = (record) => {
  Modal.confirm({
    title: 'Xác nhận thu tiền',
    content: `${record.username} đã thanh toán số tiền bồi thường ${record.amount.toLocaleString('vi-VN')} ₫?`,
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
</style>


