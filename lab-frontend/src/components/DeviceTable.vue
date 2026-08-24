<template>
  <div class="table-actions">
    <div class="left-actions">
      <a-button v-if="role === 'Sinh viên' || role === 'Giảng viên'" type="primary" @click="showScannerModal">
        Quét QR để mượn
      </a-button>
      <a-input
        v-model:value="searchQuery"
        placeholder="Tìm kiếm theo tên thiết bị..."
        style="width: 250px"
        @change="handleSearchChange"
      />
    </div>
    <div class="right-actions">
      <a-button v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)" type="primary" ghost @click="handleExport">
        Xuất Excel
      </a-button>
      <a-button v-if="['Admin', 'Trưởng lab', 'Phó lab'].includes(role)" type="primary" @click="showAddModal">
        + Thêm thiết bị
      </a-button>
    </div>
  </div>

  <a-table :dataSource="filteredDataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1500 }">
    <template #bodyCell="{ column, record }">
      <template v-if="column.key === 'status'">
        <StatusBadge :status="record.status" />
      </template>
      <template v-else-if="column.key === 'entryDate' || column.key === 'warrantyExpiry'">
        {{ formatDate(record[column.key]) }}
      </template>
      <template v-else-if="column.key === 'qrcode'">
        <a-button type="default" size="small" @click="showQR(record)">QR</a-button>
      </template>
      <template v-else-if="column.key === 'decisionFile'">
        <a-button
          v-if="record.hasDecisionFile && ['Admin', 'Trưởng lab', 'Phó lab'].includes(role)"
          type="link"
          size="small"
          @click="downloadDecisionFile(record)"
        >
          Tải file
        </a-button>
        <span v-else-if="record.hasDecisionFile" class="muted">Có file</span>
        <span v-else class="muted">Chưa có</span>
      </template>
      <template v-else-if="column.key === 'action'">
        <a-space>
          <a-button type="link" size="small" @click="showViewModal(record)" title="Xem chi tiết">
            <template #icon><EyeOutlined /></template>
          </a-button>
          <a-tooltip :title="!['Admin', 'Trưởng lab', 'Phó lab'].includes(role) ? 'Chỉ dành cho Admin/Quản lý' : 'Sửa'">
            <a-button :disabled="!['Admin', 'Trưởng lab', 'Phó lab'].includes(role)" type="link" size="small" @click="showEditModal(record)">
              <template #icon><EditOutlined /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip :title="role !== 'Admin' ? 'Chỉ dành cho Admin' : 'Xóa'">
            <a-button :disabled="role !== 'Admin'" type="link" danger size="small" @click="handleDelete(record.id)">
              <template #icon><DeleteOutlined /></template>
            </a-button>
          </a-tooltip>
          <a-button v-if="['Sinh viên', 'Giảng viên'].includes(role) && statusMatches(record.status, STATUS.AVAILABLE)" type="primary" size="small" @click="handleBorrowClick(record)">Mượn</a-button>
        </a-space>
      </template>
    </template>
  </a-table>

  <a-modal v-model:open="isQRVisible" title="QR tài sản" :footer="null" centered>
    <div class="qr-box">
      <qrcode-vue :value="qrValue" :size="200" level="H" />
      <div class="qr-title">{{ selectedDeviceName }}</div>
      <div>Số seri: {{ selectedDeviceSerial }}</div>
    </div>
  </a-modal>

  <a-modal v-model:open="isScannerVisible" title="Quét QR để mượn" :footer="null" @cancel="stopScanner" centered>
    <div id="qr-reader" style="width: 100%;"></div>
  </a-modal>

  <a-modal v-model:open="isBorrowVisible" title="Yêu cầu mượn tài sản" @ok="submitBorrowRequest" okText="Gửi yêu cầu" cancelText="Hủy" :confirmLoading="borrowSubmitting">
    <a-form layout="vertical">
      <a-form-item label="Dự kiến trả" required>
        <a-date-picker v-model:value="borrowForm.returnDate" style="width: 100%" :disabled-date="disablePastDate" />
      </a-form-item>
      <a-form-item v-if="role === 'Sinh viên'" label="Giảng viên bảo lãnh" required>
        <a-select v-model:value="borrowForm.teacherId" placeholder="Chọn giảng viên" allowClear>
          <a-select-option v-for="t in teachers" :key="t.id" :value="t.id">{{ t.username }}</a-select-option>
        </a-select>
      </a-form-item>
      <a-form-item label="Mục đích mượn" required>
        <a-textarea v-model:value="borrowForm.purpose" :rows="4" />
      </a-form-item>
    </a-form>
  </a-modal>

  <a-modal v-model:open="isFormVisible" :title="isEditMode ? 'Sửa thiết bị' : 'Thêm thiết bị'" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting" width="800px">
    <a-form layout="vertical">
      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Tên thiết bị" required>
            <a-input v-model:value="formData.name" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="Danh mục phân loại">
            <a-select v-model:value="formData.assetCategoryId" placeholder="Chọn danh mục" allowClear>
              <a-select-option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</a-select-option>
            </a-select>
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Model" required>
            <a-input v-model:value="formData.model" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="Số seri" required>
            <a-input v-model:value="formData.serial" />
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Tên seri">
            <a-input v-model:value="formData.serialName" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="Vị trí" required>
            <a-input v-model:value="formData.location" />
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Người chịu trách nhiệm">
            <a-input v-model:value="formData.responsiblePerson" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="Trạng thái" required>
            <a-select v-model:value="formData.status">
              <a-select-option :value="STATUS.AVAILABLE">Rảnh</a-select-option>
              <a-select-option v-if="statusMatches(formData.status, STATUS.BORROWED)" :value="STATUS.BORROWED" disabled>Đang mượn</a-select-option>
              <a-select-option :value="STATUS.UNDER_WARRANTY">Bảo hành</a-select-option>
              <a-select-option :value="STATUS.BROKEN">Hỏng</a-select-option>
            </a-select>
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Ngày nhập">
            <a-date-picker v-model:value="formData.entryDate" style="width: 100%" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="Hạn bảo hành">
            <a-date-picker v-model:value="formData.warrantyExpiry" style="width: 100%" />
          </a-form-item>
        </a-col>
      </a-row>

      <a-row :gutter="16">
        <a-col :span="12">
          <a-form-item label="Số hóa đơn">
            <a-input v-model:value="formData.invoiceNumber" />
          </a-form-item>
        </a-col>
        <a-col :span="12">
          <a-form-item label="File quyết định mua/thêm thiết bị" :required="!isEditMode">
            <a-upload
              v-model:file-list="decisionFileList"
              :before-upload="beforeDecisionUpload"
              :max-count="1"
              accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
            >
              <a-button>Chọn file quyết định</a-button>
            </a-upload>
            <div v-if="isEditMode && formData.decisionFileName" class="file-hint">
              File hiện tại: {{ formData.decisionFileName }}
            </div>
          </a-form-item>
        </a-col>
      </a-row>
    </a-form>
  </a-modal>

  <a-modal v-model:open="isViewVisible" title="Chi tiết thiết bị" :footer="null" width="700px">
    <a-descriptions bordered :column="2">
      <a-descriptions-item label="Tên thiết bị">{{ viewData.name }}</a-descriptions-item>
      <a-descriptions-item label="Danh mục">{{ viewData.categoryName }}</a-descriptions-item>
      <a-descriptions-item label="Model">{{ viewData.model }}</a-descriptions-item>
      <a-descriptions-item label="Số seri">{{ viewData.serial }}</a-descriptions-item>
      <a-descriptions-item label="Tên seri" :span="2">{{ viewData.serialName || 'Không có' }}</a-descriptions-item>
      <a-descriptions-item label="Vị trí">{{ viewData.location }}</a-descriptions-item>
      <a-descriptions-item label="Người chịu trách nhiệm">{{ viewData.responsiblePerson || 'Không có' }}</a-descriptions-item>
      <a-descriptions-item label="Ngày nhập">{{ viewData.entryDate ? new Date(viewData.entryDate).toLocaleDateString('vi-VN') : 'Không có' }}</a-descriptions-item>
      <a-descriptions-item label="Hạn bảo hành">{{ viewData.warrantyExpiry ? new Date(viewData.warrantyExpiry).toLocaleDateString('vi-VN') : 'Không có' }}</a-descriptions-item>
      <a-descriptions-item label="Số hóa đơn">{{ viewData.invoiceNumber || 'Không có' }}</a-descriptions-item>
      <a-descriptions-item label="Trạng thái">
          <StatusBadge :status="viewData.status" />
      </a-descriptions-item>
    </a-descriptions>
  </a-modal>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import dayjs from 'dayjs'
import QrcodeVue from 'qrcode.vue'
import { Html5QrcodeScanner } from 'html5-qrcode'
import { message, Modal, Upload } from 'ant-design-vue'
import StatusBadge from './StatusBadge.vue'
import { STATUS, statusLabel, statusMatches } from '../constants/business'
import { EditOutlined, DeleteOutlined, EyeOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import { equipmentApi } from '../api/equipmentApi'
import { borrowApi } from '../api/borrowApi'
import { userApi } from '../api/userApi'
import { assetCategoryApi } from '../api/assetCategoryApi'

const authStore = useAuthStore()
const route = useRoute()
const role = computed(() => authStore.role)

const dataSource = ref([])
const categories = ref([])
const teachers = ref([])
const loading = ref(false)
const submitting = ref(false)
const borrowSubmitting = ref(false)

const columns = [
  { title: 'Tên thiết bị', dataIndex: 'name', key: 'name', fixed: 'left', width: 180 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 130 },
  { title: 'Model', dataIndex: 'model', key: 'model', width: 130 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 140 },
  { title: 'Tên seri', dataIndex: 'serialName', key: 'serialName', width: 140 },
  { title: 'Vị trí', dataIndex: 'location', key: 'location', width: 130 },
  { title: 'Người chịu trách nhiệm', dataIndex: 'responsiblePerson', key: 'responsiblePerson', width: 180 },
  { title: 'Quyết định', key: 'decisionFile', width: 120 },
  { title: 'Ngày nhập', dataIndex: 'entryDate', key: 'entryDate', width: 120 },
  { title: 'Hạn bảo hành', dataIndex: 'warrantyExpiry', key: 'warrantyExpiry', width: 130 },
  { title: 'Số hóa đơn', dataIndex: 'invoiceNumber', key: 'invoiceNumber', width: 140 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 130 },
  { title: 'QR', key: 'qrcode', align: 'center', width: 80 },
  { title: 'Hành động', key: 'action', align: 'center', fixed: 'right', width: 180 }
]

const searchQuery = ref('')
const debouncedSearchQuery = ref('')
let searchTimeout = null

const handleSearchChange = (e) => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    debouncedSearchQuery.value = e.target.value
  }, 500)
}

const filteredDataSource = computed(() => {
  let result = dataSource.value

  const status = route.query.status
  if (status && status !== 'all') {
    if (status === 'problem') {
      result = result.filter(item => [STATUS.BROKEN, STATUS.UNDER_WARRANTY].includes(item.status))
    } else {
      result = result.filter(item => statusMatches(item.status, status))
    }
  }

  const search = debouncedSearchQuery.value.trim().toLowerCase()
  if (search) {
    result = result.filter(item => 
      (item.name && item.name.toLowerCase().includes(search)) ||
      (item.serial && item.serial.toLowerCase().includes(search)) ||
      (item.model && item.model.toLowerCase().includes(search))
    )
  }

  return result
})

const emptyForm = () => ({
  name: '',
  model: '',
  serial: '',
  serialName: '',
  location: '',
  responsiblePerson: '',
  decisionFileName: '',
  entryDate: null,
  warrantyExpiry: null,
  invoiceNumber: '',
  status: STATUS.AVAILABLE,
  assetCategoryId: null
})

const isFormVisible = ref(false)
const isEditMode = ref(false)
const isViewVisible = ref(false)
const viewData = ref({})
const currentEditId = ref(null)
const formData = ref(emptyForm())
const decisionFileList = ref([])

const isQRVisible = ref(false)
const qrValue = ref('')
const selectedDeviceName = ref('')
const selectedDeviceSerial = ref('')

const isScannerVisible = ref(false)
let html5QrcodeScanner = null

const isBorrowVisible = ref(false)
const currentBorrowEquipmentId = ref(null)
const borrowForm = ref({ returnDate: null, purpose: '', teacherId: null })

onMounted(() => {
  fetchData()
  fetchTeachers()
  fetchCategories()
})

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : ''

const normalizeDate = (value, endOfDay = false) => {
  if (!value) return null
  if (typeof value.startOf === 'function') {
    return (endOfDay ? value.endOf('day') : value.startOf('day')).toISOString()
  }
  return new Date(value).toISOString()
}
const disablePastDate = (current) => current && current.valueOf() < new Date().setHours(0, 0, 0, 0)



const fetchCategories = async () => {
  try {
    categories.value = await assetCategoryApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh mục phân loại!')
  }
}

const fetchTeachers = async () => {
  try {
    const res = await userApi.getTeachers()
    teachers.value = res.data || res || []
  } catch (err) {
    console.error('Không tải được danh sách giảng viên', err)
  }
}

const fetchData = async () => {
  loading.value = true
  try {
    dataSource.value = await equipmentApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh sách thiết bị!')
  } finally {
    loading.value = false
  }
}

const showAddModal = () => {
  isEditMode.value = false
  formData.value = emptyForm()
  decisionFileList.value = []
  isFormVisible.value = true
}

const showEditModal = (record) => {
  isEditMode.value = true
  currentEditId.value = record.id
  formData.value = {
    ...emptyForm(),
    ...record,
    entryDate: record.entryDate ? dayjs(record.entryDate) : null,
    warrantyExpiry: record.warrantyExpiry ? dayjs(record.warrantyExpiry) : null
  }
  decisionFileList.value = []
  isFormVisible.value = true
}

const showViewModal = (record) => {
  viewData.value = { ...record }
  isViewVisible.value = true
}

const beforeDecisionUpload = (file) => {
  const allowedExtensions = ['pdf', 'doc', 'docx', 'jpg', 'jpeg', 'png']
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!allowedExtensions.includes(extension)) {
    message.error('Chỉ chấp nhận PDF, Word hoặc ảnh JPG/PNG!')
    return Upload.LIST_IGNORE
  }
  if (file.size > 10 * 1024 * 1024) {
    message.error('File quyết định không được vượt quá 10 MB!')
    return Upload.LIST_IGNORE
  }
  decisionFileList.value = [file]
  return false
}

const buildEquipmentFormData = () => {
  const payload = new FormData()
  payload.append('name', formData.value.name || '')
  payload.append('model', formData.value.model || '')
  payload.append('serial', formData.value.serial || '')
  payload.append('serialName', formData.value.serialName || '')
  payload.append('location', formData.value.location || '')
  payload.append('responsiblePerson', formData.value.responsiblePerson || '')
  payload.append('invoiceNumber', formData.value.invoiceNumber || '')
  payload.append('status', formData.value.status || STATUS.AVAILABLE)
  if (formData.value.assetCategoryId !== null && formData.value.assetCategoryId !== undefined) {
    payload.append('assetCategoryId', formData.value.assetCategoryId)
  }
  const entryDate = normalizeDate(formData.value.entryDate)
  const warrantyExpiry = normalizeDate(formData.value.warrantyExpiry, true)
  if (entryDate) payload.append('entryDate', entryDate)
  if (warrantyExpiry) payload.append('warrantyExpiry', warrantyExpiry)
  if (decisionFileList.value[0]?.originFileObj || decisionFileList.value[0]) {
    payload.append('decisionFile', decisionFileList.value[0].originFileObj || decisionFileList.value[0])
  }
  return payload
}

const submitForm = async () => {
  if (!formData.value.name || !formData.value.model || !formData.value.serial || !formData.value.location) {
    message.warning('Vui lòng nhập đủ tên, model, số seri và vị trí!')
    return
  }

  if (!isEditMode.value && decisionFileList.value.length === 0) {
    message.warning('Vui lòng tải lên file quyết định mua/thêm thiết bị!')
    return
  }

  const payload = buildEquipmentFormData()

  submitting.value = true
  try {
    if (isEditMode.value) {
      await equipmentApi.update(currentEditId.value, payload)
      message.success('Đã cập nhật thiết bị!')
    } else {
      await equipmentApi.create(payload)
      message.success('Đã thêm thiết bị!')
    }
    isFormVisible.value = false
    fetchData()
  } catch {
    message.error('Lỗi khi lưu thiết bị!')
  } finally {
    submitting.value = false
  }
}

const downloadDecisionFile = async (record) => {
  try {
    const res = await equipmentApi.downloadDecisionFile(record.id)
    const url = window.URL.createObjectURL(new Blob([res]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', record.decisionFileName || `QuyetDinh_${record.id}`)
    document.body.appendChild(link)
    link.click()
    link.parentNode.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch {
    message.error('Không tải được file quyết định!')
  }
}

const handleDelete = (id) => {
  Modal.confirm({
    title: 'Xóa thiết bị',
    content: 'Bạn chắc chắn muốn xóa thiết bị này?',
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await equipmentApi.delete(id)
        message.success('Đã xóa thiết bị!')
        fetchData()
      } catch {
        message.error('Lỗi khi xóa thiết bị!')
      }
    }
  })
}

const handleExport = async () => {
  try {
    message.loading({ content: 'Đang xuất báo cáo...', key: 'export' })
    const res = await equipmentApi.export()
    const url = window.URL.createObjectURL(new Blob([res]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `TaiSan_${new Date().getTime()}.xlsx`)
    document.body.appendChild(link)
    link.click()
    link.parentNode.removeChild(link)
    message.success({ content: 'Xuất Excel thành công!', key: 'export' })
  } catch {
    message.error({ content: 'Lỗi khi xuất Excel!', key: 'export' })
  }
}

const showQR = (record) => {
  qrValue.value = `DEVICE:${record.serial}`
  selectedDeviceName.value = record.name
  selectedDeviceSerial.value = record.serial
  isQRVisible.value = true
}

const handleBorrowClick = (record) => {
  currentBorrowEquipmentId.value = record.id
  isBorrowVisible.value = true
}

const submitBorrowRequest = async () => {
  if (!borrowForm.value.returnDate || !borrowForm.value.purpose || (role.value === 'Sinh viên' && !borrowForm.value.teacherId)) {
    message.warning(role.value === 'Sinh viên'
      ? 'Vui lòng nhập ngày trả, mục đích và giảng viên bảo lãnh!'
      : 'Vui lòng nhập ngày trả và mục đích mượn!')
    return
  }

  borrowSubmitting.value = true
  try {
    await borrowApi.createRequest({
      equipmentId: currentBorrowEquipmentId.value,
      expectedReturnDate: borrowForm.value.returnDate.endOf('day').toISOString(),
      purpose: borrowForm.value.purpose,
      teacherId: borrowForm.value.teacherId || null
    })
    message.success('Đã gửi yêu cầu mượn!')
    isBorrowVisible.value = false
    borrowForm.value = { returnDate: null, purpose: '', teacherId: null }
  } catch (err) {
    message.error(err.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu mượn!')
  } finally {
    borrowSubmitting.value = false
  }
}

const showScannerModal = () => {
  isScannerVisible.value = true
  nextTick(() => {
    html5QrcodeScanner = new Html5QrcodeScanner('qr-reader', { fps: 10, qrbox: { width: 250, height: 250 } }, false)
    html5QrcodeScanner.render(onScanSuccess, () => {})
  })
}

const stopScanner = () => {
  if (html5QrcodeScanner) {
    html5QrcodeScanner.clear().catch(error => console.error('Failed to clear QR scanner', error))
    html5QrcodeScanner = null
  }
}

onUnmounted(stopScanner)

const onScanSuccess = (decodedText) => {
  if (!decodedText.startsWith('DEVICE:')) {
    message.error('Mã QR không hợp lệ!')
    return
  }

  const serial = decodedText.split(':')[1]
  const device = dataSource.value.find(d => d.serial === serial)
  if (!device) {
    message.error('Không tìm thấy thiết bị!')
    return
  }

  stopScanner()
  isScannerVisible.value = false
  if (!statusMatches(device.status, STATUS.AVAILABLE)) {
    message.warning(`Thiết bị ${device.name} hiện đang ${statusLabel(device.status)}. Không thể mượn!`)
    return
  }
  handleBorrowClick(device)
}
</script>

<style scoped>
.table-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.left-actions {
  display: flex;
  gap: 8px;
}

.right-actions {
  display: flex;
  gap: 8px;
}

.qr-box {
  text-align: center;
  padding: 20px;
}

.qr-title {
  margin-top: 16px;
  font-weight: 700;
}

.file-hint,
.muted {
  color: #6b7280;
  font-size: 13px;
  margin-top: 8px;
}
</style>



