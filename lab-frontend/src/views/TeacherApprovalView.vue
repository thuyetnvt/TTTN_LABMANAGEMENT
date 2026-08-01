<template>
  <div class="teacher-approval-container">
    <div class="toolbar">
      <h2>Duyệt bảo lãnh mượn thiết bị</h2>
      <p>Danh sách yêu cầu sinh viên nhờ giảng viên bảo lãnh trước khi gửi lên kho.</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag color="purple">{{ record.status }}</a-tag>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-button type="primary" size="small" @click="handleApprove(record)">Bảo lãnh</a-button>
              <a-button type="primary" danger size="small" @click="handleReject(record)">Từ chối</a-button>
            </a-space>
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

const dataSource = ref([])
const loading = ref(false)

const columns = [
  { title: 'Sinh viên', dataIndex: 'student', key: 'student' },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device' },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate' },
  { title: 'Dự kiến trả', dataIndex: 'returnDate', key: 'returnDate' },
  { title: 'Mục đích', dataIndex: 'purpose', key: 'purpose' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center' },
  { title: 'Hành động', key: 'action', align: 'center' }
]

onMounted(() => fetchRequests())

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'

const fetchRequests = async () => {
  loading.value = true
  try {
    dataSource.value = await borrowApi.getTeacherPending() || []
  } catch {
    message.error('Lỗi khi tải danh sách yêu cầu bảo lãnh!')
  } finally {
    loading.value = false
  }
}

const handleApprove = async (record) => {
  try {
    await borrowApi.teacherApprove(record.id)
    message.success(`Đã bảo lãnh cho sinh viên ${record.student}. Đơn đã chuyển lên kho.`)
    fetchRequests()
  } catch {
    message.error('Lỗi duyệt yêu cầu!')
  }
}

const handleReject = async (record) => {
  try {
    await borrowApi.teacherReject(record.id)
    message.warning(`Đã từ chối bảo lãnh yêu cầu của sinh viên ${record.student}.`)
    fetchRequests()
  } catch {
    message.error('Lỗi từ chối yêu cầu!')
  }
}
</script>

<style scoped>
.teacher-approval-container {
  padding: 0;
}

.toolbar h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}

.toolbar p {
  color: #6b7280;
  margin-bottom: 24px;
}
</style>
