<template>
  <div class="schedule-page">
    <div class="toolbar">
      <div>
        <h2>Bảo trì định kỳ</h2>
        <p class="muted">Theo dõi hạn đến và tạo phiếu bảo trì theo kế hoạch.</p>
      </div>
      <div class="toolbar-actions">
        <a-input-search v-model:value="searchQuery" allow-clear placeholder="Thiết bị, kế hoạch..." style="width: 240px" @search="applyFilters" />
        <a-select v-model:value="statusFilter" allow-clear placeholder="Trạng thái" style="width: 160px" @change="applyFilters">
          <a-select-option value="DUE">Đã đến hạn</a-select-option>
          <a-select-option value="ACTIVE">Đang bật</a-select-option>
          <a-select-option value="INACTIVE">Tạm tắt</a-select-option>
        </a-select>
        <a-button type="primary" @click="openCreate">+ Tạo kế hoạch</a-button>
      </div>
    </div>

    <a-card :bordered="false">
      <a-table class="desktop-table" :data-source="schedules" :columns="columns" :loading="loading" row-key="id" bordered :pagination="tablePagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'nextDueAt'">
            <a-tag :color="record.isDue ? 'red' : 'blue'">{{ formatDate(record.nextDueAt) }}</a-tag>
          </template>
          <template v-if="column.key === 'isActive'">
            <a-tag :color="record.isActive ? 'green' : 'default'">{{ record.isActive ? 'Đang bật' : 'Tạm tắt' }}</a-tag>
          </template>
          <template v-if="column.key === 'action'">
            <a-space class="table-action-buttons">
              <a-tooltip v-if="record.isActive" title="Tạo phiếu bảo trì">
                <a-button
                  size="small"
                  type="link"
                  class="schedule-action-button"
                  aria-label="Tạo phiếu bảo trì"
                  @click="generate(record)"
                >
                  <template #icon><FileAddOutlined /></template>
                </a-button>
              </a-tooltip>
              <a-tooltip title="Sửa kế hoạch bảo trì">
                <a-button type="link" size="small" class="schedule-action-button" aria-label="Sửa kế hoạch bảo trì" @click="openEdit(record)">
                  <template #icon><EditOutlined /></template>
                </a-button>
              </a-tooltip>
              <a-tooltip v-if="isAdminRole(role)" title="Xóa kế hoạch bảo trì">
                <a-button type="link" size="small" class="schedule-action-button" aria-label="Xóa kế hoạch bảo trì" @click="remove(record)">
                  <template #icon><DeleteOutlined /></template>
                </a-button>
              </a-tooltip>
            </a-space>
          </template>
        </template>
      </a-table>
      <ResponsiveDataList :items="schedules" :loading="loading" :pagination="tablePagination" empty-description="Chưa có kế hoạch bảo trì" @change="handleTableChange">
        <template #default="{ item }">
          <div class="mobile-schedule-heading"><strong>{{ item.device }}</strong><a-tag :color="item.isActive ? 'green' : 'default'">{{ item.isActive ? 'Đang bật' : 'Tạm tắt' }}</a-tag></div>
          <div class="mobile-schedule-name">{{ item.name }}</div>
          <dl class="mobile-schedule-details">
            <div><dt>Chu kỳ</dt><dd>{{ item.intervalDays }} {{ ({ DAY: 'ngày', WEEK: 'tuần', MONTH: 'tháng', QUARTER: 'quý', YEAR: 'năm' })[item.intervalUnit] || 'ngày' }}</dd></div>
            <div><dt>Hạn kế tiếp</dt><dd><a-tag :color="item.isDue ? 'red' : 'blue'">{{ formatDate(item.nextDueAt) }}</a-tag></dd></div>
          </dl>
          <div class="mobile-schedule-actions">
            <a-button v-if="item.isActive" @click="generate(item)"><template #icon><FileAddOutlined /></template>Tạo phiếu</a-button>
            <a-button @click="openEdit(item)"><template #icon><EditOutlined /></template>Sửa</a-button>
            <a-button v-if="isAdminRole(role)" danger @click="remove(item)"><template #icon><DeleteOutlined /></template>Xóa</a-button>
          </div>
        </template>
      </ResponsiveDataList>
    </a-card>

    <a-modal
      v-model:open="modalOpen"
      :title="editing ? 'Sửa kế hoạch' : 'Tạo kế hoạch bảo trì'"
      :width="900"
      :confirm-loading="saving"
      ok-text="Lưu"
      cancel-text="Hủy"
      wrap-class-name="maintenance-form-modal"
      @ok="save"
    >
      <a-form layout="vertical">
        <div class="maintenance-form-grid">
          <div class="form-column">
            <a-form-item label="Thiết bị" required>
              <a-select v-model:value="form.equipmentId" show-search :filter-option="false" :loading="lookupLoading" :disabled="!!editing" @search="searchEquipmentOptions">
                <a-select-option v-for="equipment in equipments" :key="equipment.id" :value="equipment.id" :label="`${equipment.name} ${equipment.serial}`">{{ equipment.name }} — {{ equipment.serial }}</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="Tên kế hoạch" required>
              <a-input v-model:value="form.name" placeholder="VD: Hiệu chuẩn hàng quý" />
            </a-form-item>
            <a-form-item label="Chu kỳ" required>
              <a-space class="maintenance-cycle-fields">
                <a-input-number v-model:value="form.intervalDays" :min="1" :max="3650" />
                <a-select v-model:value="form.intervalUnit">
                  <a-select-option value="DAY">Ngày</a-select-option>
                  <a-select-option value="WEEK">Tuần</a-select-option>
                  <a-select-option value="MONTH">Tháng</a-select-option>
                  <a-select-option value="QUARTER">Quý</a-select-option>
                  <a-select-option value="YEAR">Năm</a-select-option>
                </a-select>
              </a-space>
            </a-form-item>
            <a-form-item label="Hạn kế tiếp">
              <a-input v-model:value="form.nextDueAt" type="date" />
            </a-form-item>
          </div>

          <div class="form-column">
            <a-form-item label="Ghi chú">
              <a-textarea v-model:value="form.notes" :rows="5" placeholder="Ghi chú cho kế hoạch bảo trì" />
            </a-form-item>
            <a-form-item label="Checklist bảo trì">
              <a-textarea v-model:value="form.checklist" :rows="5" placeholder="Mỗi dòng một hạng mục kiểm tra" />
            </a-form-item>
            <a-form-item v-if="editing" label="Trạng thái">
              <a-switch v-model:checked="form.isActive" checked-children="Bật" un-checked-children="Tắt" />
            </a-form-item>
          </div>
        </div>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { DeleteOutlined, EditOutlined, FileAddOutlined } from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole } from '../constants/business'
import { equipmentApi } from '../api/equipmentApi'
import { maintenanceScheduleApi } from '../api/maintenanceScheduleApi'
import ResponsiveDataList from '../components/ResponsiveDataList.vue'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import { formatVietnamDate, formatVietnamDateInput, vietnamDateInputToUtc } from '../utils/dateTime'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0
})

const authStore = useAuthStore()
const role = computed(() => authStore.role)
const schedules = ref([])
const equipments = ref([])
const loading = ref(false)
const lookupLoading = ref(false)
const searchQuery = ref('')
const statusFilter = ref(undefined)
const saving = ref(false)
const modalOpen = ref(false)
const editing = ref(null)
const form = ref({ equipmentId: null, name: '', intervalDays: 90, intervalUnit: 'DAY', nextDueAt: '', notes: '', checklist: '', isActive: true })
const columns = [
  { title: 'Thiết bị', dataIndex: 'device', key: 'device' },
  { title: 'Kế hoạch', dataIndex: 'name', key: 'name' },
  { title: 'Chu kỳ', dataIndex: 'intervalDays', key: 'intervalDays', customRender: ({ record }) => `${record.intervalDays} ${({ DAY: 'ngày', WEEK: 'tuần', MONTH: 'tháng', QUARTER: 'quý', YEAR: 'năm' })[record.intervalUnit] || 'ngày'}` },
  { title: 'Hạn kế tiếp', dataIndex: 'nextDueAt', key: 'nextDueAt' },
  { title: 'Trạng thái', dataIndex: 'isActive', key: 'isActive' },
  { title: 'Hành động', key: 'action', width: 150, align: 'center' }
]

const toDateInput = value => formatVietnamDateInput(value)

const load = async () => {
  loading.value = true
  try {
    const response = await maintenanceScheduleApi.getPaged({
      page: tablePagination.current,
      pageSize: tablePagination.pageSize,
      search: searchQuery.value.trim() || undefined,
      status: statusFilter.value
    })
    schedules.value = response.items || []
    tablePagination.total = response.total || 0
  } catch { message.error('Không tải được kế hoạch bảo trì.') } finally { loading.value = false }
}

const applyFilters = () => { tablePagination.current = 1; load() }
const handleTableChange = pager => {
  tablePagination.current = pager.pageSize === tablePagination.pageSize ? pager.current : 1
  tablePagination.pageSize = pager.pageSize
  load()
}

const searchEquipmentOptions = async value => {
  lookupLoading.value = true
  try { equipments.value = await equipmentApi.lookup({ search: value?.trim() || undefined, limit: 50 }) || [] }
  finally { lookupLoading.value = false }
}

const openCreate = async () => {
  await searchEquipmentOptions('')
  editing.value = null
  form.value = { equipmentId: null, name: '', intervalDays: 90, intervalUnit: 'DAY', nextDueAt: '', notes: '', checklist: '', isActive: true }
  modalOpen.value = true
}

const openEdit = (record) => {
  equipments.value = [{ id: record.equipmentId, name: record.device, serial: record.serial }]
  editing.value = record
  form.value = { equipmentId: record.equipmentId, name: record.name, intervalDays: record.intervalDays, intervalUnit: record.intervalUnit || 'DAY', nextDueAt: toDateInput(record.nextDueAt), notes: record.notes || '', checklist: record.checklist || '', isActive: record.isActive }
  modalOpen.value = true
}

const save = async () => {
  if (!form.value.equipmentId || !form.value.name?.trim() || !form.value.intervalDays) return message.warning('Vui lòng nhập đủ thông tin.')
  saving.value = true
  try {
    const payload = { ...form.value, name: form.value.name.trim(), nextDueAt: vietnamDateInputToUtc(form.value.nextDueAt) }
    if (editing.value) await maintenanceScheduleApi.update(editing.value.id, payload)
    else await maintenanceScheduleApi.create(payload)
    message.success('Đã lưu kế hoạch bảo trì.')
    modalOpen.value = false
    await load()
  } catch (error) { message.error(error?.response?.data?.message || 'Không thể lưu kế hoạch.') } finally { saving.value = false }
}

const generate = (record) => Modal.confirm({ title: 'Tạo phiếu bảo trì?', content: `Tạo phiếu cho ${record.device} theo kế hoạch “${record.name}”?`, okText: 'Tạo', cancelText: 'Hủy', onOk: async () => {
  try { await maintenanceScheduleApi.generate(record.id); message.success('Đã tạo phiếu bảo trì.'); await load() } catch (error) { message.error(error?.response?.data?.message || 'Không thể tạo phiếu.') }
} })

const remove = (record) => Modal.confirm({ title: 'Xóa kế hoạch?', content: record.name, okType: 'danger', okText: 'Xóa', cancelText: 'Hủy', onOk: async () => {
  try { await maintenanceScheduleApi.delete(record.id); message.success('Đã xóa kế hoạch.'); await load() } catch { message.error('Không thể xóa kế hoạch.') }
} })

onMounted(load)
</script>

<style scoped>
.schedule-page { padding: 0; }
.toolbar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.toolbar-actions { display: flex; align-items: center; justify-content: flex-end; flex-wrap: wrap; gap: 10px; }
h2 { margin: 0; }
.muted { margin: 5px 0 0; color: #777; }

:deep(.schedule-action-button) {
  min-width: 36px;
  height: 36px;
  padding: 0 8px;
  border: 0;
  box-shadow: none;
  color: var(--color-primary);
}

:deep(.schedule-action-button:hover),
:deep(.schedule-action-button:focus-visible) {
  color: var(--color-primary-hover);
  background: rgba(217, 119, 87, 0.12);
}

:global(.maintenance-form-modal .ant-modal) {
  max-width: calc(100vw - 32px);
}

:global(.maintenance-form-modal .ant-modal-footer) {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.maintenance-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20px 24px;
  align-items: start;
}

.form-column {
  min-width: 0;
}
.mobile-schedule-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.mobile-schedule-heading strong { color: var(--color-ink); font-size: 15px; }
.mobile-schedule-name { margin-top: 5px; color: var(--color-text-secondary); }
.mobile-schedule-details { display: grid; gap: 8px; margin: 12px 0; }
.mobile-schedule-details div { display: flex; justify-content: space-between; gap: 12px; }
.mobile-schedule-details dt { color: var(--color-text-secondary); }
.mobile-schedule-details dd { margin: 0; }
.mobile-schedule-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.mobile-schedule-actions :deep(.ant-btn) { flex: 1; }

.maintenance-cycle-fields {
  display: flex;
  width: 100%;
}

.maintenance-cycle-fields :deep(.ant-input-number) {
  flex: 1;
  min-width: 0;
}

.maintenance-cycle-fields :deep(.ant-select) {
  flex: 1;
  min-width: 0;
}

@media (max-width: 767px) {
  .desktop-table { display: none; }
  .toolbar { align-items: stretch; flex-direction: column; gap: 14px; }
  .toolbar-actions > * { width: 100% !important; }
  :global(.maintenance-form-modal .ant-modal) {
    max-width: calc(100vw - 32px);
  }

  .maintenance-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
