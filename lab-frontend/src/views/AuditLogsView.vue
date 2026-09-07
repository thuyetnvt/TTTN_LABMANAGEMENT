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
        <a-input-search v-model:value="filters.search" allow-clear placeholder="Người thao tác, hành động..." class="filter-control" @search="applySearch" />
        <a-select v-model:value="filters.action" allowClear placeholder="Hành động" class="filter-control">
          <a-select-option value="Create">Tạo mới</a-select-option>
          <a-select-option value="Update">Cập nhật</a-select-option>
          <a-select-option value="Delete">Xóa</a-select-option>
          <a-select-option value="Approve">Duyệt</a-select-option>
          <a-select-option value="Reject">Từ chối</a-select-option>
          <a-select-option value="Return">Trả thiết bị</a-select-option>
        </a-select>
        <a-select v-model:value="filters.entityType" allowClear placeholder="Đối tượng" class="filter-control">
          <a-select-option value="Equipment">Thiết bị</a-select-option>
          <a-select-option value="Consumable">Vật tư</a-select-option>
          <a-select-option value="BorrowRecord">Phiếu mượn</a-select-option>
          <a-select-option value="ConsumableRequest">Yêu cầu vật tư</a-select-option>
          <a-select-option value="AssetCategory">Danh mục</a-select-option>
        </a-select>
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
          <template v-else-if="column.key === 'details'">
            <a-tooltip title="Xem chi tiết">
              <a-button
                type="link"
                class="table-detail-action"
                aria-label="Xem chi tiết nhật ký"
                @click="showDetails(record)"
              >
                <template #icon><EyeOutlined /></template>
              </a-button>
            </a-tooltip>
          </template>
        </template>
      </DataTable>
    </a-card>

    <a-modal v-model:open="detailsVisible" title="Chi tiết nhật ký" :footer="null" width="760px">
      <pre class="details-json">{{ selectedDetails }}</pre>
    </a-modal>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { message } from 'ant-design-vue'
import { ReloadOutlined, EyeOutlined } from '@ant-design/icons-vue'
import { auditApi } from '../api/auditApi'
import AuditActionLabel from '../components/AuditActionLabel.vue'
import FilterBar from '../components/FilterBar.vue'
import DataTable from '../components/DataTable.vue'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { formatVietnamDateTime } from '../utils/dateTime.js'
import { TABLE_PAGE_SIZE, TABLE_PAGE_SIZE_OPTIONS } from '../utils/tablePagination'

const logs = ref([])
const loading = ref(false)
const detailsVisible = ref(false)
const selectedDetails = ref('')
const filters = reactive({
  search: '',
  action: undefined,
  entityType: undefined
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
  { value: 'Create', label: 'Tạo mới' },
  { value: 'Update', label: 'Cập nhật' },
  { value: 'Delete', label: 'Xóa' },
  { value: 'Approve', label: 'Duyệt' },
  { value: 'Reject', label: 'Từ chối' },
  { value: 'Return', label: 'Trả thiết bị' }
]
const auditEntityOptions = [
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
  { title: 'IP', dataIndex: 'ipAddress', key: 'ipAddress', width: 150, sortable: true, sortKey: 'ipAddress', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm địa chỉ IP...' },
  { title: 'Chi tiết', key: 'details', width: 120, align: 'center' }
]

const fetchLogs = async () => {
  loading.value = true
  try {
    const res = await auditApi.getLogs({
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
  if (!filters.search && !filters.action && !filters.entityType) {
    pagination.current = 1
    fetchLogs()
    return
  }
  filters.action = undefined
  filters.entityType = undefined
  filters.search = ''
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

const showDetails = (record) => {
  try {
    selectedDetails.value = record.details
      ? JSON.stringify(JSON.parse(record.details), null, 2)
      : 'Không có chi tiết.'
  } catch {
    selectedDetails.value = record.details || 'Không có chi tiết.'
  }
  detailsVisible.value = true
}

const entityLabel = (entityType) => ({
  Equipment: 'Tài sản',
  User: 'Người dùng',
  BorrowRecord: 'Phiếu mượn',
  MaintenanceRecord: 'Phiếu bảo trì',
  Consumable: 'Vật tư',
  ConsumableRequest: 'Yêu cầu vật tư',
  AssetCategory: 'Danh mục',
  Penalty: 'Bồi thường',
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
  () => [filters.action, filters.entityType],
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
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.filter-control {
  width: 190px;
}

.details-json {
  max-height: 520px;
  margin: 0;
  padding: 14px;
  overflow: auto;
  border-radius: 8px;
  background: #0f172a;
  color: #e5e7eb;
  font-size: 13px;
  line-height: 1.5;
}
</style>
