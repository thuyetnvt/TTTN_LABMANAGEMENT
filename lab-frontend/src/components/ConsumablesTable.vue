<template>
  <div>
    <div class="table-actions">
      <div class="left-actions">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Tìm mã, tên vật tư..." style="width: 260px" @search="applyFilters" />
        <a-select v-model:value="stockFilter" allow-clear placeholder="Tình trạng tồn" style="width: 170px" @change="applyFilters">
          <a-select-option value="AVAILABLE">Đủ dùng</a-select-option>
          <a-select-option value="LOW_STOCK">Cần nhập thêm</a-select-option>
        </a-select>
      </div>
      <a-button v-if="isManagerRole(role)" type="primary" @click="showAddModal">+ Thêm vật tư</a-button>
    </div>

    <div class="consumables-desktop-table">
      <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: tableScrollX }" :pagination="tablePagination" @change="handleTableChange">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'quantity'">
          <span :style="{ color: availableStock(record) <= record.minQuantity ? '#dc2626' : '#16a34a', fontWeight: 700 }">
            {{ isManagerRole(role) ? record.quantity : availableStock(record) }}
          </span>
        </template>
        <template v-else-if="column.key === 'availableQuantity'">
          <strong :style="{ color: availableStock(record) <= record.minQuantity ? '#dc2626' : '#16a34a' }">{{ availableStock(record) }}</strong>
        </template>
        <template v-else-if="column.key === 'entryDate'">
          {{ formatVietnamDate(record.entryDate, '') }}
        </template>
        <template v-else-if="column.key === 'expiryDate'">
          {{ formatVietnamDate(record.expiryDate, 'Không áp dụng') }}
        </template>
        <template v-else-if="column.key === 'status'">
          <a-tag :color="availableStock(record) <= record.minQuantity ? 'red' : 'green'">
            {{ availableStock(record) <= record.minQuantity ? 'Cần nhập thêm' : 'Đủ dùng' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space class="table-action-buttons">
            <a-tooltip v-if="isBorrowerRole(role)" title="Yêu cầu cấp phát">
              <a-button type="link" size="small" aria-label="Yêu cầu cấp phát" @click="showRequestModal(record)">
                <template #icon><ShoppingCartOutlined /></template>
              </a-button>
            </a-tooltip>
            <a-tooltip v-if="isManagerRole(role)" title="Xem lịch sử vật tư">
              <a-button type="link" size="small" aria-label="Xem lịch sử vật tư" @click="showHistoryModal(record)">
                <template #icon><HistoryOutlined /></template>
              </a-button>
            </a-tooltip>
            <a-tooltip v-if="isManagerRole(role)" title="Quản lý lô vật tư">
              <a-button type="link" size="small" aria-label="Quản lý lô vật tư" @click="showLotsModal(record)">
                <template #icon><DatabaseOutlined /></template>
              </a-button>
            </a-tooltip>
            <a-tooltip v-if="isManagerRole(role)" title="Sửa vật tư">
              <a-button type="link" size="small" aria-label="Sửa vật tư" @click="showEditModal(record)">
                <template #icon><EditOutlined /></template>
              </a-button>
            </a-tooltip>
            <a-tooltip v-if="isAdminRole(role)" title="Xóa vật tư">
              <a-button type="link" danger size="small" aria-label="Xóa vật tư" @click="handleDelete(record.id)">
                <template #icon><DeleteOutlined /></template>
              </a-button>
            </a-tooltip>
          </a-space>
        </template>
      </template>
      </a-table>
    </div>

    <a-list v-if="dataSource.length" class="consumables-mobile-list" :data-source="dataSource" :pagination="false">
      <template #renderItem="{ item }">
        <a-list-item class="consumable-mobile-item">
          <div class="consumable-mobile-main">
            <div class="consumable-mobile-heading">
              <strong>{{ item.name }}</strong>
              <a-tag color="orange">{{ item.code || 'Chưa có mã' }}</a-tag>
            </div>
            <dl class="consumable-mobile-details">
              <div><dt>Danh mục</dt><dd>{{ item.categoryName || '—' }}</dd></div>
              <div v-if="isManagerRole(role)"><dt>Tổng tồn</dt><dd>{{ item.quantity }} {{ item.unit }}</dd></div>
              <div v-if="isManagerRole(role)"><dt>Đang giữ</dt><dd>{{ item.reservedQuantity || 0 }} {{ item.unit }}</dd></div>
              <div><dt>Khả dụng</dt><dd>{{ availableStock(item) }} {{ item.unit }}</dd></div>
              <div><dt>Tồn tối thiểu</dt><dd>{{ item.minQuantity }} {{ item.unit }}</dd></div>
              <div v-if="item.responsiblePerson"><dt>Người phụ trách</dt><dd>{{ item.responsiblePerson }}</dd></div>
            </dl>
            <a-tag :color="availableStock(item) <= item.minQuantity ? 'red' : 'green'">
              {{ availableStock(item) <= item.minQuantity ? 'Cần nhập thêm' : 'Đủ dùng' }}
            </a-tag>
          </div>
          <div class="consumable-mobile-actions">
            <a-button v-if="isBorrowerRole(role)" type="primary" ghost size="small" @click="showRequestModal(item)">Yêu cầu cấp phát</a-button>
            <template v-if="isManagerRole(role)">
              <a-button type="link" size="small" @click="showHistoryModal(item)">Lịch sử</a-button>
              <a-button type="link" size="small" @click="showLotsModal(item)">Quản lý lô</a-button>
              <a-button type="link" size="small" @click="showEditModal(item)">Sửa</a-button>
            </template>
            <a-button v-if="isAdminRole(role)" type="link" danger size="small" @click="handleDelete(item.id)">Xóa</a-button>
          </div>
        </a-list-item>
      </template>
    </a-list>
    <a-pagination
      v-if="tablePagination.total > 0"
      class="consumables-mobile-pagination"
      :current="tablePagination.current"
      :page-size="tablePagination.pageSize"
      :total="tablePagination.total"
      :show-size-changer="true"
      :page-size-options="tablePagination.pageSizeOptions"
      @change="handleMobilePageChange"
      @showSizeChange="handleMobilePageChange"
    />
    <a-empty v-else-if="!loading" class="consumables-mobile-empty" description="Chưa có vật tư" />

    <a-modal v-model:open="isFormVisible" :title="isEditMode ? 'Sửa vật tư' : 'Thêm vật tư'" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting" width="800px" wrapClassName="responsive-modal">
      <a-form layout="vertical">
        <a-alert
          v-if="isEditMode"
          type="info"
          show-icon
          message="Số lượng tồn và thông tin nhập kho được điều chỉnh trong chức năng Quản lý lô."
          style="margin-bottom: 16px"
        />
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
            <a-form-item :label="isEditMode ? 'Tổng tồn kho' : 'Số lượng nhập ban đầu'" required>
              <a-input-number v-model:value="formData.quantity" style="width: 100%" :min="0" :disabled="isEditMode" />
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
          <a-col v-if="!isEditMode" :xs="24" :sm="12">
            <a-form-item label="Ngày nhập">
              <a-date-picker v-model:value="formData.entryDate" style="width: 100%" />
            </a-form-item>
          </a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12">
            <a-form-item label="Số hóa đơn">
              <a-input v-model:value="formData.invoiceNumber" />
            </a-form-item>
          </a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12"><a-form-item label="Nhà cung cấp"><a-input v-model:value="formData.supplier" /></a-form-item></a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12"><a-form-item label="Giá nhập mỗi đơn vị"><a-input-number v-model:value="formData.unitCost" :min="0" style="width: 100%" /></a-form-item></a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12"><a-form-item label="Vị trí lưu"><a-input v-model:value="formData.storageLocation" /></a-form-item></a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12"><a-form-item label="Số lô"><a-input v-model:value="formData.lotNumber" placeholder="Bắt buộc khi có số lượng" /></a-form-item></a-col>
          <a-col v-if="!isEditMode" :xs="24" :sm="12"><a-form-item label="Hạn sử dụng"><a-date-picker v-model:value="formData.expiryDate" style="width: 100%" /></a-form-item></a-col>
        </a-row>
      </a-form>
    </a-modal>

    <a-modal v-model:open="isRequestModalVisible" title="Yêu cầu cấp phát vật tư" @ok="submitRequest" @cancel="isRequestModalVisible = false" okText="Gửi yêu cầu" cancelText="Hủy" :confirmLoading="requestSubmitting">
      <a-form layout="vertical">
        <a-form-item label="Vật tư">
          <strong>{{ currentRequestConsumable?.name }}</strong>
        </a-form-item>
        <a-form-item label="Số lượng" required>
          <a-input-number v-model:value="requestForm.quantity" style="width: 100%" :min="1" :max="availableStock(currentRequestConsumable)" />
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
        :pagination="historyPagination"
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

    <a-modal
      v-model:open="isLotsVisible"
      :title="`Quản lý lô: ${currentLotConsumable?.name || ''}`"
      width="980px"
      :footer="null"
      @cancel="isLotsVisible = false"
    >
      <div class="lot-toolbar">
        <div>
          Tổng tồn: <strong>{{ currentLotConsumable?.quantity || 0 }}</strong>
          <span v-if="currentLotConsumable?.reservedQuantity" class="reserved-stock">
            · Đang giữ: {{ currentLotConsumable.reservedQuantity }}
          </span>
        </div>
        <a-button type="primary" @click="showAddLotModal">+ Nhập lô mới</a-button>
      </div>
      <a-table
        :dataSource="lotData"
        :columns="lotColumns"
        :loading="lotsLoading"
        rowKey="id"
        size="small"
        bordered
        :pagination="lotPagination"
        :scroll="{ x: 1110 }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'entryDate'">{{ formatDate(record.entryDate) }}</template>
          <template v-else-if="column.key === 'expiryDate'">
            <a-tag :color="record.isExpired ? 'red' : 'default'">
              {{ record.expiryDate ? formatDate(record.expiryDate) : 'Không áp dụng' }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'unitCost'">{{ formatCurrency(record.unitCost) }}</template>
          <template v-else-if="column.key === 'action'">
            <a-button type="link" size="small" @click="showEditLotModal(record)">Điều chỉnh</a-button>
          </template>
        </template>
      </a-table>
    </a-modal>

    <a-modal
      v-model:open="isLotFormVisible"
      :title="isLotEditMode ? 'Điều chỉnh lô vật tư' : 'Nhập lô vật tư mới'"
      okText="Lưu"
      cancelText="Hủy"
      :confirmLoading="lotSubmitting"
      @ok="submitLotForm"
    >
      <a-form layout="vertical">
        <a-form-item label="Số lô" required><a-input v-model:value="lotForm.lotNumber" /></a-form-item>
        <a-form-item :label="isLotEditMode ? 'Số lượng còn lại' : 'Số lượng nhập'" required>
          <a-input-number v-model:value="lotForm.quantity" :min="isLotEditMode ? 0 : 1" :precision="0" style="width: 100%" />
        </a-form-item>
        <a-row :gutter="12">
          <a-col :span="12"><a-form-item label="Ngày nhập"><a-date-picker v-model:value="lotForm.entryDate" style="width: 100%" /></a-form-item></a-col>
          <a-col :span="12"><a-form-item label="Hạn sử dụng"><a-date-picker v-model:value="lotForm.expiryDate" style="width: 100%" /></a-form-item></a-col>
          <a-col :span="12"><a-form-item label="Nhà cung cấp"><a-input v-model:value="lotForm.supplier" /></a-form-item></a-col>
          <a-col :span="12"><a-form-item label="Số hóa đơn"><a-input v-model:value="lotForm.invoiceNumber" /></a-form-item></a-col>
          <a-col :span="12"><a-form-item label="Đơn giá"><a-input-number v-model:value="lotForm.unitCost" :min="0" style="width: 100%" /></a-form-item></a-col>
          <a-col :span="12"><a-form-item label="Vị trí lưu"><a-input v-model:value="lotForm.storageLocation" /></a-form-item></a-col>
        </a-row>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import dayjs from 'dayjs'
import { message, Modal } from 'ant-design-vue'
import { consumableApi } from '../api/consumableApi'
import { consumableRequestApi } from '../api/consumableRequestApi'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isBorrowerRole, isManagerRole } from '../constants/business'
import { DatabaseOutlined, DeleteOutlined, EditOutlined, HistoryOutlined, ShoppingCartOutlined } from '@ant-design/icons-vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDate, formatVietnamDateTime as formatVietnamDateTimeValue } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})
const historyPagination = createTablePagination()
const lotPagination = createTablePagination()

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const route = useRoute()

const dataSource = ref([])
const categories = ref([])
const loading = ref(false)
const submitting = ref(false)
const searchQuery = ref('')
const stockFilter = ref(undefined)
const availableStock = record => Number(record?.availableQuantity ?? Math.max(0, Number(record?.quantity || 0) - Number(record?.reservedQuantity || 0)))

const columns = computed(() => {
  const commonColumns = [
  { title: 'Mã vật tư', dataIndex: 'code', key: 'code', width: 150 },
  { title: 'Tên vật tư', dataIndex: 'name', key: 'name', width: 240 },
  { title: 'Danh mục', dataIndex: 'categoryName', key: 'categoryName', width: 140 },
  { title: 'Đơn vị', dataIndex: 'unit', key: 'unit', width: 100 },
  { title: isManagerRole(role.value) ? 'Tổng tồn' : 'Khả dụng', dataIndex: 'quantity', key: 'quantity', align: 'center', width: 110 },
  { title: 'Tồn tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity', align: 'center', width: 120 }
  ]
  const managerColumns = isManagerRole(role.value) ? [
    { title: 'Đang giữ', dataIndex: 'reservedQuantity', key: 'reservedQuantity', align: 'center', width: 100 },
    { title: 'Khả dụng', dataIndex: 'availableQuantity', key: 'availableQuantity', align: 'center', width: 100 },
    { title: 'Số lô', dataIndex: 'lotCount', key: 'lotCount', align: 'center', width: 90 },
    { title: 'Người chịu trách nhiệm', dataIndex: 'responsiblePerson', key: 'responsiblePerson', width: 180 }
  ] : []
  return [...commonColumns, ...managerColumns,
  { title: 'Trạng thái', key: 'status', align: 'center', width: 120 },
  { 
    title: 'Hành động', 
    key: 'action', 
    align: 'center', 
    className: 'table-sticky-action-column',
    customCell: () => ({ class: 'table-sticky-action-column' }),
    width: isAdminRole(role.value) ? 170 : (isManagerRole(role.value) ? 140 : 70)
  }
  ]
})

const tableScrollX = computed(() => {
  const commonWidth = 860
  const managerWidth = isManagerRole(role.value) ? 470 : 0
  const actionWidth = isAdminRole(role.value) ? 170 : (isManagerRole(role.value) ? 140 : 70)

  return commonWidth + managerWidth + 120 + actionWidth
})

const lotColumns = [
  { title: 'Số lô', dataIndex: 'lotNumber', key: 'lotNumber', width: 150 },
  { title: 'Ban đầu', dataIndex: 'initialQuantity', key: 'initialQuantity', align: 'center', width: 90 },
  { title: 'Còn lại', dataIndex: 'quantity', key: 'quantity', align: 'center', width: 90 },
  { title: 'Ngày nhập', key: 'entryDate', width: 115 },
  { title: 'Hạn sử dụng', key: 'expiryDate', width: 135 },
  { title: 'Nhà cung cấp', dataIndex: 'supplier', key: 'supplier', width: 160 },
  { title: 'Đơn giá', key: 'unitCost', width: 120 },
  { title: 'Vị trí', dataIndex: 'storageLocation', key: 'storageLocation', width: 140 },
  { title: 'Hành động', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), align: 'center', width: 110 }
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
const isLotsVisible = ref(false)
const lotsLoading = ref(false)
const lotData = ref([])
const currentLotConsumable = ref(null)
const isLotFormVisible = ref(false)
const isLotEditMode = ref(false)
const currentLotId = ref(null)
const lotSubmitting = ref(false)

const emptyLotForm = () => ({
  lotNumber: '',
  quantity: 1,
  entryDate: dayjs(),
  expiryDate: null,
  supplier: '',
  invoiceNumber: '',
  unitCost: null,
  storageLocation: ''
})
const lotForm = ref(emptyLotForm())

onMounted(() => {
  stockFilter.value = typeof route.query.stock === 'string' ? route.query.stock : undefined
  fetchData()
  fetchCategories()
})

watch(() => route.query.stock, value => {
  stockFilter.value = typeof value === 'string' ? value : undefined
  applyFilters()
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
    const response = await consumableApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: stockFilter.value
    })
    dataSource.value = response.items || []
    tablePagination.total = response.total || 0
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

const fetchLots = async () => {
  if (!currentLotConsumable.value) return
  lotsLoading.value = true
  try {
    lotData.value = await consumableApi.getLots(currentLotConsumable.value.id) || []
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách lô vật tư.'))
  } finally {
    lotsLoading.value = false
  }
}

const applyFilters = () => {
  tablePagination.current = 1
  fetchData()
}

const handleTableChange = (pager) => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  fetchData()
}

const handleMobilePageChange = (page, pageSize) => {
  tablePagination.current = pageSize === tablePagination.pageSize ? page : 1
  tablePagination.pageSize = pageSize
  fetchData()
}

const showLotsModal = async record => {
  currentLotConsumable.value = record
  isLotsVisible.value = true
  await fetchLots()
}

const showAddLotModal = () => {
  isLotEditMode.value = false
  currentLotId.value = null
  lotForm.value = {
    ...emptyLotForm(),
    supplier: currentLotConsumable.value?.supplier || '',
    storageLocation: currentLotConsumable.value?.storageLocation || ''
  }
  isLotFormVisible.value = true
}

const showEditLotModal = record => {
  isLotEditMode.value = true
  currentLotId.value = record.id
  lotForm.value = {
    lotNumber: record.lotNumber,
    quantity: record.quantity,
    entryDate: record.entryDate ? dayjs(record.entryDate) : dayjs(),
    expiryDate: record.expiryDate ? dayjs(record.expiryDate) : null,
    supplier: record.supplier || '',
    invoiceNumber: record.invoiceNumber || '',
    unitCost: record.unitCost,
    storageLocation: record.storageLocation || ''
  }
  isLotFormVisible.value = true
}

const submitLotForm = async () => {
  if (!lotForm.value.lotNumber?.trim() || lotForm.value.quantity === null || lotForm.value.quantity === undefined) {
    message.warning('Vui lòng nhập số lô và số lượng hợp lệ.')
    return
  }
  if (!isLotEditMode.value && lotForm.value.quantity <= 0) {
    message.warning('Số lượng nhập phải lớn hơn 0.')
    return
  }

  const payload = {
    ...lotForm.value,
    lotNumber: lotForm.value.lotNumber.trim(),
    entryDate: lotForm.value.entryDate ? lotForm.value.entryDate.toISOString() : null,
    expiryDate: lotForm.value.expiryDate ? lotForm.value.expiryDate.toISOString() : null
  }
  lotSubmitting.value = true
  try {
    if (isLotEditMode.value) {
      await consumableApi.updateLot(currentLotConsumable.value.id, currentLotId.value, payload)
      message.success('Đã điều chỉnh lô và đồng bộ tổng tồn kho.')
    } else {
      await consumableApi.addLot(currentLotConsumable.value.id, payload)
      message.success('Đã nhập lô mới và cộng tồn kho.')
    }
    isLotFormVisible.value = false
    await fetchData()
    currentLotConsumable.value = dataSource.value.find(item => item.id === currentLotConsumable.value.id) || currentLotConsumable.value
    await fetchLots()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể lưu thông tin lô vật tư.'))
  } finally {
    lotSubmitting.value = false
  }
}

const formatDateTime = (value) => {
  return formatVietnamDateTimeValue(value, '')
}

const formatDate = value => formatVietnamDate(value)
const formatCurrency = value => value === null || value === undefined
  ? '—'
  : new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

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
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}

.left-actions { display: flex; flex-wrap: wrap; gap: 10px; }

.consumables-mobile-list,
.consumables-mobile-empty,
.consumables-mobile-pagination {
  display: none;
}

.lot-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.reserved-stock { color: #b45309; }

.consumables-desktop-table :deep(.ant-table-cell) {
  white-space: normal;
  overflow-wrap: anywhere;
}

.consumable-mobile-item {
  display: block;
  padding: 16px 4px;
}

.consumable-mobile-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.consumable-mobile-heading strong {
  min-width: 0;
  color: var(--color-ink);
  overflow-wrap: anywhere;
}

.consumable-mobile-details {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin: 14px 0;
}

.consumable-mobile-details div {
  min-width: 0;
}

.consumable-mobile-details dt {
  color: #64748b;
  font-size: 12px;
}

.consumable-mobile-details dd {
  margin: 2px 0 0;
  color: var(--color-ink);
  font-size: 13px;
  overflow-wrap: anywhere;
}

.consumable-mobile-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 6px;
  margin-top: 12px;
}

@media (max-width: 767px) {
  .consumables-desktop-table {
    display: none;
  }

  .consumables-mobile-list {
    display: block;
  }

  .consumables-mobile-empty {
    display: block;
  }

  .consumables-mobile-pagination {
    display: flex;
    justify-content: flex-end;
    margin-top: 16px;
  }

  .table-actions { align-items: stretch; flex-direction: column; }
  .left-actions > * { width: 100% !important; }
}

@media (max-width: 420px) {
  .consumable-mobile-heading {
    flex-direction: column;
    gap: 6px;
  }

  .consumable-mobile-details {
    grid-template-columns: 1fr;
  }

  .consumable-mobile-actions {
    justify-content: flex-start;
  }
}
</style>


