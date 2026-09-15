<template>
  <div class="locations-page">
    <div class="page-heading">
      <div>
        <h2>Danh sách vị trí tài sản</h2>
        <p>Quản lý các vị trí lưu trữ thiết bị và vật tư trong Phòng Lab IoT.</p>
      </div>
      <a-button type="primary" @click="openCreate">Thêm vị trí</a-button>
    </div>

    <a-card :bordered="false">
      <div class="location-filters">
        <a-input-search class="location-filter-control" v-model:value="searchQuery" allow-clear placeholder="Tìm mã, tên vị trí..." @search="applyFilters" />
        <a-select class="location-filter-control" v-model:value="typeFilter" allow-clear placeholder="Loại vị trí" @change="applyFilters">
          <a-select-option v-for="option in typeOptions" :key="option.value" :value="option.value">{{ option.label }}</a-select-option>
        </a-select>
        <a-select class="location-filter-control" v-model:value="activeFilter" allow-clear placeholder="Trạng thái" @change="applyFilters">
          <a-select-option value="ACTIVE">Đang sử dụng</a-select-option>
          <a-select-option value="INACTIVE">Ngừng sử dụng</a-select-option>
        </a-select>
      </div>
      <a-table bordered :data-source="filteredLocations" :columns="columns" :loading="loading" row-key="id" :scroll="{ x: 900 }" :pagination="tablePagination" @change="handleTableChange">
        <template #headerCell="{ column }">
          <TableColumnFilter
            v-if="column.filterType || column.sortable"
            :title="column.title"
            :type="column.filterType"
            :options="column.filterOptions"
            :filterable="Boolean(column.filterType)"
            :sortable="Boolean(column.sortable)"
            :sort-order="sortState.field === column.sortKey ? sortState.order : undefined"
            :value="column.filterKey === 'type' ? typeFilter : (column.filterKey === 'status' ? activeFilter : searchQuery)"
            :placeholder="column.filterPlaceholder"
            @apply="value => applyColumnFilter(column, value)"
            @sort="value => applyColumnSort(column, value)"
          />
          <span v-else>{{ column.title }}</span>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'type'">
            {{ locationTypeLabels[record.type] || 'Không xác định' }}
          </template>
          <template v-else-if="column.key === 'status'">
            <a-tag :color="record.isActive ? 'green' : 'default'">{{ record.isActive ? 'Đang sử dụng' : 'Ngừng sử dụng' }}</a-tag>
          </template>
          <template v-else-if="column.key === 'action'">
            <a-space class="table-action-buttons">
              <a-tooltip title="Sửa vị trí">
                <a-button type="link" size="small" aria-label="Sửa vị trí" @click="openEdit(record)">
                  <template #icon><EditOutlined /></template>
                </a-button>
              </a-tooltip>
              <a-tooltip :title="deleteLocationLabel(record)">
                <a-button
                  type="link"
                  danger
                  size="small"
                  :aria-label="deleteLocationLabel(record)"
                  :disabled="!canDeleteLocation(record)"
                  @click="removeLocation(record)"
                >
                  <template #icon><DeleteOutlined /></template>
                </a-button>
              </a-tooltip>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalOpen"
      :title="editing ? 'Sửa vị trí' : 'Thêm vị trí'"
      :confirm-loading="saving"
      width="620px"
      wrap-class-name="app-modal app-form-modal"
      ok-text="Lưu"
      cancel-text="Hủy"
      @ok="submit"
    >
      <a-form layout="vertical">
        <a-form-item label="Mã vị trí" required><a-input v-model:value="form.code" placeholder="VD: LAB-IOT-A1" /></a-form-item>
        <a-form-item label="Tên vị trí" required><a-input v-model:value="form.name" placeholder="VD: Tủ linh kiện A1" /></a-form-item>
        <a-form-item label="Loại vị trí" required>
          <a-select v-model:value="form.type" :options="typeOptions" />
        </a-form-item>
        <a-form-item label="Mô tả"><a-textarea v-model:value="form.description" :rows="3" /></a-form-item>
        <a-form-item><a-checkbox v-model:checked="form.isActive">Đang sử dụng</a-checkbox></a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { EditOutlined, DeleteOutlined } from '@ant-design/icons-vue'
import { locationApi } from '../api/locationApi'
import { getApiErrorMessage } from '../utils/apiError'
import { createTablePagination, TABLE_PAGE_SIZE } from '../utils/tablePagination'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { sortTableRows } from '../utils/tableSort'

const tablePagination = reactive({
  ...createTablePagination(),
  current: 1,
  pageSize: TABLE_PAGE_SIZE
})

const locations = ref([])
const loading = ref(false)
const saving = ref(false)
const modalOpen = ref(false)
const editing = ref(null)
const searchQuery = ref('')
const typeFilter = ref(undefined)
const activeFilter = ref(undefined)
const sortState = reactive({ field: undefined, order: undefined })
const form = reactive({ code: '', name: '', type: 'ROOM', description: '', isActive: true })
const typeOptions = [
  { value: 'ROOM', label: 'Phòng lab' },
  { value: 'AREA', label: 'Khu vực' },
  { value: 'CABINET', label: 'Tủ/Kệ/Bàn' },
  { value: 'SHELF', label: 'Ngăn' }
]
const locationTypeLabels = {
  ROOM: 'Phòng lab',
  AREA: 'Khu vực',
  CABINET: 'Tủ/Kệ/Bàn',
  SHELF: 'Ngăn',
  BUILDING: 'Tòa nhà',
  STORE: 'Kho vật tư',
  LEGACY: 'Chưa phân loại'
}
const columns = [
  { title: 'Mã', dataIndex: 'code', key: 'code', sortKey: 'code', sortable: true, filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm mã vị trí...' },
  { title: 'Tên vị trí', dataIndex: 'name', key: 'name', sortKey: 'name', sortable: true, filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm tên vị trí...' },
  { title: 'Loại', key: 'type', sortKey: 'type', sortable: true, filterType: 'select', filterKey: 'type', filterOptions: typeOptions },
  { title: 'Số tài sản', dataIndex: 'equipmentCount', key: 'equipmentCount', sortKey: 'equipmentCount', sortable: true, width: 130, align: 'center', className: 'location-asset-count-column' },
  { title: 'Trạng thái', key: 'status', sortKey: 'status', sortable: true, width: 170, align: 'center', className: 'status-column', filterType: 'select', filterKey: 'status', filterOptions: [
    { value: 'ACTIVE', label: 'Đang sử dụng' },
    { value: 'INACTIVE', label: 'Ngừng sử dụng' }
  ] },
  { title: 'Thao tác', key: 'action', className: 'table-sticky-action-column', customCell: () => ({ class: 'table-sticky-action-column' }), width: 120, align: 'center' }
]

const canDeleteLocation = (record) => Number(record.equipmentCount || 0) === 0
const deleteLocationLabel = (record) => canDeleteLocation(record)
  ? 'Xóa vị trí'
  : `Không thể xóa: vị trí còn ${record.equipmentCount} tài sản`

const filteredLocations = computed(() => {
  const keyword = searchQuery.value.trim().toLowerCase()
  const filtered = locations.value.filter(location => {
    const matchesSearch = !keyword || [location.code, location.name].some(value => String(value || '').toLowerCase().includes(keyword))
    const matchesType = !typeFilter.value || location.type === typeFilter.value
    const matchesStatus = !activeFilter.value || (activeFilter.value === 'ACTIVE' ? location.isActive : !location.isActive)
    return matchesSearch && matchesType && matchesStatus
  })
  return sortTableRows(filtered, sortState.field, sortState.order)
})

const applyFilters = () => {
  tablePagination.current = 1
}

const applyColumnFilter = (column, value) => {
  if (column.filterKey === 'type') typeFilter.value = value
  else if (column.filterKey === 'status') activeFilter.value = value
  else searchQuery.value = value || ''
  applyFilters()
}

const applyColumnSort = (column, order) => {
  sortState.field = order ? column.sortKey : undefined
  sortState.order = order
}

const handleTableChange = pager => {
  tablePagination.current = pager.current
  tablePagination.pageSize = pager.pageSize
}

const fetchLocations = async () => {
  loading.value = true
  try {
    locations.value = await locationApi.getAll() || []
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách vị trí.'))
  } finally {
    loading.value = false
  }
}

const resetForm = () => Object.assign(form, { code: '', name: '', type: 'ROOM', description: '', isActive: true })
const openCreate = () => { editing.value = null; resetForm(); modalOpen.value = true }
const openEdit = (record) => {
  editing.value = record
  Object.assign(form, {
    code: record.code || '',
    name: record.name || '',
    type: record.type || 'ROOM',
    description: record.description || '',
    isActive: record.isActive !== false
  })
  modalOpen.value = true
}

const locationPayload = () => ({
  code: form.code.trim(),
  name: form.name.trim(),
  type: form.type,
  description: form.description?.trim() || '',
  isActive: form.isActive
})

const submit = async () => {
  if (!form.code.trim() || !form.name.trim() || !form.type) {
    message.warning('Vui lòng nhập mã, tên và loại vị trí.')
    return
  }
  saving.value = true
  try {
    if (editing.value) await locationApi.update(editing.value.id, locationPayload())
    else await locationApi.create(locationPayload())
    message.success(editing.value ? 'Đã cập nhật vị trí.' : 'Đã thêm vị trí.')
    modalOpen.value = false
    await fetchLocations()
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể lưu vị trí.'))
  } finally {
    saving.value = false
  }
}

const removeLocation = (record) => {
  if (!canDeleteLocation(record)) {
    message.warning(`Không thể xóa vị trí vì đang có ${record.equipmentCount} tài sản. Hãy chuyển tài sản hoặc ngừng sử dụng vị trí.`)
    return
  }
  Modal.confirm({
    title: 'Xóa vị trí',
    content: `Bạn có chắc muốn xóa vị trí ${record.name}?`,
    okText: 'Xóa',
    okType: 'danger',
    cancelText: 'Hủy',
    onOk: async () => {
      try { await locationApi.remove(record.id); message.success('Đã xóa vị trí.'); await fetchLocations() }
      catch (error) { message.error(getApiErrorMessage(error, 'Không thể xóa vị trí.')) }
    }
  })
}

onMounted(fetchLocations)
</script>

<style scoped>
.locations-page { padding: 0; }
.page-heading { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 20px; }
.page-heading h2 { margin: 0 0 6px; }
.page-heading p { margin: 0; color: #64748b; }
.location-filters {
  --location-filter-width: 280px;
  --location-filter-height: 40px;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}
.location-filter-control {
  flex: 0 0 var(--location-filter-width);
  width: var(--location-filter-width) !important;
  height: var(--location-filter-height);
  min-width: 0;
}
.location-filter-control :deep(.ant-input-affix-wrapper),
.location-filter-control :deep(.ant-select-selector) {
  height: var(--location-filter-height) !important;
  align-items: center;
}
.locations-page :deep(.location-asset-count-column) {
  width: 130px !important;
  min-width: 130px !important;
  max-width: 130px !important;
  padding-right: 8px !important;
  padding-left: 8px !important;
  text-align: center !important;
}
.locations-page :deep(th.location-asset-count-column .table-column-header) {
  justify-content: center;
  width: 100%;
  min-width: 0;
}
.locations-page :deep(th.location-asset-count-column .table-column-controls) {
  margin-left: 4px;
}
@media (max-width: 640px) { .page-heading { flex-direction: column; } }
@media (max-width: 640px) {
  .location-filter-control {
    flex: 1 1 100%;
    width: 100% !important;
    max-width: 100%;
  }
}
</style>
