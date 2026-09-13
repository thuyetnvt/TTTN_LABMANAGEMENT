<template>
  <div class="audit-container">
    <div class="page-header">
      <div>
        <h2>Nhật ký hoạt động</h2>
        <p>Theo dõi thao tác thêm, sửa, xóa, duyệt và trả thiết bị trong hệ thống.</p>
      </div>
      <a-button @click="fetchLogs">
        <template #icon><reload-outlined /></template>
        Tải lại
      </a-button>
    </div>

    <FilterBar class="filter-card">
      <div class="filters">
        <a-input-search v-model:value="filters.search" allow-clear placeholder="Người thao tác, hành động..." class="filter-search" @search="applySearch" />
        <a-select v-model:value="filters.action" allowClear placeholder="Hành động" class="filter-control">
          <a-select-option v-for="option in auditActionOptions" :key="option.value" :value="option.value">{{ option.label }}</a-select-option>
        </a-select>
        <a-select v-model:value="filters.entityType" allowClear placeholder="Đối tượng" class="filter-control">
          <a-select-option v-for="option in auditEntityOptions" :key="option.value" :value="option.value">{{ option.label }}</a-select-option>
        </a-select>
        <a-range-picker v-model:value="filters.dates" format="DD/MM/YYYY" :placeholder="['Từ ngày', 'Đến ngày']" class="filter-dates" />
        <a-button @click="resetFilters">Xóa lọc</a-button>
      </div>
    </FilterBar>

    <a-card :bordered="false" class="table-card">
      <DataTable
        :dataSource="logs"
        :columns="columns"
        :loading="loading"
        rowKey="id"
        bordered
        :pagination="pagination"
        :scroll="{ x: 'max-content' }"
        @change="handleTableChange"
      >
        <template #headerCell="{ column }">
          <TableColumnFilter
            v-if="column.filterType || column.sortable"
            :title="column.title"
            :type="column.filterType"
            :options="column.filterOptions"
            :value="filters[column.filterKey]"
            :placeholder="column.filterPlaceholder"
            :filterable="Boolean(column.filterType)"
            :sortable="Boolean(column.sortable)"
            :sort-order="sortState.field === column.sortKey ? sortState.order : undefined"
            @apply="value => applyColumnFilter(column, value)"
            @sort="value => applyColumnSort(column, value)"
          />
          <span v-else>{{ column.title }}</span>
        </template>
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'action'">
            <AuditActionLabel :action="record.action" />
          </template>
          <template v-else-if="column.key === 'username'">
            {{ actorLabel(record) }}
          </template>
          <template v-else-if="column.key === 'createdAt'">
            {{ formatDateTime(record.createdAt) }}
          </template>
          <template v-else-if="column.key === 'entityType'">
            {{ entityLabel(record.entityType) }}
          </template>
        </template>
      </DataTable>
    </a-card>

  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { message } from 'ant-design-vue'
import { ReloadOutlined } from '@ant-design/icons-vue'
import { auditApi } from '../api/auditApi'
import AuditActionLabel from '../components/AuditActionLabel.vue'
import FilterBar from '../components/FilterBar.vue'
import DataTable from '../components/DataTable.vue'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { formatVietnamDateTime } from '../utils/dateTime.js'
import { TABLE_PAGE_SIZE, TABLE_PAGE_SIZE_OPTIONS } from '../utils/tablePagination'

const logs = ref([])
const loading = ref(false)
const filters = reactive({
  search: '',
  action: undefined,
  entityType: undefined,
  dates: null
})
const sortState = reactive({ field: undefined, order: undefined })
const pagination = reactive({
  current: 1,
  pageSize: TABLE_PAGE_SIZE,
  total: 0,
  showSizeChanger: true,
  pageSizeOptions: TABLE_PAGE_SIZE_OPTIONS,
  hideOnSinglePage: false,
  position: ['bottomRight']
})

const auditActionOptions = [
  { value: '__OTHER__', label: 'Khác (ngoài các mục bên dưới)' },
  { value: 'LoginSucceeded', label: 'Đăng nhập thành công' },
  { value: 'LoginFailed', label: 'Đăng nhập thất bại' },
  { value: 'UpdateProfile', label: 'Cập nhật hồ sơ' },
  { value: 'Activate', label: 'Mở khóa tài khoản' },
  { value: 'Deactivate', label: 'Khóa tài khoản' },
  { value: 'ChangePassword', label: 'Đổi mật khẩu' },
  { value: 'SendReturnReminder', label: 'Nhắc trả' },
  { value: 'Create', label: 'Tạo mới' },
  { value: 'Update', label: 'Cập nhật' },
  { value: 'Delete', label: 'Xóa' },
  { value: 'Approve', label: 'Duyệt' },
  { value: 'Reject', label: 'Từ chối' },
  { value: 'Return', label: 'Trả thiết bị' }
]
const auditEntityOptions = [
  { value: '__OTHER__', label: 'Khác (ngoài các mục bên dưới)' },
  { value: 'User', label: 'Người dùng' },
  { value: 'LocationNode', label: 'Vị trí' },
  { value: 'InventorySession', label: 'Đợt kiểm kê' },
  { value: 'Equipment', label: 'Thiết bị' },
  { value: 'Consumable', label: 'Vật tư' },
  { value: 'BorrowRecord', label: 'Phiếu mượn' },
  { value: 'ConsumableRequest', label: 'Yêu cầu vật tư' },
  { value: 'AssetCategory', label: 'Danh mục' }
]

const columns = [
  { title: 'Thời gian', dataIndex: 'createdAt', key: 'createdAt', width: 170, sortable: true, sortKey: 'createdAt' },
  { title: 'Người thao tác', dataIndex: 'username', key: 'username', width: 150, sortable: true, sortKey: 'username', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm người thao tác...' },
  { title: 'Hành động', dataIndex: 'action', key: 'action', width: 130, sortable: true, sortKey: 'action', filterType: 'select', filterKey: 'action', filterOptions: auditActionOptions },
  { title: 'Đối tượng', dataIndex: 'entityType', key: 'entityType', width: 150, sortable: true, sortKey: 'entityType', filterType: 'select', filterKey: 'entityType', filterOptions: auditEntityOptions },
  { title: 'IP', dataIndex: 'ipAddress', key: 'ipAddress', width: 150, sortable: true, sortKey: 'ipAddress', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm địa chỉ IP...' }
]

const fetchLogs = async () => {
  loading.value = true
  try {
    const res = await auditApi.getLogs({
      from: filters.dates?.[0]?.format('YYYY-MM-DD'),
      to: filters.dates?.[1]?.format('YYYY-MM-DD'),
      page: pagination.current,
      pageSize: pagination.pageSize,
      action: filters.action,
      entityType: filters.entityType,
      search: filters.search.trim() || undefined,
      sortBy: sortState.field,
      sortDirection: sortState.order === 'descend' ? 'desc' : (sortState.order === 'ascend' ? 'asc' : undefined)
    })
    logs.value = res.items || []
    pagination.total = res.total || 0
  } catch {
    message.error('Không tải được nhật ký hoạt động')
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  if (!filters.search && !filters.action && !filters.entityType && !filters.dates) {
    pagination.current = 1
    fetchLogs()
    return
  }
  filters.action = undefined
  filters.entityType = undefined
  filters.search = ''
  filters.dates = null
}

const applySearch = () => {
  pagination.current = 1
  fetchLogs()
}

const applyColumnFilter = (column, value) => {
  if (column.filterKey === 'action' || column.filterKey === 'entityType') {
    filters[column.filterKey] = value
    return
  }
  filters.search = value || ''
  applySearch()
}

const applyColumnSort = (column, order) => {
  sortState.field = order ? (column.sortKey || column.key) : undefined
  sortState.order = order || undefined
  pagination.current = 1
  fetchLogs()
}

const handleTableChange = (pager) => {
  pagination.current = pager.pageSize === pagination.pageSize ? pager.current : 1
  pagination.pageSize = pager.pageSize
  fetchLogs()
}

const entityLabel = (entityType) => ({
  Equipment: 'Tài sản',
  User: 'Người dùng',
  BorrowRecord: 'Phiếu mượn',
  MaintenanceRecord: 'Phiếu bảo trì',
  Consumable: 'Vật tư',
  ConsumableRequest: 'Yêu cầu vật tư',
  AssetCategory: 'Danh mục',
  Database: 'Cơ sở dữ liệu',
  LocationNode: 'Vị trí',
  InventorySession: 'Đợt kiểm kê',
  ReturnEvidence: 'Minh chứng trả',
  MaintenanceSchedule: 'Kế hoạch bảo trì'
}[entityType] || 'Đối tượng khác')

const formatDateTime = value => formatVietnamDateTime(value, '')

const actorLabel = record => {
  const displayName = String(record?.actorDisplayName || '').trim()
  if (displayName) return displayName

  const username = record?.username
  const normalized = String(username || '').trim()
  if (!normalized) return 'Không xác định'
  return normalized.toLowerCase() === 'system' ? 'Hệ thống' : normalized
}

watch(
  () => [filters.action, filters.entityType, filters.dates],
  () => {
    pagination.current = 1
    fetchLogs()
  }
)

onMounted(fetchLogs)
</script>

<style scoped>
.audit-container {
  padding: 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 18px;
}

.page-header h2 {
  margin: 0;
  font-size: 24px;
  font-weight: 700;
  color: #111827;
}

.page-header p {
  margin: 4px 0 0;
  color: #6b7280;
}

.filter-card,
.table-card {
  border-radius: 8px;
  box-shadow: 0 3px 10px rgba(15, 23, 42, 0.06);
}

.filter-card {
  margin-bottom: 16px;
}

.filters {
  --audit-filter-width: 150px;
  --audit-filter-height: 40px;
  --audit-search-button-width: 40px;
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.filter-control {
  width: var(--audit-filter-width);
}

.filter-dates { width: var(--audit-filter-width); }
.filter-search {
  width: calc(var(--audit-filter-width) + var(--audit-search-button-width)) !important;
  min-width: calc(var(--audit-filter-width) + var(--audit-search-button-width));
}

.filter-control,
.filter-dates,
.filters :deep(.ant-select-selector),
.filter-search :deep(.ant-input-affix-wrapper),
.filter-search :deep(.ant-input-search-button),
.filters > .ant-btn {
  height: var(--audit-filter-height);
}

.filter-search :deep(.ant-input-affix-wrapper) {
  flex: 0 0 var(--audit-filter-width);
}

.filter-search :deep(.ant-input-search-button) {
  width: var(--audit-search-button-width);
}

.filters :deep(.ant-select-selector) {
  align-items: center;
}
@media (max-width: 640px) {
  .filter-search, .filter-control, .filter-dates { width: 100%; }
}

</style>
