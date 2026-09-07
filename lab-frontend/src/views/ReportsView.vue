<template>
  <div class="reports-page">
    <PageHeader
      title="Báo cáo vận hành"
    >
      <template #actions>
        <a-button :loading="exportingPdf" @click="exportPdf">
          <template #icon><FilePdfOutlined /></template>
          Xuất PDF
        </a-button>
        <a-button type="primary" :loading="exporting" @click="exportReport">
          <template #icon><FileExcelOutlined /></template>
          Xuất Excel
        </a-button>
      </template>
    </PageHeader>

    <section class="filter-section" aria-label="Bộ lọc báo cáo">
      <a-card :bordered="false" class="filter-card">
        <div class="filter-grid">
          <div class="filter-field">
            <label for="reports-from">Từ ngày</label>
            <a-input id="reports-from" v-model:value="filterForm.from" type="date" />
          </div>
          <div class="filter-field">
            <label for="reports-to">Đến ngày</label>
            <a-input id="reports-to" v-model:value="filterForm.to" type="date" />
          </div>
          <div class="filter-field">
            <label>Danh mục</label>
            <a-select v-model:value="filterForm.categoryId" allow-clear placeholder="Tất cả danh mục">
              <a-select-option v-for="category in categories" :key="category.id" :value="category.id">
                {{ category.name }}
              </a-select-option>
            </a-select>
          </div>
          <div class="filter-field">
            <label>Vị trí</label>
            <a-select v-model:value="filterForm.locationNodeId" allow-clear placeholder="Tất cả vị trí">
              <a-select-option v-for="location in locations" :key="location.id" :value="location.id">
                {{ location.code }} — {{ location.name }}
              </a-select-option>
            </a-select>
          </div>
          <div class="filter-actions">
            <a-button :disabled="loading" @click="resetFilters">
              <template #icon><ReloadOutlined /></template>
              Đặt lại
            </a-button>
          </div>
        </div>
      </a-card>
    </section>

    <a-spin :spinning="loading" class="reports-spin">
      <section class="overview-section" aria-label="Tổng quan vận hành">
        <div class="overview-grid">
          <a-card v-for="item in summaryCards" :key="item.label" :bordered="false" class="summary-card">
            <div class="summary-icon" :class="`summary-icon--${item.tone}`">
              <component :is="item.icon" />
            </div>
            <div class="summary-copy">
              <span class="summary-label">{{ item.label }}</span>
              <strong class="summary-value">{{ item.value }}</strong>
            </div>
          </a-card>
        </div>
      </section>

      <a-card :bordered="false" class="report-card detail-card">
        <template #title>Chi tiết vận hành</template>
        <a-tabs v-model:active-key="activeTab" class="operation-tabs">
          <a-tab-pane key="borrow" tab="Mượn trả">
            <a-table
              v-if="borrowedDisplay.length"
              :data-source="borrowedDisplay"
              :columns="borrowColumns"
              :pagination="borrowPagination"
              :scroll="{ x: 680 }"
              row-key="id"
              size="small"
            >
              <template #headerCell="{ column }">
                <TableColumnFilter
                  v-if="column.filterType || column.sortable"
                  :title="column.title"
                  :type="column.filterType"
                  :options="column.filterOptions"
                  :value="column.filterKey === 'status' ? reportTableFilters.borrowStatus : reportTableFilters.borrowSearch"
                  :placeholder="column.filterPlaceholder"
                  :filterable="Boolean(column.filterType)"
                  :sortable="Boolean(column.sortable)"
                  :sort-order="borrowSortState.field === column.sortKey ? borrowSortState.order : undefined"
                  @apply="value => applyReportFilter('borrow', column, value)"
                  @sort="value => applyReportSort(borrowSortState, column, value)"
                />
                <span v-else>{{ column.title }}</span>
              </template>
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'expectedReturnDate'">
                  {{ formatDate(record.expectedReturnDate) }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="record.overdue ? 'red' : (record.processingReturn ? 'orange' : 'blue')">
                    {{ record.overdue ? 'Quá hạn' : (record.processingReturn ? 'Đang xử lý trả' : 'Đang mượn') }}
                  </a-tag>
                </template>
                <template v-else>
                  <span class="cell-ellipsis" :title="cellText(record[column.dataIndex])">
                    {{ cellText(record[column.dataIndex]) }}
                  </span>
                </template>
              </template>
            </a-table>
            <a-empty v-else description="Chưa có dữ liệu mượn trả" />
          </a-tab-pane>

          <a-tab-pane key="responsible" tab="Người chịu trách nhiệm">
            <a-table
              v-if="responsibleDisplay.length"
              :data-source="responsibleDisplay"
              :columns="responsibleColumns"
              :pagination="responsiblePagination"
              :scroll="{ x: 680 }"
              row-key="responsiblePerson"
              size="small"
            >
              <template #headerCell="{ column }">
                <TableColumnFilter
                  v-if="column.filterType || column.sortable"
                  :title="column.title"
                  :type="column.filterType"
                  :value="reportTableFilters.responsibleSearch"
                  :placeholder="column.filterPlaceholder"
                  :filterable="Boolean(column.filterType)"
                  :sortable="Boolean(column.sortable)"
                  :sort-order="responsibleSortState.field === column.sortKey ? responsibleSortState.order : undefined"
                  @apply="value => applyReportFilter('responsible', column, value)"
                  @sort="value => applyReportSort(responsibleSortState, column, value)"
                />
                <span v-else>{{ column.title }}</span>
              </template>
              <template #bodyCell="{ column, record }">
                <span class="cell-ellipsis" :title="cellText(record[column.dataIndex])">
                  {{ cellText(record[column.dataIndex]) }}
                </span>
              </template>
            </a-table>
            <a-empty v-else description="Chưa có dữ liệu người chịu trách nhiệm" />
          </a-tab-pane>

          <a-tab-pane key="consumables" tab="Vật tư">
            <a-table
              v-if="consumablesDisplay.length"
              :data-source="consumablesDisplay"
              :columns="consumableColumns"
              :pagination="consumablePagination"
              :scroll="{ x: 600 }"
              row-key="id"
              size="small"
            >
              <template #headerCell="{ column }">
                <TableColumnFilter
                  v-if="column.filterType || column.sortable"
                  :title="column.title"
                  :type="column.filterType"
                  :options="column.filterOptions"
                  :value="column.filterKey === 'status' ? reportTableFilters.consumableStatus : reportTableFilters.consumableSearch"
                  :placeholder="column.filterPlaceholder"
                  :filterable="Boolean(column.filterType)"
                  :sortable="Boolean(column.sortable)"
                  :sort-order="consumableSortState.field === column.sortKey ? consumableSortState.order : undefined"
                  @apply="value => applyReportFilter('consumables', column, value)"
                  @sort="value => applyReportSort(consumableSortState, column, value)"
                />
                <span v-else>{{ column.title }}</span>
              </template>
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'quantity'">
                  {{ formatNumber(record.availableQuantity ?? record.quantity) }} {{ record.unit || '' }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="(record.availableQuantity ?? record.quantity) <= record.minQuantity ? 'orange' : 'green'">
                    {{ (record.availableQuantity ?? record.quantity) <= record.minQuantity ? 'Sắp hết' : 'Đủ tồn' }}
                  </a-tag>
                </template>
                <template v-else>
                  <span class="cell-ellipsis" :title="cellText(record[column.dataIndex])">
                    {{ cellText(record[column.dataIndex]) }}
                  </span>
                </template>
              </template>
            </a-table>
            <a-empty v-else description="Chưa có dữ liệu vật tư" />
          </a-tab-pane>
        </a-tabs>
      </a-card>

      <section class="main-grid" aria-label="Tình hình và cảnh báo tài sản">
        <a-card :bordered="false" class="report-card status-card">
          <template #title>Tình hình tài sản</template>
          <div v-if="statusRows.length" class="status-list">
            <button
              v-for="item in statusRows"
              :key="item.value"
              type="button"
              class="status-row"
              :disabled="item.count === 0"
              :aria-label="`Xem ${getEquipmentStatusLabel(item.value)}: ${item.count} thiết bị`"
              @click="openStatusDetails(item)"
            >
              <div class="status-heading">
                <span class="status-name">{{ getEquipmentStatusLabel(item.value) }}</span>
                <strong>{{ formatNumber(item.count) }}</strong>
              </div>
              <div class="status-track" aria-hidden="true">
                <span
                  class="status-fill"
                  :class="`status-fill--${getStatusColor(item.value)}`"
                  :style="{ width: `${statusPercent(item.count)}%` }"
                />
              </div>
            </button>
          </div>
          <a-empty v-else description="Chưa có dữ liệu" />
        </a-card>

        <a-card :bordered="false" class="report-card attention-card">
          <template #title>Cần chú ý</template>
          <div v-if="hasAttention" class="attention-list">
            <div v-for="alert in attentionCards" :key="alert.key" class="attention-item">
              <span class="attention-icon" :class="`attention-icon--${alert.tone}`">
                <component :is="alert.icon" />
              </span>
              <div class="attention-copy">
                <strong>{{ alert.title }}</strong>
                <span>{{ alert.description }}</span>
              </div>
              <strong class="attention-count">{{ alert.value }}</strong>
              <a-button type="link" class="attention-action" @click="goTo(alert.route)">
                Xem
                <ArrowRightOutlined />
              </a-button>
            </div>
          </div>
          <a-empty v-else description="Không có cảnh báo" />
        </a-card>
      </section>
    </a-spin>

    <a-modal
      v-model:open="statusDetailsVisible"
      :title="statusDetailsTitle"
      :footer="null"
      width="900px"
      @cancel="closeStatusDetails"
    >
      <a-spin :spinning="statusDetailsLoading">
        <p class="status-details-description">
          Danh sách thiết bị thuộc trạng thái “{{ selectedStatusLabel }}”.
        </p>
        <a-table
          v-if="statusDetails.length"
          :data-source="statusDetails"
          :columns="statusDetailsColumns"
          :pagination="{ pageSize: 10, hideOnSinglePage: true, showSizeChanger: false }"
          row-key="id"
          size="small"
          :scroll="{ x: 700 }"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'">
              <a-tag :color="getStatusColor(record.status)">
                {{ getEquipmentStatusLabel(record.status) }}
              </a-tag>
            </template>
            <span v-else class="cell-ellipsis" :title="cellText(record[column.dataIndex])">
              {{ cellText(record[column.dataIndex]) }}
            </span>
          </template>
        </a-table>
        <a-empty v-else-if="!statusDetailsLoading" description="Không có thiết bị thuộc trạng thái này" />
      </a-spin>
    </a-modal>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { message } from 'ant-design-vue'
import {
  AppstoreOutlined,
  ArrowRightOutlined,
  ClockCircleOutlined,
  DollarOutlined,
  FileExcelOutlined,
  FilePdfOutlined,
  ReloadOutlined,
  ToolOutlined,
  WarningOutlined
} from '@ant-design/icons-vue'
import PageHeader from '../components/PageHeader.vue'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { equipmentApi } from '../api/equipmentApi'
import { locationApi } from '../api/locationApi'
import { reportsApi } from '../api/reportsApi'
import { STATUS, normalizeStatus } from '../constants/business'
import { getEquipmentStatusLabel, getStatusColor } from '../utils/statusLabels'
import { getApiErrorMessage } from '../utils/apiError'
import router from '../router'
import { createTablePagination } from '../utils/tablePagination'
import { formatVietnamDate } from '../utils/dateTime'
import TableColumnFilter from '../components/TableColumnFilter.vue'
import { sortTableRows } from '../utils/tableSort'

const borrowPagination = createTablePagination()
const responsiblePagination = createTablePagination()
const consumablePagination = createTablePagination()
const reportTableFilters = reactive({
  borrowSearch: '',
  borrowStatus: undefined,
  responsibleSearch: '',
  consumableSearch: '',
  consumableStatus: undefined
})
const borrowSortState = reactive({ field: undefined, order: undefined })
const responsibleSortState = reactive({ field: undefined, order: undefined })
const consumableSortState = reactive({ field: undefined, order: undefined })

const filters = () => ({ from: '', to: '', categoryId: null, locationNodeId: null })
const filterForm = ref(filters())
const appliedFilters = ref(filters())
const categories = ref([])
const locations = ref([])
const loading = ref(false)
const exporting = ref(false)
const exportingPdf = ref(false)
const activeTab = ref('borrow')
const report = ref({
  totals: {},
  byStatus: [],
  byLocation: [],
  borrowed: [],
  lowStock: [],
  maintenance: [],
  responsible: [],
  consumables: []
})

const assetStatusOrder = [
  STATUS.AVAILABLE,
  STATUS.BORROW_PENDING,
  STATUS.BORROWED,
  STATUS.MAINTENANCE_IN_PROGRESS,
  STATUS.BROKEN,
  STATUS.MISSING
]

const borrowColumns = [
  { title: 'Người mượn', dataIndex: 'user', key: 'user', width: 150, ellipsis: true, sortable: true, sortKey: 'user', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm người mượn...' },
  { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment', width: 220, ellipsis: true, sortable: true, sortKey: 'equipment', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm thiết bị...' },
  { title: 'Hạn trả', key: 'expectedReturnDate', width: 130, sortable: true, sortKey: 'expectedReturnDate' },
  { title: 'Trạng thái', key: 'status', width: 130, sortable: true, sortKey: 'status', filterType: 'select', filterKey: 'status', filterOptions: [
    { value: 'BORROWED', label: 'Đang mượn' },
    { value: 'OVERDUE', label: 'Quá hạn' },
    { value: 'RETURN_PROCESSING', label: 'Đang xử lý trả' }
  ] }
]
const responsibleColumns = [
  { title: 'Người chịu trách nhiệm', dataIndex: 'responsiblePerson', key: 'responsiblePerson', width: 220, ellipsis: true, sortable: true, sortKey: 'responsiblePerson', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm người phụ trách...' },
  { title: 'Số thiết bị', dataIndex: 'equipmentCount', key: 'equipmentCount', width: 130, sortable: true, sortKey: 'equipmentCount' },
  { title: 'Thiết bị phụ trách', dataIndex: 'equipment', key: 'equipment', width: 420, ellipsis: true, sortable: true, sortKey: 'equipment', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm thiết bị...' }
]
const consumableColumns = [
  { title: 'Vật tư', dataIndex: 'name', key: 'name', width: 280, ellipsis: true, sortable: true, sortKey: 'name', filterType: 'search', filterKey: 'search', filterPlaceholder: 'Tìm vật tư...' },
  { title: 'Khả dụng', key: 'quantity', width: 150, sortable: true, sortKey: 'quantity' },
  { title: 'Mức tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity', width: 150, sortable: true, sortKey: 'minQuantity' },
  { title: 'Trạng thái', key: 'status', width: 140, sortable: true, sortKey: 'status', filterType: 'select', filterKey: 'status', filterOptions: [
    { value: 'LOW_STOCK', label: 'Sắp hết' },
    { value: 'AVAILABLE', label: 'Đủ tồn' }
  ] }
]

const borrowedDisplay = computed(() => {
  const search = reportTableFilters.borrowSearch.trim().toLowerCase()
  const rows = report.value.borrowed.filter(item => {
    const matchesSearch = !search || [item.user, item.equipment].some(value => String(value || '').toLowerCase().includes(search))
    const status = item.overdue ? 'OVERDUE' : (item.processingReturn ? 'RETURN_PROCESSING' : 'BORROWED')
    return matchesSearch && (!reportTableFilters.borrowStatus || status === reportTableFilters.borrowStatus)
  })
  return sortTableRows(rows, borrowSortState.field, borrowSortState.order, (row, field) => {
    if (field === 'status') return row.overdue ? 'Quá hạn' : (row.processingReturn ? 'Đang xử lý trả' : 'Đang mượn')
    return row[field]
  })
})

const responsibleDisplay = computed(() => {
  const search = reportTableFilters.responsibleSearch.trim().toLowerCase()
  const rows = report.value.responsible.filter(item => !search || [item.responsiblePerson, item.equipment].some(value => String(value || '').toLowerCase().includes(search)))
  return sortTableRows(rows, responsibleSortState.field, responsibleSortState.order)
})

const consumablesDisplay = computed(() => {
  const search = reportTableFilters.consumableSearch.trim().toLowerCase()
  const rows = report.value.consumables.filter(item => {
    const available = Number(item.availableQuantity ?? item.quantity ?? 0)
    const status = available <= Number(item.minQuantity || 0) ? 'LOW_STOCK' : 'AVAILABLE'
    return (!search || String(item.name || '').toLowerCase().includes(search))
      && (!reportTableFilters.consumableStatus || status === reportTableFilters.consumableStatus)
  })
  return sortTableRows(rows, consumableSortState.field, consumableSortState.order, (row, field) => {
    if (field === 'quantity') return Number(row.availableQuantity ?? row.quantity ?? 0)
    if (field === 'status') return Number(row.availableQuantity ?? row.quantity ?? 0) <= Number(row.minQuantity || 0) ? 'Sắp hết' : 'Đủ tồn'
    return row[field]
  })
})

const summaryCards = computed(() => [
  {
    label: 'Tổng tài sản',
    value: formatNumber(report.value.totals.assets),
    icon: AppstoreOutlined,
    tone: 'primary'
  },
  {
    label: 'Đang mượn',
    value: formatNumber(report.value.totals.borrowed),
    icon: ClockCircleOutlined,
    tone: 'info'
  },
  {
    label: 'Đang hỏng',
    value: formatNumber(report.value.totals.broken),
    icon: ToolOutlined,
    tone: 'warning'
  },
  {
    label: 'Chi phí bảo trì',
    value: formatCurrency(report.value.totals.maintenanceCost),
    icon: DollarOutlined,
    tone: 'success'
  }
])

const statusRows = computed(() => {
  if (!report.value.byStatus.length) return []
  const counts = new Map(
    report.value.byStatus.map(item => [normalizeStatus(item.status), Number(item.count || 0)])
  )
  const extraStatuses = [...counts.keys()].filter(value => !assetStatusOrder.includes(value))
  return [...assetStatusOrder, ...extraStatuses].map(value => ({ value, count: counts.get(value) || 0 }))
})

const statusDetailsVisible = ref(false)
const statusDetailsLoading = ref(false)
const selectedStatus = ref('')
const selectedStatusCount = ref(0)
const statusDetails = ref([])
const statusDetailsColumns = [
  { title: 'Tên thiết bị', dataIndex: 'name', key: 'name', width: 220, ellipsis: true },
  { title: 'Mã tài sản', dataIndex: 'assetCode', key: 'assetCode', width: 140, ellipsis: true },
  { title: 'Model', dataIndex: 'model', key: 'model', width: 180, ellipsis: true },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 150, ellipsis: true },
  { title: 'Vị trí', dataIndex: 'location', key: 'location', width: 150, ellipsis: true },
  { title: 'Trạng thái', key: 'status', width: 130 }
]
const selectedStatusLabel = computed(() => selectedStatus.value ? getEquipmentStatusLabel(selectedStatus.value) : '')
const statusDetailsTitle = computed(() => selectedStatusLabel.value
  ? `${selectedStatusLabel.value} (${formatNumber(selectedStatusCount.value)} thiết bị)`
  : 'Danh sách thiết bị')

const openStatusDetails = async item => {
  if (!item || item.count === 0) return
  selectedStatus.value = item.value
  selectedStatusCount.value = item.count
  statusDetails.value = []
  statusDetailsVisible.value = true
  statusDetailsLoading.value = true
  try {
    const result = await equipmentApi.getPaged({ page: 1, pageSize: 100, status: item.value })
    statusDetails.value = Array.isArray(result?.items) ? result.items : []
    selectedStatusCount.value = Number(result?.total ?? item.count)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được danh sách thiết bị.'))
  } finally {
    statusDetailsLoading.value = false
  }
}

const closeStatusDetails = () => {
  statusDetailsVisible.value = false
  statusDetails.value = []
  selectedStatus.value = ''
  selectedStatusCount.value = 0
}

const statusTotal = computed(() => statusRows.value.reduce((total, item) => total + item.count, 0))

const maintenanceInProgressCount = computed(() => Number(
  report.value.totals.maintenanceInProgress
    ?? report.value.maintenance.filter(item => {
      const status = normalizeStatus(item.status)
      return status === 'IN_PROGRESS' || status === STATUS.MAINTENANCE_IN_PROGRESS
    }).length
))

const attentionCards = computed(() => [
  {
    key: 'overdue',
    title: 'Thiết bị quá hạn trả',
    count: Number(report.value.totals.overdue || 0),
    value: formatNumber(report.value.totals.overdue),
    description: 'Thiết bị cần được trả trước hạn.',
    icon: ClockCircleOutlined,
    tone: 'info',
    route: { name: 'BorrowHistory', query: { status: 'OVERDUE' } }
  },
  {
    key: 'maintenance',
    title: 'Thiết bị đang bảo trì',
    count: maintenanceInProgressCount.value,
    value: formatNumber(maintenanceInProgressCount.value),
    description: 'Thiết bị đang trong quá trình bảo trì.',
    icon: ToolOutlined,
    tone: 'warning',
    route: { name: 'Maintenance' }
  },
  {
    key: 'broken',
    title: 'Thiết bị hỏng',
    count: Number(report.value.totals.broken || 0),
    value: formatNumber(report.value.totals.broken),
    description: 'Thiết bị đang hỏng và cần xử lý.',
    icon: WarningOutlined,
    tone: 'danger',
    route: { name: 'Devices', query: { status: STATUS.BROKEN } }
  },
  {
    key: 'maintenance-cost',
    title: 'Chi phí bảo trì trong kỳ',
    count: Number(report.value.totals.maintenanceCost || 0),
    value: formatCurrency(report.value.totals.maintenanceCost),
    description: 'Tổng chi phí phát sinh trong kỳ.',
    icon: DollarOutlined,
    tone: 'success',
    route: { name: 'Maintenance' }
  }
])

const hasAttention = computed(() => attentionCards.value.some(item => item.count > 0))

const formatNumber = value => Number(value || 0).toLocaleString('vi-VN')
const formatCurrency = value => `${Number(value || 0).toLocaleString('vi-VN')} ₫`
const cellText = value => value === null || value === undefined || value === '' ? '—' : String(value)
const statusPercent = count => statusTotal.value ? (Number(count || 0) / statusTotal.value) * 100 : 0

const applyReportFilter = (group, column, value) => {
  const suffix = column.filterKey === 'status' ? 'Status' : 'Search'
  const prefix = group === 'consumables' ? 'consumable' : group
  reportTableFilters[`${prefix}${suffix}`] = value || undefined
  if (group === 'borrow') borrowPagination.current = 1
  if (group === 'responsible') responsiblePagination.current = 1
  if (group === 'consumables') consumablePagination.current = 1
}

const applyReportSort = (state, column, order) => {
  state.field = order ? (column.sortKey || column.key) : undefined
  state.order = order || undefined
}

const loadOptions = async () => {
  const [categoryResult, locationResult] = await Promise.allSettled([assetCategoryApi.getAll(), locationApi.getAll()])
  if (categoryResult.status === 'fulfilled') categories.value = Array.isArray(categoryResult.value) ? categoryResult.value : []
  if (locationResult.status === 'fulfilled') {
    locations.value = Array.isArray(locationResult.value)
      ? locationResult.value.filter(item => item.isActive !== false)
      : []
  }
}

const load = async () => {
  loading.value = true
  try {
    const result = await reportsApi.summary(appliedFilters.value)
    report.value = {
      totals: result?.totals || {},
      byStatus: Array.isArray(result?.byStatus) ? result.byStatus : [],
      byLocation: Array.isArray(result?.byLocation) ? result.byLocation : [],
      borrowed: Array.isArray(result?.borrowed) ? result.borrowed : [],
      lowStock: Array.isArray(result?.lowStock) ? result.lowStock : [],
      maintenance: Array.isArray(result?.maintenance) ? result.maintenance : [],
      responsible: Array.isArray(result?.responsible) ? result.responsible : [],
      consumables: Array.isArray(result?.consumables) ? result.consumables : []
    }
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được báo cáo.'))
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  filterForm.value = filters()
}

let filterTimer = null
watch(filterForm, value => {
  clearTimeout(filterTimer)
  filterTimer = setTimeout(() => {
    appliedFilters.value = { ...value }
    load()
  }, 180)
}, { deep: true })

const downloadBlob = (blob, filename, type) => {
  if (typeof Blob === 'undefined' || !(blob instanceof Blob) || blob.size === 0) {
    throw new Error('Tệp báo cáo rỗng hoặc không hợp lệ.')
  }
  const responseType = (blob.type || '').split(';')[0]
  if (responseType && responseType !== type) {
    throw new Error('Máy chủ trả về tệp báo cáo không đúng định dạng.')
  }
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}

const exportReport = async () => {
  exporting.value = true
  try {
    const blob = await reportsApi.export(appliedFilters.value)
    downloadBlob(blob, `BaoCaoVanHanh_${Date.now()}.xlsx`, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet')
    message.success('Đã xuất báo cáo Excel theo điều kiện lọc.')
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể xuất báo cáo Excel.'))
  } finally {
    exporting.value = false
  }
}

const exportPdf = async () => {
  exportingPdf.value = true
  try {
    const blob = await reportsApi.exportPdf(appliedFilters.value)
    downloadBlob(blob, `BaoCaoVanHanh_${Date.now()}.pdf`, 'application/pdf')
    message.success('Đã xuất báo cáo PDF theo điều kiện lọc.')
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không thể xuất báo cáo PDF.'))
  } finally {
    exportingPdf.value = false
  }
}

const goTo = route => router.push(route)

onMounted(async () => {
  await loadOptions()
  await load()
})
</script>

<style scoped>
.reports-page { display: flex; flex-direction: column; gap: 24px; padding: 0; }
.reports-page :deep(.page-header) { margin-bottom: 0; }
.reports-page :deep(.page-actions .ant-btn) { min-width: 112px; height: 36px; }
.filter-card, .report-card, .summary-card { border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; box-shadow: 0 2px 8px rgba(15, 35, 63, .035); }
.filter-card :deep(.ant-card-body) { padding: 16px 20px 14px; }
.filter-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)) auto; align-items: end; gap: 16px; width: 100%; }
.filter-field { min-width: 0; }
.filter-field label { display: block; margin-bottom: 7px; color: var(--color-ink); font-size: 13px; font-weight: 600; }
.filter-field :deep(.ant-input), .filter-field :deep(.ant-select), .filter-field :deep(.ant-select-selector) {
  width: 100%;
  min-height: 38px;
  box-sizing: border-box;
}
.filter-actions { display: flex; gap: 8px; }
.filter-actions .ant-btn { min-height: 38px; padding-inline: 14px; white-space: nowrap; }
.reports-spin { display: block; }
.reports-spin :deep(.ant-spin-container) { display: block; }
.overview-section { margin-bottom: 24px; }
.main-grid { margin-bottom: 24px; }
.reports-spin :deep(.ant-spin-container > .detail-card) { margin-bottom: 0; }
.overview-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
.summary-card :deep(.ant-card-body) { display: flex; align-items: flex-start; gap: 14px; padding: 18px 20px; }
.summary-icon { display: grid; width: 44px; height: 44px; flex: 0 0 44px; place-items: center; border-radius: 10px; font-size: 20px; }
.summary-icon--primary { color: var(--color-primary); background: #fff1eb; }
.summary-icon--info { color: #2563eb; background: #eff6ff; }
.summary-icon--warning { color: #d97706; background: #fffbeb; }
.summary-icon--success { color: #059669; background: #ecfdf5; }
.summary-copy { min-width: 0; }
.summary-label { display: block; }
.summary-label { color: var(--color-secondary); font-size: 13px; line-height: 1.35; }
.summary-value { display: block; margin-top: 5px; overflow-wrap: anywhere; color: var(--color-ink); font-size: 26px; line-height: 1.15; }
.main-grid { display: grid; grid-template-columns: minmax(0, 1.15fr) minmax(360px, .95fr); gap: 20px; }
.report-card :deep(.ant-card-head) { min-height: 54px; padding: 0 20px; border-bottom-color: #f0f0f0; }
.report-card :deep(.ant-card-head-title) { padding: 16px 0; color: var(--color-ink); font-size: 17px; }
.report-card :deep(.ant-card-body) { padding: 18px 20px; }
.status-list { display: flex; flex-direction: column; gap: 14px; }
.status-row { display: block; width: 100%; padding: 0; border: 0; color: inherit; text-align: left; background: transparent; border-radius: 7px; cursor: pointer; }
.status-row:not(:disabled):hover { background: rgba(77, 145, 216, .06); }
.status-row:not(:disabled):hover .status-name { color: var(--color-primary); }
.status-row:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 4px; }
.status-row:disabled { cursor: default; }
.status-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 7px; }
.status-name { color: var(--color-ink); font-size: 14px; }
.status-heading strong { color: var(--color-ink); font-size: 14px; }
.status-track { height: 7px; overflow: hidden; border-radius: 999px; background: #f1f5f9; }
.status-fill { display: block; height: 100%; border-radius: inherit; transition: width .2s ease; }
.status-fill--green { background: #22c55e; }
.status-fill--blue { background: #3b82f6; }
.status-fill--red { background: #ef4444; }
.status-fill--orange { background: #f59e0b; }
.status-fill--gold { background: #eab308; }
.status-fill--purple { background: #8b5cf6; }
.status-fill--default { background: var(--color-primary); }
.attention-list { display: flex; flex-direction: column; gap: 10px; }
.attention-item { display: grid; grid-template-columns: 40px minmax(0, 1fr) auto auto; align-items: center; gap: 12px; padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
.attention-item:last-child { padding-bottom: 0; border-bottom: 0; }
.attention-icon { display: grid; width: 40px; height: 40px; place-items: center; border-radius: 10px; font-size: 18px; }
.attention-icon--danger { color: #dc2626; background: #fef2f2; }
.attention-icon--warning { color: #d97706; background: #fffbeb; }
.attention-icon--info { color: #2563eb; background: #eff6ff; }
.attention-icon--success { color: #16a34a; background: #ecfdf5; }
.attention-copy { min-width: 0; }
.attention-copy strong, .attention-copy span { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.attention-copy strong { color: var(--color-ink); font-size: 13px; }
.attention-copy span { margin-top: 3px; color: var(--color-secondary); font-size: 12px; }
.attention-count { color: var(--color-ink); font-size: 15px; white-space: nowrap; }
.attention-action { padding: 0; color: var(--color-primary); }
.attention-action :deep(.anticon) { margin-left: 3px; }
.detail-card { width: 100%; }
.operation-tabs :deep(.ant-tabs-nav) { margin-bottom: 14px; }
.operation-tabs :deep(.ant-tabs-tab-active .ant-tabs-tab-btn) { color: var(--color-primary); }
.operation-tabs :deep(.ant-tabs-ink-bar) { background: var(--color-primary); }
.detail-card :deep(.ant-table-wrapper) { overflow: hidden; }
.detail-card :deep(.ant-table-thead > tr > th) { color: var(--color-ink); background: #fafafa; font-weight: 600; }
.detail-card :deep(.ant-empty) { margin: 12px 0; }
.cell-ellipsis { display: block; max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.status-details-description { margin: 0 0 14px; color: var(--color-secondary); }

@media (max-width: 1199px) {
  .filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .filter-actions { grid-column: 1 / -1; }
  .overview-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 767px) {
  .reports-page { gap: 14px; }
  .overview-section, .main-grid { margin-bottom: 16px; }
  .filter-grid, .main-grid { grid-template-columns: 1fr; gap: 12px; }
  .filter-actions { grid-column: auto; }
  .filter-actions .ant-btn { flex: 1; }
  .overview-grid { grid-template-columns: 1fr; gap: 12px; }
  .summary-card :deep(.ant-card-body), .report-card :deep(.ant-card-body) { padding: 15px; }
  .report-card :deep(.ant-card-head) { padding: 0 15px; }
  .attention-item { grid-template-columns: 36px minmax(0, 1fr) auto; gap: 10px; }
  .attention-icon { width: 36px; height: 36px; }
  .attention-action { grid-column: 2 / -1; justify-self: start; }
}

@media (max-width: 479px) {
  .filter-actions { flex-direction: column; }
  .filter-actions .ant-btn { width: 100%; }
  .summary-value { font-size: 21px; }
}
</style>
