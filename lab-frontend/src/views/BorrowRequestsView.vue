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
            <a-tag v-else-if="statusMatches(record.status, STATUS.BORROWED) && record.daysUntilDue <= 2" color="orange">Sắp tới hạn</a-tag>
            <a-tag v-else color="green">Trong hạn</a-tag>
          </template>
          <template v-else-if="column.key === 'status'">
            <StatusBadge :status="record.status" />
          </template>
          <template v-else-if="column.key === 'details'">
            <div v-for="detail in record.details" :key="detail.id">
              {{ detail.equipmentName }} x{{ detail.quantity }}
            </div>
          </template>
          <template v-else-if="column.key === 'action'">
            <template v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)">
              <a-space>
                <template v-if="statusMatches(record.status, STATUS.BORROW_PENDING)">
                  <a-button type="primary" size="small" @click="handleApprove(record)">Duyệt</a-button>
                  <a-button type="primary" danger size="small" @click="handleReject(record)">Từ chối</a-button>
                </template>
                <template v-else-if="statusMatches(record.status, STATUS.BORROWED)">
                  <a-button type="primary" ghost size="small" @click="showHandoverModal(record)">Bàn giao</a-button>
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
        <a-alert
          type="info"
          show-icon
          message="Kiểm tra theo từng tài sản"
          description="Có thể ghi nhận riêng tình trạng, ghi chú và bồi thường cho từng món trong phiếu."
          style="margin-bottom: 16px"
        />
        <a-card v-for="item in returnForm.items" :key="item.equipmentId" size="small" :title="`${item.equipmentName || 'Tài sản'} — ${item.serial || ''}`" style="margin-bottom: 12px">
          <a-form-item label="Tình trạng sau kiểm tra" required>
            <a-select v-model:value="item.condition">
              <a-select-option :value="STATUS.AVAILABLE">Rảnh</a-select-option>
              <a-select-option :value="STATUS.BROKEN">Hỏng</a-select-option>
            </a-select>
          </a-form-item>
          <a-form-item label="Ghi chú kiểm tra">
            <a-textarea v-model:value="item.note" :rows="2" placeholder="Mô tả lỗi, phụ kiện thiếu..." />
          </a-form-item>
          <a-form-item v-if="statusMatches(item.condition, STATUS.BROKEN)" label="Số tiền bồi thường nếu hết bảo hành">
            <a-input-number v-model:value="item.compensationAmount" style="width: 100%" :min="0" :step="10000" :formatter="value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')" :parser="value => value.replace(/\$\s?|(,*)/g, '')" />
          </a-form-item>
          <a-form-item label="Ảnh/file trước hoặc sau khi nhận trả">
            <a-upload :before-upload="file => selectReturnEvidence(item, file)" :show-upload-list="false" accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx"><a-button size="small">Chọn minh chứng</a-button></a-upload>
            <span v-if="item.returnEvidenceFile" class="muted">{{ item.returnEvidenceFile.name }}</span>
            <a-select v-if="item.returnEvidenceFile" v-model:value="item.returnEvidenceType" style="width: 100%; margin-top: 6px"><a-select-option value="PHOTO_BEFORE">Ảnh trước khi trả</a-select-option><a-select-option value="PHOTO_AFTER">Ảnh sau khi trả</a-select-option><a-select-option value="DOCUMENT">Biên bản</a-select-option><a-select-option value="SIGNATURE">Xác nhận điện tử</a-select-option></a-select>
          </a-form-item>
        </a-card>
      </a-form>
    </a-modal>

    <a-modal v-model:open="isHandoverModalVisible" title="Lập biên bản bàn giao" @ok="submitHandover" @cancel="isHandoverModalVisible = false" okText="Lưu biên bản" cancelText="Hủy" :confirmLoading="handoverSubmitting">
      <a-alert type="info" show-icon message="Ghi nhận đủ từng tài sản trước khi bàn giao" style="margin-bottom: 16px" />
      <a-card v-for="item in handoverForm.items" :key="item.equipmentId" size="small" :title="`${item.equipmentName} — ${item.serial}`" style="margin-bottom: 12px">
        <a-form-item label="Tình trạng" required><a-select v-model:value="item.condition"><a-select-option :value="STATUS.AVAILABLE">Tốt/Rảnh</a-select-option><a-select-option :value="STATUS.BROKEN">Hỏng</a-select-option></a-select></a-form-item>
        <a-form-item label="Phụ kiện bàn giao"><a-input v-model:value="item.accessories" placeholder="Ví dụ: nguồn, cáp USB, hộp..." /></a-form-item>
        <a-form-item label="Ghi chú"><a-textarea v-model:value="item.note" :rows="2" /></a-form-item>
      </a-card>
      <a-form-item label="Ghi chú biên bản"><a-textarea v-model:value="handoverForm.notes" :rows="3" /></a-form-item>
      <a-form-item label="File/ảnh minh chứng">
        <a-upload :before-upload="selectHandoverEvidence" :show-upload-list="false" accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx">
          <a-button>Chọn file minh chứng</a-button>
        </a-upload>
        <span v-if="handoverEvidenceFile" class="muted">{{ handoverEvidenceFile.name }}</span>
      </a-form-item>
      <a-form-item v-if="handoverEvidenceFile" label="Loại minh chứng">
        <a-select v-model:value="handoverEvidenceType"><a-select-option value="PHOTO">Ảnh</a-select-option><a-select-option value="DOCUMENT">Tài liệu</a-select-option><a-select-option value="SIGNATURE">Xác nhận điện tử</a-select-option></a-select>
      </a-form-item>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { message, Upload } from 'ant-design-vue'
import { borrowApi } from '../api/borrowApi'
import { useAuthStore } from '../stores/authStore'
import StatusBadge from '../components/StatusBadge.vue'
import { STATUS, statusMatches } from '../constants/business'
import { handoverApi } from '../api/handoverApi'

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const loading = ref(false)
const returnSubmitting = ref(false)
const isReturnModalVisible = ref(false)
const isHandoverModalVisible = ref(false)
const handoverSubmitting = ref(false)
const currentReturnRecord = ref(null)
const currentHandoverRecord = ref(null)
const handoverForm = ref({ notes: '', items: [] })
const handoverEvidenceFile = ref(null)
const handoverEvidenceType = ref('PHOTO')
const returnForm = ref({
  condition: STATUS.AVAILABLE,
  note: '',
  compensationAmount: 0,
  items: []
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
  const details = record.details?.length
    ? record.details
    : [{ equipmentId: record.equipmentId, equipmentName: record.device, serial: record.serial }]
  returnForm.value = {
    condition: STATUS.AVAILABLE,
    note: '',
    compensationAmount: 0,
    items: details.filter(item => !item.returnedAt).map(item => ({
      equipmentId: item.equipmentId,
      equipmentName: item.equipmentName,
      serial: item.serial,
      condition: STATUS.AVAILABLE,
      note: '',
      compensationAmount: 0
      ,returnEvidenceFile: null, returnEvidenceType: 'PHOTO_AFTER'
    }))
  }
  isReturnModalVisible.value = true
}

const submitReturnInspection = async () => {
  returnSubmitting.value = true
  try {
    if (!returnForm.value.items.length) {
      message.warning('Không còn tài sản chưa nhận trả trong phiếu này!')
      return
    }
    for (const item of returnForm.value.items) {
      if (item.returnEvidenceFile) {
        await borrowApi.uploadReturnEvidence(currentReturnRecord.value.id, item.returnEvidenceFile, item.returnEvidenceType, item.equipmentId)
      }
    }
    await borrowApi.returnEquipment(currentReturnRecord.value.id, {
      items: returnForm.value.items.map(item => ({
        equipmentId: item.equipmentId,
        condition: item.condition,
        note: item.note,
        compensationAmount: item.compensationAmount
      }))
    })
    message.success('Đã lưu kết quả kiểm tra và cập nhật trạng thái tài sản!')
    isReturnModalVisible.value = false
    fetchRequests()
  } catch {
    message.error('Lỗi khi lưu kết quả kiểm tra!')
  } finally {
    returnSubmitting.value = false
  }
}

const selectReturnEvidence = (item, file) => {
  const allowed = ['pdf', 'jpg', 'jpeg', 'png', 'webp', 'doc', 'docx']
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!allowed.includes(extension) || file.size > 10 * 1024 * 1024) {
    message.error('Minh chứng phải là PDF, Word hoặc ảnh và không quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  item.returnEvidenceFile = file
  return false
}

const showHandoverModal = record => {
  currentHandoverRecord.value = record
  handoverForm.value = {
    notes: '',
    items: (record.details || []).map(item => ({
      equipmentId: item.equipmentId,
      equipmentName: item.equipmentName,
      serial: item.serial || '',
      condition: STATUS.AVAILABLE,
      accessories: '',
      note: ''
    }))
  }
  handoverEvidenceFile.value = null
  handoverEvidenceType.value = 'PHOTO'
  isHandoverModalVisible.value = true
}

const selectHandoverEvidence = (file) => {
  const allowed = ['pdf', 'jpg', 'jpeg', 'png', 'webp', 'doc', 'docx']
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!allowed.includes(extension)) {
    message.error('Chỉ chấp nhận PDF, Word hoặc ảnh JPG/PNG/WEBP.')
    return Upload.LIST_IGNORE
  }
  if (file.size > 10 * 1024 * 1024) {
    message.error('File minh chứng không được vượt quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  handoverEvidenceFile.value = file
  return false
}

const submitHandover = async () => {
  handoverSubmitting.value = true
  try {
    await handoverApi.create({
      borrowRecordId: currentHandoverRecord.value.id,
      notes: handoverForm.value.notes,
      items: handoverForm.value.items.map(item => ({
        equipmentId: item.equipmentId,
        condition: item.condition,
        accessories: item.accessories,
        note: item.note
      }))
    })
    if (handoverEvidenceFile.value) {
      await handoverApi.uploadEvidence(
        currentHandoverRecord.value.id,
        handoverEvidenceFile.value,
        handoverEvidenceType.value
      )
    }
    message.success('Đã lập biên bản bàn giao.')
    isHandoverModalVisible.value = false
  } catch (error) {
    message.error(error.response?.data?.message || 'Không thể lập biên bản bàn giao!')
  } finally { handoverSubmitting.value = false }
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



