<template>
  <div class="borrow-history-container">
    <div class="toolbar">
      <h2>Lịch sử mượn/trả</h2>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1200 }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" />
          </template>
          <template v-else-if="column.key === 'compensationAmount'">
            {{ record.compensationAmount ? record.compensationAmount.toLocaleString('vi-VN') + ' VNĐ' : '' }}
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import StatusBadge from '../components/StatusBadge.vue'

const dataSource = ref([])
const loading = ref(false)

const columns = [
  { title: 'Người mượn', dataIndex: 'student', key: 'student', width: 130 },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', width: 160 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 130 },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', width: 120 },
  { title: 'Ngày trả/hạn trả', dataIndex: 'returnDate', key: 'returnDate', width: 130 },
  { title: 'Tình trạng trả', dataIndex: 'returnCondition', key: 'returnCondition', width: 130 },
  { title: 'Ghi chú kiểm tra', dataIndex: 'returnInspectionNote', key: 'returnInspectionNote', width: 200 },
  { title: 'Xử lý bảo hành', dataIndex: 'warrantyAction', key: 'warrantyAction', width: 180 },
  { title: 'Bồi thường', dataIndex: 'compensationAmount', key: 'compensationAmount', width: 130 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center', fixed: 'right', width: 140 }
]

onMounted(() => fetchHistory())

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'

const fetchHistory = async () => {
  loading.value = true
  try {
    dataSource.value = await borrowApi.getHistory() || []
  } catch {
    message.error('Lỗi khi tải lịch sử!')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.borrow-history-container {
  padding: 0;
}

.toolbar {
  margin-bottom: 24px;
}

h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}
</style>
