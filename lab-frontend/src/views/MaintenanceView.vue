<template>
  <div class="maintenance-container">
    <div class="toolbar">
      <h2>Lịch sử Bảo trì & Hiệu chuẩn</h2>
      <a-button type="primary" v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)" @click="showAddModal">+ Tạo phiếu bảo trì</a-button>
    </div>
    
    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 'max-content' }">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'maintenanceDate'">
             {{ formatDate(record[column.key]) }}
          </template>
          <template v-if="column.key === 'cost'">
             {{ Number(record.cost || 0).toLocaleString('vi-VN') }} VNĐ
          </template>
          <template v-if="column.key === 'status'">
             <StatusBadge :status="record.status" />
          </template>
          <template v-if="column.key === 'action'">
            <a-space>
              <a-button
                v-if="statusMatches(record.status, STATUS.MAINTENANCE_IN_PROGRESS)"
                type="primary"
                size="small"
                @click="showCompleteModal(record)"
              >
                Hoàn tất
              </a-button>
              <a-button
                v-if="role === 'Admin' && statusMatches(record.status, STATUS.MAINTENANCE_COMPLETED)"
                type="link"
                danger
                size="small"
                @click="handleDelete(record.id)"
              >
                Xóa
              </a-button>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal v-model:open="isFormVisible" title="Thêm lịch sử bảo trì" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting" width="700px" wrapClassName="responsive-modal">
      <a-form layout="vertical">
        <a-row :gutter="16">
          <a-col :xs="24" :sm="12">
            <a-form-item label="Chọn thiết bị" required>
              <a-select v-model:value="formData.equipmentId" placeholder="-- Chọn thiết bị --">
                 <a-select-option v-for="eq in equipments" :key="eq.id" :value="eq.id">{{ eq.name }} - {{ eq.serial }}</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Ngày thực hiện" required>
              <a-date-picker v-model:value="formData.maintenanceDate" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Người thực hiện" required>
              <a-input v-model:value="formData.performedBy" placeholder="Nhân viên / Kỹ thuật viên..." />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Chi phí (VNĐ)" required>
              <a-input-number v-model:value="formData.cost" style="width: 100%" :min="0" />
            </a-form-item>
          </a-col>
          <a-col :span="24">
            <a-form-item label="Nội dung bảo trì" required>
              <a-textarea v-model:value="formData.description" :rows="3" placeholder="VD: Thay dầu, lau ống kính..." />
            </a-form-item>
          </a-col>
        </a-row>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="isCompleteVisible"
      title="Hoàn tất bảo trì"
      okText="Xác nhận hoàn tất"
      cancelText="Hủy"
      :confirmLoading="completing"
      @ok="submitComplete"
    >
      <a-form layout="vertical">
        <a-form-item label="Kết quả bảo trì" required>
          <a-textarea
            v-model:value="completeResult"
            :rows="4"
            placeholder="Mô tả kết quả sửa chữa, hiệu chuẩn hoặc linh kiện đã thay..."
          />
        </a-form-item>
        <a-form-item label="Trạng thái thiết bị sau bảo trì" required>
          <a-select v-model:value="completeStatus">
            <a-select-option :value="STATUS.AVAILABLE">Hoạt động bình thường — Rảnh</a-select-option>
            <a-select-option :value="STATUS.BROKEN">Chưa sửa được — Hỏng</a-select-option>
            <a-select-option :value="STATUS.UNDER_WARRANTY">Gửi hãng — Bảo hành</a-select-option>
            <a-select-option :value="STATUS.MAINTENANCE_IN_PROGRESS">Cần xử lý tiếp — Đang bảo trì</a-select-option>
          </a-select>
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { useAuthStore } from '../stores/authStore'
import { maintenanceApi } from '../api/maintenanceApi'
import { equipmentApi } from '../api/equipmentApi'
import StatusBadge from '../components/StatusBadge.vue'
import { STATUS, statusMatches } from '../constants/business'

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const equipments = ref([])
const loading = ref(false)
const submitting = ref(false)
const isFormVisible = ref(false)
const isCompleteVisible = ref(false)
const completing = ref(false)
const completingRecordId = ref(null)
const completeResult = ref('')
const completeStatus = ref(STATUS.AVAILABLE)

const formData = ref({
  equipmentId: null,
  maintenanceDate: null,
  description: '',
  performedBy: '',
  cost: 0
})

const columns = [
  { title: 'Thiết bị', dataIndex: 'device', key: 'device' },
  { title: 'Ngày thực hiện', dataIndex: 'maintenanceDate', key: 'maintenanceDate' },
  { title: 'Nội dung', dataIndex: 'description', key: 'description' },
  { title: 'Người thực hiện', dataIndex: 'performedBy', key: 'performedBy' },
  { title: 'Chi phí', dataIndex: 'cost', key: 'cost' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
  { title: 'Kết quả', dataIndex: 'result', key: 'result' },
  { title: 'Hành động', key: 'action', align: 'center' }
]

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'

onMounted(() => {
  fetchData()
})

const fetchData = async () => {
  loading.value = true
  try {
    const res = await maintenanceApi.getAll()
    dataSource.value = res || []
  } catch (error) {
    message.error('Lỗi tải lịch sử bảo trì!')
  } finally {
    loading.value = false
  }
}

const showAddModal = async () => {
  try {
    const res = await equipmentApi.getAll()
    equipments.value = res || []
    formData.value = { equipmentId: null, maintenanceDate: null, description: '', performedBy: '', cost: 0 }
    isFormVisible.value = true
  } catch (err) {
    message.error('Không tải được danh sách thiết bị')
  }
}

const submitForm = async () => {
  if (
    !formData.value.equipmentId ||
    !formData.value.maintenanceDate ||
    !formData.value.description?.trim() ||
    !formData.value.performedBy?.trim() ||
    formData.value.cost === null ||
    formData.value.cost === undefined
  ) {
    message.warning('Vui lòng điền đủ thông tin!')
    return
  }
  submitting.value = true
  try {
    await maintenanceApi.create(formData.value)
    message.success('Đã lưu lịch sử bảo trì!')
    isFormVisible.value = false
    fetchData()
  } catch (error) {
    message.error('Có lỗi xảy ra!')
  } finally {
    submitting.value = false
  }
}

const handleDelete = (id) => {
  Modal.confirm({
    title: 'Xóa lịch sử',
    content: 'Bạn có chắc chắn muốn xóa lịch sử này không?',
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await maintenanceApi.delete(id)
        message.success('Đã xóa thành công!')
        fetchData()
      } catch (error) {
        message.error('Lỗi khi xóa!')
      }
    }
  })
}

const showCompleteModal = (record) => {
  completingRecordId.value = record.id
  completeResult.value = ''
  completeStatus.value = STATUS.AVAILABLE
  isCompleteVisible.value = true
}

const submitComplete = async () => {
  if (!completeResult.value.trim()) {
    message.warning('Vui lòng nhập kết quả bảo trì!')
    return
  }

  completing.value = true
  try {
    await maintenanceApi.complete(completingRecordId.value, {
      result: completeResult.value.trim(),
      nextEquipmentStatus: completeStatus.value
    })
    message.success('Đã hoàn tất phiếu bảo trì và cập nhật trạng thái thiết bị!')
    isCompleteVisible.value = false
    fetchData()
  } catch (error) {
    message.error(error?.response?.data?.message || 'Không thể hoàn tất bảo trì!')
  } finally {
    completing.value = false
  }
}
</script>

<style scoped>
.maintenance-container {
  padding: 0;
}
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}
h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}
</style>


