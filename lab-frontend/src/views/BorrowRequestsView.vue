<template>
  <div class="borrow-requests-container">
    <div class="toolbar">
      <h2>Duyệt yêu cầu mượn/trả</h2>
    </div>

    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1200 }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'requestDate' || column.key === 'returnDate'">
            {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'dueStatus'">
            <a-tag v-if="record.isOverdue" color="red">Quá hạn {{ Math.abs(record.daysUntilDue) }} ngày</a-tag>
            <a-tag v-else-if="record.status === 'Đang mượn' && record.daysUntilDue <= 2" color="orange">Sắp tới hạn</a-tag>
            <a-tag v-else color="green">Trong hạn</a-tag>
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="statusColor(record.status)">{{ record.status }}</a-tag>
          </template>
          <template v-else-if="column.key === 'details'">
            <div v-for="detail in record.details" :key="detail.id">
              {{ detail.equipmentName }} x{{ detail.quantity }}
            </div>
          </template>
          <template v-else-if="column.key === 'action'">
            <template v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)">
              <a-space>
                <template v-if="record.status === 'Chờ duyệt'">
                  <a-button type="primary" size="small" @click="handleApprove(record)">Duyệt</a-button>
                  <a-button type="primary" danger size="small" @click="handleReject(record)">Từ chối</a-button>
                </template>
                <template v-else-if="record.status === 'Đang mượn'">
                  <a-button type="default" size="small" @click="showReturnModal(record)">Kiểm tra trả</a-button>
                  <a-button type="primary" size="small" @click="handleRemind(record)">Nhắc trả</a-button>
                </template>
              </a-space>
            </template>
            <span v-else class="muted">Chỉ xem</span>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="isReturnModalVisible" title="Kiểm tra tài sản khi trả" @ok="submitReturnInspection" @cancel="isReturnModalVisible = false" okText="Lưu kiểm tra" cancelText="Hủy" :confirmLoading="returnSubmitting">
      <a-form layout="vertical">
        <a-form-item label="Tình trạng sau kiểm tra" required>
          <a-select v-model:value="returnForm.condition">
            <a-select-option value="Rảnh">Rảnh</a-select-option>
            <a-select-option value="Hỏng">Hỏng</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="Ghi chú kiểm tra">
          <a-textarea v-model:value="returnForm.note" :rows="3" placeholder="Mô tả tình trạng thực tế, lỗi phát hiện, phụ kiện thiếu..." />
        </a-form-item>
        <a-form-item v-if="returnForm.condition === 'Hỏng'">
          <a-alert
            type="info"
            show-icon
            message="Hệ thống tự kiểm tra hạn bảo hành"
            description="Còn bảo hành: chuyển bảo hành. Hết bảo hành: ghi nhận hỏng và bồi thường (nếu có)."
          />
        </a-form-item>
        <a-form-item v-if="returnForm.condition === 'Hỏng'" label="Số tiền bồi thường nếu hết bảo hành">
          <a-input-number v-model:value="returnForm.compensationAmount" style="width: 100%" :min="0" :step="10000" :formatter="value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')" :parser="value => value.replace(/\$\s?|(,*)/g, '')" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import { useAuthStore } from '../stores/authStore'

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const returnSubmitting = ref(false)
const isReturnModalVisible = ref(false)
const currentReturnRecord = ref(null)
const returnForm = ref({
  condition: 'Rảnh',
  note: '',
  compensationAmount: 0
})

const columns = [
  { title: 'Người mượn', dataIndex: 'student', key: 'student', width: 130 },
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', width: 160 },
  { title: 'Danh mục', dataIndex: 'category', key: 'category', width: 110 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 130 },
  { title: 'Chi tiết yêu cầu', key: 'details', width: 180 },
  { title: 'Ngày đăng ký', dataIndex: 'requestDate', key: 'requestDate', width: 120 },
  { title: 'Dự kiến trả', dataIndex: 'returnDate', key: 'returnDate', width: 120 },
  { title: 'Hạn trả', key: 'dueStatus', align: 'center', width: 130 },
  { title: 'Mục đích', dataIndex: 'purpose', key: 'purpose', width: 180 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', align: 'center', width: 120 },
  { title: 'Hành động', key: 'action', align: 'center', fixed: 'right', width: 190 }
]

onMounted(() => fetchRequests())

const statusColor = (status) => {
  if (status === 'Chờ duyệt') return 'orange'
  if (status === 'Đang mượn') return 'blue'
  if (status.includes('Hỏng')) return 'red'
  return 'green'
}

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'

const fetchRequests = async () => {
  loading.value = true
  try {
    dataSource.value = await borrowApi.getPendingRequests() || []
  } catch {
    message.error('Lỗi khi tải danh sách yêu cầu!')
  } finally {
    loading.value = false
  }
}

const handleApprove = async (record) => {
  try {
    await borrowApi.approve(record.id)
    message.success(`Đã duyệt cho ${record.student} mượn tài sản!`)
    fetchRequests()
  } catch (error) {
    message.error(error?.response?.data?.message || 'Lỗi duyệt yêu cầu!')
  }
}

const handleReject = async (record) => {
  try {
    await borrowApi.reject(record.id)
    message.warning(`Đã từ chối yêu cầu của ${record.student}.`)
    fetchRequests()
  } catch {
    message.error('Lỗi từ chối yêu cầu!')
  }
}

const showReturnModal = (record) => {
  currentReturnRecord.value = record
  returnForm.value = {
    condition: 'Rảnh',
    note: '',
    compensationAmount: 0
  }
  isReturnModalVisible.value = true
}

const submitReturnInspection = async () => {
  returnSubmitting.value = true
  try {
    await borrowApi.returnEquipment(currentReturnRecord.value.id, returnForm.value)
    message.success('Đã lưu kết quả kiểm tra và cập nhật trạng thái tài sản!')
    isReturnModalVisible.value = false
    fetchRequests()
  } catch {
    message.error('Lỗi khi lưu kết quả kiểm tra!')
  } finally {
    returnSubmitting.value = false
  }
}

const handleRemind = async (record) => {
  try {
    message.loading({ content: 'Đang gửi nhắc trả...', key: 'remind' })
    await borrowApi.remind(record.id)
    message.success({ content: 'Đã gửi email nhắc trả!', key: 'remind' })
  } catch (error) {
    message.error({ content: error.response?.data || 'Lỗi khi gửi email nhắc trả!', key: 'remind' })
  }
}
</script>

<style scoped>
.borrow-requests-container {
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

.muted {
  color: #9ca3af;
  font-size: 13px;
}
</style>



