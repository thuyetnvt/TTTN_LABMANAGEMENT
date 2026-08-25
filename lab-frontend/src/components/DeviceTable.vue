<template>
  <div class="table-actions">
    <div class="left-actions">
      <a-button v-if="isBorrowerRole(role)" type="primary" @click="showScannerModal('borrow')">
        Quét QR để mượn
      </a-button>
      <a-button v-if="isManagerRole(role)" type="primary" ghost @click="showScannerModal('inventory')">
        Quét QR kiểm kê
      </a-button>
      <a-input
        v-model:value="searchQuery"
        placeholder="Tìm kiếm theo tên thiết bị..."
        style="width: 250px"
        @change="handleSearchChange"
      />
    </div>
    <div class="right-actions">
      <a-button
        v-if="isManagerRole(role)"
        :disabled="!selectedBatchItems.length"
        @click="openBatchQR"
      >
        In QR đã chọn ({{ selectedBatchItems.length }})
      </a-button>
      <a-button v-if="isManagerRole(role)" @click="openImport">
        Nhập Excel
      </a-button>
      <a-button v-if="isManagerRole(role)" type="primary" ghost @click="handleExport">
        Xuất Excel
      </a-button>
      <a-button v-if="isManagerRole(role)" type="primary" @click="showAddModal">
        + Thêm thiết bị
      </a-button>
    </div>
  </div>

  <a-table
    :dataSource="filteredDataSource"
    :columns="columns"
    :loading="loading"
    rowKey="id"
    bordered
    :scroll="{ x: 1500 }"
    :row-selection="isManager ? rowSelection : undefined"
  >
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
          v-if="record.hasDecisionFile && isManagerRole(role)"
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
          <a-button v-if="isManagerRole(role)" type="link" size="small" @click="showEditModal(record)" title="Sửa">
            <template #icon><EditOutlined /></template>
          </a-button>
          <a-button v-if="isAdminRole(role)" type="link" danger size="small" @click="handleDelete(record.id)" title="Xóa">
            <template #icon><DeleteOutlined /></template>
          </a-button>
          <a-button v-if="isManagerRole(role)" type="link" size="small" @click="handleInventory(record)">Kiểm kê</a-button>
          <a-button v-if="isBorrowerRole(role) && statusMatches(record.status, STATUS.AVAILABLE)" type="primary" size="small" @click="handleBorrowClick(record)">Mượn</a-button>
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

  <a-modal v-model:open="isBatchQRVisible" title="In QR hàng loạt" :footer="null" width="900px">
    <div ref="qrPrintSheet" class="qr-print-sheet">
      <div v-for="item in selectedBatchItems" :key="item.id" class="qr-print-card">
        <qrcode-vue :value="`DEVICE_TOKEN:${item.qrToken || item.serial}`" :size="150" level="H" />
        <strong>{{ item.name }}</strong>
        <span>{{ item.assetCode || item.serial }}</span>
      </div>
    </div>
    <a-space class="batch-actions">
      <a-button @click="isBatchQRVisible = false">Đóng</a-button>
      <a-button type="primary" @click="printBatchQR">In</a-button>
    </a-space>
  </a-modal>

  <a-modal v-model:open="isImportVisible" title="Nhập Excel tài sản" :footer="null" width="1100px">
    <a-upload :before-upload="previewImport" :show-upload-list="false" accept=".xlsx">
      <a-button :loading="importLoading">Chọn file Excel và xem trước</a-button>
    </a-upload>
    <a-alert v-if="importPreviewRows.length" class="import-summary" type="info" :message="`Tổng ${importPreviewRows.length} dòng — hợp lệ ${importValidCount}, lỗi ${importPreviewRows.length - importValidCount}`" />
    <a-table v-if="importPreviewRows.length" :data-source="importPreviewRows" :columns="importColumns" row-key="rowNumber" size="small" bordered :pagination="{ pageSize: 10 }" :scroll="{ x: 900 }">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'name'">{{ record.row.name }}</template>
        <template v-else-if="column.key === 'model'">{{ record.row.model }}</template>
        <template v-else-if="column.key === 'serial'">{{ record.row.serial }}</template>
        <template v-else-if="column.key === 'location'">{{ record.row.location }}</template>
        <template v-else-if="column.key === 'result'">
          <a-tag :color="record.valid ? 'green' : 'red'">{{ record.valid ? 'Hợp lệ' : record.errors.join('; ') }}</a-tag>
        </template>
      </template>
    </a-table>
    <a-space v-if="importPreviewRows.length" class="batch-actions">
      <a-button @click="isImportVisible = false">Hủy</a-button>
      <a-button type="primary" :disabled="!importValidCount" :loading="importSaving" @click="commitImport">Nhập {{ importValidCount }} dòng hợp lệ</a-button>
    </a-space>
  </a-modal>

  <a-modal v-model:open="isScannerVisible" :title="inventoryScannerMode ? 'Quét QR kiểm kê' : 'Quét QR để mượn'" :footer="null" @cancel="stopScanner" centered>
    <div id="qr-reader" style="width: 100%;"></div>
  </a-modal>

  <a-modal v-model:open="isBorrowVisible" title="Yêu cầu mượn tài sản" @ok="submitBorrowRequest" okText="Gửi yêu cầu" cancelText="Hủy" :confirmLoading="borrowSubmitting">
    <a-form layout="vertical">
      <a-form-item label="Dự kiến trả" required>
        <a-date-picker v-model:value="borrowForm.returnDate" style="width: 100%" :disabled-date="disablePastDate" />
      </a-form-item>
      <a-form-item label="Tài sản trong phiếu mượn" required>
        <a-select v-model:value="borrowSelectionToAdd" placeholder="Chọn thêm tài sản đang rảnh" allowClear @change="addBorrowItem">
          <a-select-option v-for="item in availableBorrowOptions" :key="item.id" :value="item.id">
            {{ item.name }} — {{ item.serial }}
          </a-select-option>
        </a-select>
        <a-list v-if="borrowItems.length" size="small" bordered class="borrow-items-list">
          <a-list-item v-for="item in borrowItems" :key="item.id">
            <span>{{ item.name }} — {{ item.serial }}</span>
            <a-button type="link" danger size="small" @click="removeBorrowItem(item.id)">Bỏ</a-button>
          </a-list-item>
        </a-list>
      </a-form-item>
      <a-form-item v-if="isStudentRole(role)" label="Giảng viên bảo lãnh" required>
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

      <a-form-item v-if="isEditMode" label="Lý do điều chuyển vị trí">
        <a-input v-model:value="formData.locationChangeReason" placeholder="Bắt buộc nếu thay đổi vị trí" />
      </a-form-item>

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
            <LocationTreeSelect
              v-model:value="formData.locationNodeId"
              :nodes="locations"
              placeholder="Chọn vị trí trong cây"
              @change="syncLocationName"
            />
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

  <a-modal v-model:open="isViewVisible" title="Chi tiết thiết bị" :footer="null" width="780px" wrap-class-name="equipment-detail-modal">
    <div class="equipment-detail-content" data-testid="equipment-detail-modal">
      <section v-for="section in detailSections" :key="section.key" class="equipment-detail-section">
        <h3>{{ section.title }}</h3>
        <dl class="equipment-detail-grid">
          <div v-for="field in section.fields" :key="field.key" class="equipment-detail-field">
            <dt>{{ field.label }}</dt>
            <dd v-if="field.key === 'status'"><StatusBadge :status="viewData.status" /></dd>
            <dd v-else>{{ field.value }}</dd>
          </div>
        </dl>
      </section>
    </div>
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
import LocationTreeSelect from './LocationTreeSelect.vue'
import { STATUS, isAdminRole, isBorrowerRole, isManagerRole, isStudentRole, statusLabel, statusMatches } from '../constants/business'
import { EditOutlined, DeleteOutlined, EyeOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import { equipmentApi } from '../api/equipmentApi'
import { borrowApi } from '../api/borrowApi'
import { userApi } from '../api/userApi'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { locationApi } from '../api/locationApi'

const authStore = useAuthStore()
const route = useRoute()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))

const dataSource = ref([])
const categories = ref([])
const locations = ref([])
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
  locationNodeId: null,
  locationChangeReason: '',
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
const selectedBatchKeys = ref([])
const isBatchQRVisible = ref(false)
const qrPrintSheet = ref(null)
const isImportVisible = ref(false)
const importLoading = ref(false)
const importSaving = ref(false)
const importPreviewRows = ref([])
const importColumns = [
  { title: 'Dòng', dataIndex: 'rowNumber', key: 'rowNumber', width: 60 },
  { title: 'Tên thiết bị', key: 'name' },
  { title: 'Model', key: 'model' },
  { title: 'Số seri', key: 'serial' },
  { title: 'Vị trí', key: 'location' },
  { title: 'Kết quả', key: 'result', width: 280 }
]

const isScannerVisible = ref(false)
const inventoryScannerMode = ref(false)
let html5QrcodeScanner = null

const isBorrowVisible = ref(false)
const currentBorrowEquipmentId = ref(null)
const borrowItems = ref([])
const borrowSelectionToAdd = ref(null)
const borrowForm = ref({ returnDate: null, purpose: '', teacherId: null })

const availableBorrowOptions = computed(() => dataSource.value.filter(item =>
  statusMatches(item.status, STATUS.AVAILABLE) && !borrowItems.value.some(selected => selected.id === item.id)
))

const selectedBatchItems = computed(() => dataSource.value.filter(item => selectedBatchKeys.value.includes(item.id)))
const importValidCount = computed(() => importPreviewRows.value.filter(row => row.valid).length)
const rowSelection = computed(() => ({
  selectedRowKeys: selectedBatchKeys.value,
  onChange: (keys) => { selectedBatchKeys.value = keys }
}))

onMounted(() => {
  fetchData()
  fetchTeachers()
  fetchCategories()
  fetchLocations()
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

const syncLocationName = (locationNodeId) => {
  const location = locations.value.find(item => item.id === locationNodeId)
  if (location) formData.value.location = location.name
}

const showViewModal = (record) => {
  viewData.value = { ...record }
  isViewVisible.value = true
}

const hasDetailValue = (value) => value !== null && value !== undefined && String(value).trim() !== ''
const detailDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : ''
const detailField = (key, label, value) => ({ key, label, value })

const detailSections = computed(() => {
  const data = viewData.value || {}
  const sections = [
    {
      key: 'basic',
      title: 'Thông tin cơ bản',
      fields: [
        detailField('assetCode', 'Mã tài sản', data.assetCode),
        detailField('name', 'Tên thiết bị', data.name),
        detailField('categoryName', 'Danh mục', data.categoryName),
        detailField('model', 'Model', data.model),
        detailField('serial', 'Số seri', data.serial),
        detailField('serialName', 'Tên định danh', data.serialName),
        detailField('invoiceNumber', 'Số hóa đơn', data.invoiceNumber)
      ]
    },
    {
      key: 'location',
      title: 'Vị trí',
      fields: [detailField('location', 'Vị trí lưu trữ', data.location)]
    },
    {
      key: 'management',
      title: 'Quản lý',
      fields: [
        detailField('responsiblePerson', 'Người phụ trách', data.responsiblePerson),
        detailField('status', 'Trạng thái', data.status)
      ]
    },
    {
      key: 'warranty',
      title: 'Bảo hành',
      fields: [
        detailField('entryDate', 'Ngày nhập', detailDate(data.entryDate)),
        detailField('warrantyExpiry', 'Hạn bảo hành', detailDate(data.warrantyExpiry))
      ]
    }
  ]
  return sections
    .map(section => ({ ...section, fields: section.fields.filter(field => hasDetailValue(field.value)) }))
    .filter(section => section.fields.length)
})

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
  if (formData.value.locationNodeId !== null && formData.value.locationNodeId !== undefined) {
    payload.append('locationNodeId', formData.value.locationNodeId)
  }
  if (formData.value.locationChangeReason) payload.append('locationChangeReason', formData.value.locationChangeReason)
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
  if (!formData.value.name || !formData.value.model || !formData.value.serial || !formData.value.locationNodeId) {
    message.warning('Vui lòng nhập đủ tên, model, số seri và chọn vị trí!')
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
  qrValue.value = `DEVICE_TOKEN:${record.qrToken || record.serial}`
  selectedDeviceName.value = record.name
  selectedDeviceSerial.value = record.serial
  isQRVisible.value = true
}

const fetchLocations = async () => {
  try {
    locations.value = await locationApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải cây vị trí!')
  }
}

const openBatchQR = () => {
  if (!selectedBatchItems.value.length) {
    message.warning('Hãy chọn ít nhất một tài sản để in QR.')
    return
  }
  isBatchQRVisible.value = true
}

const printBatchQR = async () => {
  await nextTick()
  const printWindow = window.open('', '_blank', 'noopener,noreferrer,width=1000,height=800')
  if (!printWindow || !qrPrintSheet.value) {
    message.error('Trình duyệt đã chặn cửa sổ in. Hãy cho phép popup rồi thử lại.')
    return
  }
  const markup = qrPrintSheet.value.innerHTML
    .replaceAll('qr-print-card', 'card')
    .replace('qr-print-sheet', 'sheet')
  printWindow.document.write(`<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>QR tài sản</title><style>body{font-family:Arial,sans-serif;margin:20px}.sheet{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.card{border:1px solid #ddd;padding:14px;text-align:center;page-break-inside:avoid}.card svg{display:block;margin:0 auto 10px}.card strong,.card span{display:block;margin-top:4px;font-size:13px}@media print{body{margin:0}.sheet{padding:10mm}}</style></head><body><div class="sheet">${markup}</div></body></html>`)
  printWindow.document.close()
  printWindow.focus()
  printWindow.print()
  printWindow.close()
}

const openImport = () => {
  importPreviewRows.value = []
  isImportVisible.value = true
}

const previewImport = async (file) => {
  const payload = new FormData()
  payload.append('file', file)
  importLoading.value = true
  try {
    const result = await equipmentApi.previewImport(payload)
    importPreviewRows.value = result.rows || []
    if (!importPreviewRows.value.length) message.warning('File không có dòng dữ liệu để import.')
    else message.success(`Đã đọc ${importPreviewRows.value.length} dòng và kiểm tra lỗi.`)
  } catch (error) {
    message.error(error?.response?.data?.message || error.message || 'Không thể đọc file Excel.')
  } finally {
    importLoading.value = false
  }
  return Upload.LIST_IGNORE
}

const commitImport = async () => {
  const rows = importPreviewRows.value.filter(row => row.valid).map(row => row.row)
  if (!rows.length) return
  importSaving.value = true
  try {
    await equipmentApi.importRows(rows)
    message.success(`Đã import ${rows.length} tài sản.`)
    isImportVisible.value = false
    await fetchData()
  } catch (error) {
    message.error(error?.response?.data?.message || error.message || 'Không thể import tài sản.')
  } finally {
    importSaving.value = false
  }
}

const handleBorrowClick = (record) => {
  currentBorrowEquipmentId.value = record.id
  borrowItems.value = [{ id: record.id, name: record.name, serial: record.serial }]
  borrowSelectionToAdd.value = null
  isBorrowVisible.value = true
}

const handleInventory = async (record) => {
  try {
    await equipmentApi.inventory(record.id)
    record.lastInventoryAt = new Date().toISOString()
    message.success(`Đã ghi nhận kiểm kê ${record.name}.`)
  } catch (err) {
    message.error(err.response?.data?.message || 'Không thể ghi nhận kiểm kê!')
  }
}

const addBorrowItem = (id) => {
  if (!id) return
  const item = dataSource.value.find(candidate => candidate.id === id)
  if (item && !borrowItems.value.some(selected => selected.id === id)) {
    borrowItems.value.push({ id: item.id, name: item.name, serial: item.serial })
  }
  borrowSelectionToAdd.value = null
}

const removeBorrowItem = (id) => {
  if (borrowItems.value.length <= 1) {
    message.warning('Phiếu mượn phải có ít nhất một tài sản!')
    return
  }
  borrowItems.value = borrowItems.value.filter(item => item.id !== id)
}

const submitBorrowRequest = async () => {
  if (!borrowForm.value.returnDate || !borrowForm.value.purpose || (isStudentRole(role.value) && !borrowForm.value.teacherId)) {
    message.warning(isStudentRole(role.value)
      ? 'Vui lòng nhập ngày trả, mục đích và giảng viên bảo lãnh!'
      : 'Vui lòng nhập ngày trả và mục đích mượn!')
    return
  }

  borrowSubmitting.value = true
  try {
    await borrowApi.createRequest({
      expectedReturnDate: borrowForm.value.returnDate.endOf('day').toISOString(),
      purpose: borrowForm.value.purpose,
      teacherId: borrowForm.value.teacherId || null,
      equipmentId: borrowItems.value[0]?.id || currentBorrowEquipmentId.value,
      items: borrowItems.value.map(item => ({ equipmentId: item.id }))
    })
    message.success('Đã gửi yêu cầu mượn!')
    isBorrowVisible.value = false
    borrowItems.value = []
    borrowSelectionToAdd.value = null
    borrowForm.value = { returnDate: null, purpose: '', teacherId: null }
  } catch (err) {
    message.error(err.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu mượn!')
  } finally {
    borrowSubmitting.value = false
  }
}

const showScannerModal = (mode = 'borrow') => {
  inventoryScannerMode.value = mode === 'inventory'
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
  const isTokenQr = decodedText.startsWith('DEVICE_TOKEN:')
  const isLegacyQr = decodedText.startsWith('DEVICE:')
  if (!isTokenQr && !isLegacyQr) {
    message.error('Mã QR không hợp lệ!')
    return
  }

  const value = decodedText.slice(decodedText.indexOf(':') + 1)
  const device = dataSource.value.find(d => isTokenQr ? d.qrToken === value : d.serial === value)
  if (!device) {
    message.error('Không tìm thấy thiết bị!')
    return
  }

  stopScanner()
  isScannerVisible.value = false
  if (inventoryScannerMode.value) {
    handleInventory(device)
    return
  }
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

.qr-print-sheet {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  max-height: 60vh;
  overflow: auto;
}

.qr-print-card {
  border: 1px solid #d9d9d9;
  padding: 12px;
  text-align: center;
}

.qr-print-card :deep(svg) {
  display: block;
  margin: 0 auto 8px;
}

.qr-print-card strong,
.qr-print-card span {
  display: block;
  margin-top: 4px;
  font-size: 12px;
}

.batch-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

.import-summary {
  margin: 16px 0;
}

.equipment-detail-content {
  display: grid;
  gap: 18px;
}

:global(.equipment-detail-modal .ant-modal) {
  max-width: calc(100vw - 24px);
}

.equipment-detail-section {
  min-width: 0;
  padding: 16px;
  border: 1px solid rgba(0, 0, 0, 0.06);
  border-radius: 12px;
  background: #fffaf7;
}

.equipment-detail-section h3 {
  margin: 0 0 12px;
  color: var(--color-ink);
  font-size: 15px;
  font-weight: 700;
}

.equipment-detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px 20px;
  margin: 0;
}

.equipment-detail-field {
  min-width: 0;
  padding-bottom: 10px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.equipment-detail-field dt {
  margin-bottom: 5px;
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
}

.equipment-detail-field dd {
  min-width: 0;
  margin: 0;
  color: var(--color-ink);
  font-size: 14px;
  line-height: 1.45;
  overflow-wrap: anywhere;
}

@media (max-width: 640px) {
  :global(.equipment-detail-modal .ant-modal) {
    width: calc(100vw - 24px) !important;
    margin: 12px auto;
  }

  .equipment-detail-grid {
    grid-template-columns: 1fr;
    gap: 10px;
  }

  .equipment-detail-section {
    padding: 14px;
  }
}
</style>



