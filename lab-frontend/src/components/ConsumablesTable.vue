<template>
  <div>
    <div class="table-actions" v-if="isManagerRole(role)">
      <a-button type="primary" @click="showAddModal">+ Thêm vật tư</a-button>
    </div>

    <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1100 }">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'quantity'">
          <span :style="{ color: record.quantity <= record.minQuantity ? '#dc2626' : '#16a34a', fontWeight: 700 }">
            {{ record.quantity }}
          </span>
        </template>
        <template v-else-if="column.key === 'entryDate'">
          {{ record.entryDate ? new Date(record.entryDate).toLocaleDateString('vi-VN') : '' }}
        </template>
        <template v-else-if="column.key === 'expiryDate'">
          {{ record.expiryDate ? new Date(record.expiryDate).toLocaleDateString('vi-VN') : 'Không áp dụng' }}
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag :color="record.quantity <= record.minQuantity ? 'red' : 'green'">
            {{ record.quantity <= record.minQuantity ? 'Cần nhập thêm' : 'Đủ dùng' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button v-if="isBorrowerRole(role)" type="link" size="small" @click="showRequestModal(record)">Yêu cầu cấp phát</a-button>
            <a-button v-if="isManagerRole(role)" type="link" size="small" @click="showHistoryModal(record)" title="Lịch sử">
              <template #icon><HistoryOutlined /></template>
            </a-button>
            <a-button v-if="isManagerRole(role)" type="link" size="small" @click="showEditModal(record)" title="Sửa">
              <template #icon><EditOutlined /></template>
            </a-button>
            <a-button v-if="isAdminRole(role)" type="link" danger size="small" @click="handleDelete(record.id)" title="Xóa">
              <template #icon><DeleteOutlined /></template>
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-modal v-model:open="isFormVisible" :title="isEditMode ? 'Sửa vật tư' : 'Thêm vật tư'" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting" width="800px" wrapClassName="responsive-modal">
      <a-form layout="vertical">
        <a-row :gutter="16">
          <a-col :xs="24" :sm="12">
            <a-form-item label="Mã vật tư">
              <a-input v-model:value="formData.code" placeholder="Tự sinh nếu bỏ trống" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Tên vật tư" required>
              <a-input v-model:value="formData.name" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Danh mục phân loại">
              <a-select v-model:value="formData.assetCategoryId" placeholder="Chọn danh mục" allowClear>
                <a-select-option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</a-select-option>
              </a-select>
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Đơn vị tính" required>
              <a-input v-model:value="formData.unit" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Số lượng hiện có" required>
              <a-input-number v-model:value="formData.quantity" style="width: 100%" :min="0" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Tồn tối thiểu" required>
              <a-input-number v-model:value="formData.minQuantity" style="width: 100%" :min="1" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Người chịu trách nhiệm">
              <a-input v-model:value="formData.responsiblePerson" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Ngày nhập">
              <a-date-picker v-model:value="formData.entryDate" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12">
            <a-form-item label="Số hóa đơn">
              <a-input v-model:value="formData.invoiceNumber" />
            </a-form-item>
          </a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Nhà cung cấp"><a-input v-model:value="formData.supplier" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Giá nhập mỗi đơn vị"><a-input-number v-model:value="formData.unitCost" :min="0" style="width: 100%" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Vị trí lưu"><a-input v-model:value="formData.storageLocation" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Số lô"><a-input v-model:value="formData.lotNumber" /></a-form-item></a-col>
          <a-col :xs="24" :sm="12"><a-form-item label="Hạn sử dụng"><a-date-picker v-model:value="formData.expiryDate" style="width: 100%" /></a-form-item></a-col>
        </a-row>
      </a-form>
    </a-modal>

    <a-modal v-model:open="isRequestModalVisible" title="Yêu cầu cấp phát vật tư" @ok="submitRequest" @cancel="isRequestModalVisible = false" okText="Gửi yêu cầu" cancelText="Hủy" :confirmLoading="requestSubmitting">
      <a-form layout="vertical">
        <a-form-item label="Vật tư">
          <strong>{{ currentRequestConsumable?.name }}</strong>
        </a-form-item>
        <a-form-item label="Số lượng" required>
          <a-input-number v-model:value="requestForm.quantity" style="width: 100%" :min="1" :max="currentRequestConsumable?.quantity" />
        </a-form-item>
        <a-form-item label="Mục đích" required>
          <a-textarea v-model:value="requestForm.reason" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      v-model:open="isHistoryVisible"
      :title="`Lịch sử nhập-xuất: ${currentHistoryConsumable?.name || ''}`"
      width="860px"
      :footer="null"
      @cancel="isHistoryVisible = false"
    >
      <a-table
        :dataSource="historyData"
        :columns="historyColumns"
        :loading="historyLoading"
        rowKey="id"
        size="small"
        :pagination="{ pageSize: 8 }"
        :scroll="{ x: 780 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'type'">
            <a-tag :color="getTransactionColor(record.type)">{{ record.type }}</a-tag>
          </template>
          <template v-else-if="column.key === 'quantity'">
            <strong>{{ record.quantity }}</strong>
          </template>
          <template v-else-if="column.key === 'change'">
            {{ record.beforeQuantity }} → {{ record.afterQuantity }}
          </template>
          <template v-else-if="column.key === 'createdAt'">
            {{ formatDateTime(record.createdAt) }}
          </template>
          <template v-else-if="column.key === 'username'">
            {{ record.username || 'Hệ thống' }}
          </template>
        </template>
      </a-table>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import dayjs from 'dayjs'
import { message, Modal } from 'ant-design-vue'
import { consumableApi } from '../api/consumableApi'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isBorrowerRole, isManagerRole } from '../constants/business'
import { EditOutlined, DeleteOutlined, EyeOutlined, HistoryOutlined } from '@ant-design/icons-vue'

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const categories = ref([])
const loading = ref(false)
const submitting = ref(false)

const columns = [
  { title: 'Mã vật tư', dataIndex: 'code', key: 'code', width: 180 },
  { title: 'Tên vật tư', dataIndex: 'name', key: 'name', fixed: 'left', width: 180 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 140 },
  { title: 'Đơn vị', dataIndex: 'unit', key: 'unit', width: 100 },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'center', width: 110 },
  { title: 'Tồn tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity', align: 'center', width: 120 },
  { title: 'Người chịu trách nhiệm', dataIndex: 'responsiblePerson', key: 'responsiblePerson', width: 180 },
  { title: 'Ngày nhập', dataIndex: 'entryDate', key: 'entryDate', width: 120 },
  { title: 'Số hóa đơn', dataIndex: 'invoiceNumber', key: 'invoiceNumber', width: 140 },
  { title: 'Nhà cung cấp', dataIndex: 'supplier', key: 'supplier', width: 160 },
  { title: 'Hạn sử dụng', dataIndex: 'expiryDate', key: 'expiryDate', width: 120 },
  { title: 'Trạng thái', key: 'status', align: 'center', width: 120 },
  { title: 'Hành động', key: 'action', align: 'center', fixed: 'right', width: 230 }
]

const historyColumns = [
  { title: 'Thời gian', dataIndex: 'createdAt', key: 'createdAt', width: 150 },
  { title: 'Loại', dataIndex: 'type', key: 'type', width: 110 },
  { title: 'Số lượng', dataIndex: 'quantity', key: 'quantity', align: 'center', width: 90 },
  { title: 'Trước → Sau', key: 'change', align: 'center', width: 120 },
  { title: 'Người thực hiện', dataIndex: 'username', key: 'username', width: 140 },
  { title: 'Lý do', dataIndex: 'reason', key: 'reason', width: 220 }
]

const emptyForm = () => ({
  code: '',
  name: '',
  unit: '',
  quantity: 0,
  minQuantity: 5,
  responsiblePerson: '',
  assetCategoryId: null,
  entryDate: null,
  invoiceNumber: '',
  supplier: '',
  unitCost: null,
  storageLocation: '',
  lotNumber: '',
  expiryDate: null
})

const isFormVisible = ref(false)
const isEditMode = ref(false)
const currentEditId = ref(null)
const formData = ref(emptyForm())

const isRequestModalVisible = ref(false)
const requestSubmitting = ref(false)
const currentRequestConsumable = ref(null)
const requestForm = ref({ quantity: 1, reason: '' })
const isHistoryVisible = ref(false)
const historyLoading = ref(false)
const historyData = ref([])
const currentHistoryConsumable = ref(null)

onMounted(() => {
  fetchData()
  fetchCategories()
})

const fetchCategories = async () => {
  try {
    categories.value = await assetCategoryApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh mục phân loại!')
  }
}

const fetchData = async () => {
  loading.value = true
  try {
    dataSource.value = await consumableApi.getAll() || []
  } catch {
    message.error('Lỗi khi tải danh sách vật tư!')
  } finally {
    loading.value = false
  }
}

const showAddModal = () => {
  isEditMode.value = false
  formData.value = emptyForm()
  isFormVisible.value = true
}

const showEditModal = (record) => {
  isEditMode.value = true
  currentEditId.value = record.id
  formData.value = {
    ...emptyForm(),
    ...record,
    entryDate: record.entryDate ? dayjs(record.entryDate) : null,
    expiryDate: record.expiryDate ? dayjs(record.expiryDate) : null
  }
  isFormVisible.value = true
}

const showRequestModal = (record) => {
  currentRequestConsumable.value = record
  requestForm.value = { quantity: 1, reason: '' }
  isRequestModalVisible.value = true
}

const showHistoryModal = async (record) => {
  currentHistoryConsumable.value = record
  isHistoryVisible.value = true
  historyLoading.value = true
  try {
    historyData.value = await consumableApi.getTransactions(record.id) || []
  } catch {
    message.error('Lỗi khi tải lịch sử nhập-xuất!')
  } finally {
    historyLoading.value = false
  }
}

const formatDateTime = (value) => {
  return value ? new Date(value).toLocaleString('vi-VN') : ''
}

const getTransactionColor = (type) => {
  if (type === 'Nhập kho') return 'green'
  if (type === 'Cấp phát') return 'blue'
  if (type === 'Hoàn trả') return 'purple'
  return 'orange'
}

const submitForm = async () => {
  if (!formData.value.name || !formData.value.unit || formData.value.quantity === null || !formData.value.minQuantity) {
    message.warning('Vui lòng nhập đủ thông tin bắt buộc!')
    return
  }

  const payload = {
    ...formData.value,
    entryDate: formData.value.entryDate ? new Date(formData.value.entryDate).toISOString() : null,
    expiryDate: formData.value.expiryDate ? new Date(formData.value.expiryDate).toISOString() : null
  }

  submitting.value = true
  try {
    if (isEditMode.value) {
      await consumableApi.update(currentEditId.value, payload)
      message.success('Đã cập nhật vật tư!')
    } else {
      await consumableApi.create(payload)
      message.success('Đã thêm vật tư!')
    }
    isFormVisible.value = false
    fetchData()
  } catch {
    message.error('Lỗi khi lưu vật tư!')
  } finally {
    submitting.value = false
  }
}

const submitRequest = async () => {
  if (!requestForm.value.quantity || !requestForm.value.reason) {
    message.warning('Vui lòng nhập số lượng và mục đích!')
    return
  }

  requestSubmitting.value = true
  try {
    await consumableRequestApi.create({
      consumableId: currentRequestConsumable.value.id,
      quantity: requestForm.value.quantity,
      reason: requestForm.value.reason
    })
    message.success('Đã gửi yêu cầu cấp phát!')
    isRequestModalVisible.value = false
  } catch {
    message.error('Lỗi khi gửi yêu cầu!')
  } finally {
    requestSubmitting.value = false
  }
}

const handleDelete = (id) => {
  Modal.confirm({
    title: 'Xóa vật tư',
    content: 'Bạn chắc chắn muốn xóa vật tư này?',
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try {
        await consumableApi.delete(id)
        message.success('Đã xóa vật tư!')
        fetchData()
      } catch {
        message.error('Lỗi khi xóa vật tư!')
      }
    }
  })
}
</script>

<style scoped>
.table-actions {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 16px;
}
</style>


