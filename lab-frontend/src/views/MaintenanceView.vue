<template>
  <div class="maintenance-container">
    <div class="toolbar">
      <h2>Lịch sử Bảo trì & Hiệu chuẩn</h2>
      <div class="toolbar-actions">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Thiết bị, nội dung..." style="width: 240px" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" style="width: 180px" @change="applyFilters">
          <a-select-option :value="STATUS.MAINTENANCE_IN_PROGRESS">Đang bảo trì</a-select-option>
          <a-select-option :value="STATUS.MAINTENANCE_COMPLETED">Đã hoàn tất</a-select-option>
        </a-select>
        <a-button type="primary" v-if="isManagerRole(role)" @click="showAddModal">+ Tạo phiếu bảo trì</a-button>
      </div>
    </div>
    
    <a-card :bordered="false" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05);">
      <div class="maintenance-desktop-table">
        <a-table :dataSource="dataSource" :columns="columns" :loading="loading" rowKey="id" bordered :scroll="{ x: 1450 }" :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'maintenanceDate'">
             {{ formatDate(record[column.key]) }}
          </template>
          <template v-else-if="column.key === 'cost'">
             {{ Number(record.cost || 0).toLocaleString('vi-VN') }} VNĐ
          </template>
          <template v-else-if="column.key === 'status'">
             <StatusBadge :status="record.status" type="maintenance" />
          </template>
          <template v-else-if="column.key === 'action'">
              <a-space class="table-action-buttons">
                <a-tooltip v-if="statusMatches(record.status, STATUS.MAINTENANCE_IN_PROGRESS)" title="Hoàn tất bảo trì">
                  <a-button
                    type="link"
                    size="small"
                    aria-label="Hoàn tất bảo trì"
                    @click="showCompleteModal(record)"
                  >
                    <template #icon><CheckCircleOutlined /></template>
                  </a-button>
                </a-tooltip>
                <a-tooltip v-if="isAdminRole(role) && statusMatches(record.status, STATUS.MAINTENANCE_COMPLETED)" title="Xóa phiếu bảo trì">
                  <a-button
                    type="link"
                    danger
                    class="table-delete-action"
                    aria-label="Xóa phiếu bảo trì"
                    :loading="deleteLoading && deletingId === record.id"
                    :disabled="deleteLoading"
                    @click="requestDelete(record.id)"
                  >
                    <template #icon><DeleteOutlined /></template>
                  </a-button>
                </a-tooltip>
              </a-space>
          </template>
        </template>
        </a-table>
      </div>

      <ResponsiveDataList
        class="maintenance-mobile-list"
        :items="dataSource"
        :loading="loading"
        :pagination="tablePagination"
        @change="handleTableChange"
        empty-description="Chưa có lịch sử bảo trì"
      >
        <template #default="{ item }">
          <div class="maintenance-mobile-card">
            <div class="maintenance-mobile-card-header">
              <strong>{{ item.device || 'Thiết bị chưa xác định' }}</strong>
              <StatusBadge :status="item.status" type="maintenance" />
            </div>
            <div class="maintenance-mobile-details">
              <div><span>Ngày thực hiện</span><strong>{{ formatDate(item.maintenanceDate) }}</strong></div>
              <div><span>Người thực hiện</span><strong>{{ item.performedBy || '—' }}</strong></div>
              <div><span>Chi phí</span><strong>{{ Number(item.cost || 0).toLocaleString('vi-VN') }} VNĐ</strong></div>
              <div><span>Nội dung</span><strong>{{ item.description || '—' }}</strong></div>
              <div><span>Kết quả</span><strong>{{ item.result || '—' }}</strong></div>
            </div>
            <div class="maintenance-mobile-actions">
              <a-button
                v-if="statusMatches(item.status, STATUS.MAINTENANCE_IN_PROGRESS)"
                type="primary"
                size="small"
                @click="showCompleteModal(item)"
              >
                Hoàn tất
              </a-button>
              <a-tooltip v-if="isAdminRole(role) && statusMatches(item.status, STATUS.MAINTENANCE_COMPLETED)" title="Xóa phiếu bảo trì">
                <a-button
                  type="link"
                  danger
                  class="table-delete-action"
                  aria-label="Xóa phiếu bảo trì"
                  :loading="deleteLoading && deletingId === item.id"
                  :disabled="deleteLoading"
                  @click="requestDelete(item.id)"
                >
                  <template #icon><DeleteOutlined /></template>
                </a-button>
              </a-tooltip>
            </div>
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>

    <ConfirmDialog
      :open="deleteDialogOpen"
      title="Xóa lịch sử bảo trì"
      message="Bạn có chắc chắn muốn xóa phiếu bảo trì này không?"
      ok-text="Xóa"
      ok-type="danger"
      :loading="deleteLoading"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />

    <a-modal v-model:open="isFormVisible" title="Thêm lịch sử bảo trì" @ok="submitForm" @cancel="isFormVisible = false" okText="Lưu" cancelText="Hủy" :confirmLoading="submitting" width="700px" wrapClassName="responsive-modal">
      <a-form layout="vertical">
        <a-row :gutter="16">
          <a-col :xs="24" :sm="12">
            <a-form-item label="Chọn thiết bị" required>
              <a-select v-model:value="formData.equipmentId" show-search :filter-option="false" :loading="lookupLoading" placeholder="Nhập tên, seri hoặc mã tài sản" @search="searchEquipmentOptions">
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
          <a-form-item label="Nhà cung cấp sửa chữa"><a-input v-model:value="formData.supplier" /></a-form-item>
          <a-form-item label="Checklist bảo trì"><a-textarea v-model:value="formData.checklist" :rows="3" placeholder="Mỗi dòng một hạng mục kiểm tra" /></a-form-item>
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
        <a-form-item label="Kết quả checklist"><a-textarea v-model:value="completeChecklistResult" :rows="3" placeholder="Đạt/không đạt theo từng hạng mục" /></a-form-item>
        <a-form-item label="Linh kiện/vật tư đã sử dụng">
          <a-space v-for="(part, index) in completeParts" :key="index" style="display: flex; margin-bottom: 6px">
            <a-select v-model:value="part.consumableId" show-search :filter-option="false" :loading="lookupLoading" style="width: 230px" @search="searchConsumableOptions"><a-select-option v-for="item in consumables" :key="item.id" :value="item.id" :label="item.name">{{ item.name }} (còn {{ item.quantity }})</a-select-option></a-select>
            <a-input-number v-model:value="part.quantity" :min="1" style="width: 90px" />
            <a-button danger @click="completeParts.splice(index, 1)">Xóa</a-button>
          </a-space>
          <a-button size="small" @click="completeParts.push({ consumableId: null, quantity: 1, unitCost: null, note: '' })">+ Thêm vật tư</a-button>
        </a-form-item>
        <a-form-item label="Ảnh/file kết quả">
          <a-upload :before-upload="selectMaintenanceEvidence" :show-upload-list="false" accept=".pdf,.jpg,.jpeg,.png,.webp,.doc,.docx"><a-button>Chọn file</a-button></a-upload>
          <span v-if="completeEvidenceFile" class="muted">{{ completeEvidenceFile.name }}</span>
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { reactive, ref, computed, onMounted } from 'vue'
import { message, Upload } from 'ant-design-vue'
import { DeleteOutlined, CheckCircleOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import { maintenanceApi } from '../api/maintenanceApi'
import { equipmentApi } from '../api/equipmentApi'
import { consumableApi } from '../api/consumableApi'
import StatusBadge from '../components/StatusBadge.vue'
import ConfirmDialog from '../components/ConfirmDialog.vue'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { STATUS, isAdminRole, isManagerRole, statusMatches } from '../constants/business'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { formatVietnamDate as formatDate } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const authStore = useAuthStore()
const role = computed(() => authStore.role)

const dataSource = ref([])
const equipments = ref([])
const consumables = ref([])
const loading = ref(false)
const lookupLoading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const submitting = ref(false)
const isFormVisible = ref(false)
const isCompleteVisible = ref(false)
const completing = ref(false)
const completingRecordId = ref(null)
const deleteDialogOpen = ref(false)
const deleteLoading = ref(false)
const deletingId = ref(null)
const completeResult = ref('')
const completeStatus = ref(STATUS.AVAILABLE)
const completeChecklistResult = ref('')
const completeParts = ref([])
const completeEvidenceFile = ref(null)

const formData = ref({
  equipmentId: null,
  maintenanceDate: null,
  description: '',
  performedBy: '',
  cost: 0,
  supplier: '',
  checklist: ''
})

const columns = [
  { title: 'Thiết bị', dataIndex: 'device', key: 'device', width: 180 },
  { title: 'Ngày thực hiện', dataIndex: 'maintenanceDate', key: 'maintenanceDate', width: 140 },
  { title: 'Nội dung', dataIndex: 'description', key: 'description', width: 320 },
  { title: 'Người thực hiện', dataIndex: 'performedBy', key: 'performedBy', width: 170 },
  { title: 'Chi phí', dataIndex: 'cost', key: 'cost', width: 120 },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 160 },
  { title: 'Kết quả', dataIndex: 'result', key: 'result', width: 280 },
  { title: 'Hành động', key: 'action', align: 'center', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 120 }
]


onMounted(() => {
  fetchData()
})

const fetchData = async () => {
  loading.value = true
  try {
    const res = await maintenanceApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    dataSource.value = res.items || []
    tablePagination.total = res.total || 0
  } catch (error) {
    message.error('Lỗi tải lịch sử bảo trì!')
  } finally {
    loading.value = false
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

const searchEquipmentOptions = async value => {
  lookupLoading.value = true
  try { equipments.value = await equipmentApi.lookup({ search: value?.trim() || undefined, limit: 50 }) || [] }
  finally { lookupLoading.value = false }
}

const searchConsumableOptions = async value => {
  lookupLoading.value = true
  try { consumables.value = await consumableApi.lookup({ search: value?.trim() || undefined, limit: 50 }) || [] }
  finally { lookupLoading.value = false }
}

const showAddModal = async () => {
  try {
    await searchEquipmentOptions('')
    formData.value = { equipmentId: null, maintenanceDate: null, description: '', performedBy: '', cost: 0, supplier: '', checklist: '' }
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

const requestDelete = (id) => {
  if (deleteLoading.value) return
  deletingId.value = id
  deleteDialogOpen.value = true
}

const cancelDelete = () => {
  if (deleteLoading.value) return
  deleteDialogOpen.value = false
  deletingId.value = null
}

const confirmDelete = async () => {
  if (!deletingId.value || deleteLoading.value) return
  deleteLoading.value = true
  try {
    await maintenanceApi.delete(deletingId.value)
    message.success('Đã xóa thành công!')
    deleteDialogOpen.value = false
    await fetchData()
  } catch (error) {
    message.error('Lỗi khi xóa!')
  } finally {
    deleteLoading.value = false
    deletingId.value = null
  }
}

const showCompleteModal = async (record) => {
  completingRecordId.value = record.id
  completeResult.value = ''
  completeStatus.value = STATUS.AVAILABLE
  completeChecklistResult.value = ''
  completeParts.value = []
  completeEvidenceFile.value = null
  await searchConsumableOptions('').catch(() => { consumables.value = [] })
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
      nextEquipmentStatus: completeStatus.value,
      checklistResult: completeChecklistResult.value.trim(),
      parts: completeParts.value.filter(part => part.consumableId && part.quantity > 0)
    })
    if (completeEvidenceFile.value) await maintenanceApi.uploadEvidence(completingRecordId.value, completeEvidenceFile.value)
    message.success('Đã hoàn tất phiếu bảo trì và cập nhật trạng thái thiết bị!')
    isCompleteVisible.value = false
    fetchData()
  } catch (error) {
    message.error(error?.response?.data?.message || 'Không thể hoàn tất bảo trì!')
  } finally {
    completing.value = false
  }
}

const selectMaintenanceEvidence = (file) => {
  const extension = file.name.split('.').pop()?.toLowerCase()
  if (!['pdf', 'jpg', 'jpeg', 'png', 'webp', 'doc', 'docx'].includes(extension) || file.size > 10 * 1024 * 1024) {
    message.error('File phải là PDF, Word hoặc ảnh và không quá 10 MB.')
    return Upload.LIST_IGNORE
  }
  completeEvidenceFile.value = file
  return false
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
.toolbar-actions { display: flex; align-items: center; justify-content: flex-end; flex-wrap: wrap; gap: 10px; }
h2 {
  margin: 0;
  font-weight: 600;
  color: #1f1f1f;
}
.maintenance-desktop-table {
  display: block;
}
.maintenance-mobile-list {
  display: none;
}
.maintenance-mobile-card {
  display: grid;
  gap: 14px;
}
.maintenance-mobile-card-header,
.maintenance-mobile-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}
.maintenance-mobile-card-header strong {
  min-width: 0;
  overflow-wrap: anywhere;
}
.maintenance-mobile-details {
  display: grid;
  gap: 10px;
}
.maintenance-mobile-details div {
  display: grid;
  gap: 2px;
}
.maintenance-mobile-details span {
  color: var(--color-muted);
  font-size: 12px;
}
.maintenance-mobile-details strong {
  overflow-wrap: anywhere;
}
@media (max-width: 767px) {
  .maintenance-desktop-table {
    display: none;
  }
  .maintenance-mobile-list {
    display: grid;
  }
  .toolbar {
    align-items: flex-start;
    flex-direction: column;
    gap: 12px;
  }
  .toolbar-actions { width: 100%; }
  .toolbar-actions > * { width: 100% !important; }
}
</style>


