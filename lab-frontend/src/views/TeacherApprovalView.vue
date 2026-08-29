<template>
  <div class="teacher-approval-container">
    <div class="toolbar">
      <h2>Duyệt bảo lãnh mượn thiết bị</h2>
      <p>Danh sách yêu cầu sinh viên nhờ giảng viên bảo lãnh trước khi gửi lên kho.</p>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }" :pagination="tablePagination">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'device'">
            <div>{{ record.device }}</div>
            <div v-for="detail in record.details || []" :key="detail.equipmentId" class="detail-line">
              {{ detail.equipmentName }} — {{ detail.serial }}
            </div>
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" type="borrow" />
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space>
              <a-button type="primary" size="small" @click="openDecision(record, 'approve')">Bảo lãnh</a-button>
              <a-button type="primary" danger size="small" @click="openDecision(record, 'reject')">Từ chối</a-button>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>

  <a-modal
    v-model:open="decisionOpen"
    :title="decisionType === 'approve' ? 'Xác nhận bảo lãnh' : 'Từ chối bảo lãnh'"
    :confirm-loading="decisionLoading"
    ok-text="Xác nhận"
    cancel-text="Hủy"
    @ok="submitDecision"
  >
    <p v-if="selectedRecord">Yêu cầu của {{ selectedRecord.student }} — {{ selectedRecord.device }}</p>
    <a-form-item label="Ghi chú quyết định" required>
      <a-textarea v-model:value="decisionNote" :rows="4" placeholder="Nhập lý do hoặc ghi chú xử lý..." />
    </a-form-item>
  </a-modal>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import StatusBadge from '../components/StatusBadge.vue'
import { createTablePagination } from '../utils/tablePagination'

const tablePagination = createTablePagination()

const dataSource = ref([])
const loading = ref(false)
const decisionOpen = ref(false)
const decisionLoading = ref(false)
const decisionType = ref('approve')
const decisionNote = ref('')
const selectedRecord = ref(null)

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

const openDecision = (record, type) => {
  selectedRecord.value = record
  decisionType.value = type
  decisionNote.value = ''
  decisionOpen.value = true
}

const submitDecision = async () => {
  const note = decisionNote.value.trim()
  if (!note) {
    message.warning('Vui lòng nhập ghi chú quyết định!')
    return
  }

  decisionLoading.value = true
  try {
    const record = selectedRecord.value
    if (decisionType.value === 'approve') {
      await borrowApi.teacherApprove(record.id, note)
      message.success(`Đã bảo lãnh cho sinh viên ${record.student}. Đơn đã chuyển lên kho.`)
    } else {
      await borrowApi.teacherReject(record.id, note)
      message.warning(`Đã từ chối bảo lãnh yêu cầu của sinh viên ${record.student}.`)
    }
    decisionOpen.value = false
    fetchRequests()
  } catch {
    message.error(decisionType.value === 'approve' ? 'Lỗi duyệt yêu cầu!' : 'Lỗi từ chối yêu cầu!')
  } finally {
    decisionLoading.value = false
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

.detail-line {
  color: #6b7280;
  font-size: 12px;
}
</style>
