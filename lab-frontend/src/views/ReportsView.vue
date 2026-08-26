<template>
  <div class="reports-page">
    <PageHeader
      title="Báo cáo vận hành"
      subtitle="Tổng hợp tình trạng tài sản và các hoạt động cần theo dõi."
    >
      <template #actions>
        <a-button :loading="exportingPdf" @click="exportPdf">
          Xuất PDF
        </a-button>
        <a-button type="primary" :loading="exporting" @click="exportReport">
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
            <a-button type="primary" :loading="loading" @click="applyFilters">
              <template #icon><FilterOutlined /></template>
              Lọc
            </a-button>
            <a-button :disabled="loading" @click="resetFilters">
              <template #icon><ReloadOutlined /></template>
              Đặt lại
            </a-button>
          </div>
        </div>
        <div class="applied-filters" aria-live="polite">
          <span class="applied-filters-label">Đang áp dụng:</span>
          <a-tag v-for="item in appliedFilterLabels" :key="item">{{ item }}</a-tag>
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
              <small>{{ item.description }}</small>
            </div>
          </a-card>
        </div>
      </section>

      <section class="main-grid" aria-label="Tình hình và cảnh báo tài sản">
        <a-card :bordered="false" class="report-card status-card">
          <template #title>Tình hình tài sản</template>
          <div v-if="statusRows.length" class="status-list">
            <div v-for="item in statusRows" :key="item.value" class="status-row">
              <div class="status-heading">
                <span class="status-name">{{ statusLabel(item.value) }}</span>
                <strong>{{ formatNumber(item.count) }}</strong>
              </div>
              <div class="status-track" aria-hidden="true">
                <span
                  class="status-fill"
                  :class="`status-fill--${statusColor(item.value)}`"
                  :style="{ width: `${statusPercent(item.count)}%` }"
                />
              </div>
            </div>
          </div>
          <a-empty v-else description="Chưa có dữ liệu" />
        </a-card>

        <a-card :bordered="false" class="report-card attention-card">
          <template #title>Cần chú ý</template>
          <div v-if="activeAlerts.length" class="attention-list">
            <div v-for="alert in activeAlerts" :key="alert.key" class="attention-item">
              <span class="attention-icon" :class="`attention-icon--${alert.tone}`">
                <component :is="alert.icon" />
              </span>
              <div class="attention-copy">
                <strong>{{ alert.title }}</strong>
                <span>{{ alert.description }}</span>
              </div>
              <strong class="attention-count">{{ formatNumber(alert.count) }}</strong>
              <a-button type="link" class="attention-action" @click="goTo(alert.route)">
                Xem
                <ArrowRightOutlined />
              </a-button>
            </div>
          </div>
          <a-empty v-else description="Không có cảnh báo" />
        </a-card>
      </section>

      <a-card :bordered="false" class="report-card detail-card">
        <template #title>Chi tiết vận hành</template>
        <a-tabs v-model:active-key="activeTab" class="operation-tabs">
          <a-tab-pane key="borrow" tab="Mượn trả">
            <a-table
              v-if="report.borrowed.length"
              :data-source="report.borrowed"
              :columns="borrowColumns"
              :pagination="{ pageSize: 8, hideOnSinglePage: true }"
              :scroll="{ x: 680 }"
              row-key="id"
              size="small"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'expectedReturnDate'">
                  {{ formatDate(record.expectedReturnDate) }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="record.overdue ? 'red' : 'blue'">
                    {{ record.overdue ? 'Quá hạn' : 'Đang mượn' }}
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

          <a-tab-pane key="maintenance" tab="Bảo trì">
            <a-table
              v-if="report.maintenance.length"
              :data-source="report.maintenance"
              :columns="maintenanceColumns"
              :pagination="{ pageSize: 8, hideOnSinglePage: true }"
              :scroll="{ x: 680 }"
              row-key="id"
              size="small"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'maintenanceDate'">
                  {{ formatDate(record.maintenanceDate) }}
                </template>
                <template v-else-if="column.key === 'cost'">
                  {{ formatCurrency(record.cost) }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <StatusBadge :status="record.status" />
                </template>
                <template v-else>
                  <span class="cell-ellipsis" :title="cellText(record[column.dataIndex])">
                    {{ cellText(record[column.dataIndex]) }}
                  </span>
                </template>
              </template>
            </a-table>
            <a-empty v-else description="Chưa có dữ liệu bảo trì" />
          </a-tab-pane>

          <a-tab-pane key="consumables" tab="Vật tư">
            <a-table
              v-if="report.consumables.length"
              :data-source="report.consumables"
              :columns="consumableColumns"
              :pagination="{ pageSize: 8, hideOnSinglePage: true }"
              :scroll="{ x: 600 }"
              row-key="id"
              size="small"
            >
              <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'quantity'">
                  {{ formatNumber(record.quantity) }} {{ record.unit || '' }}
                </template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="record.quantity <= record.minQuantity ? 'orange' : 'green'">
                    {{ record.quantity <= record.minQuantity ? 'Sắp hết' : 'Đủ tồn' }}
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
    </a-spin>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { message } from 'ant-design-vue'
import {
  AppstoreOutlined,
  ArrowRightOutlined,
  ClockCircleOutlined,
  DollarOutlined,
  FilterOutlined,
  ReloadOutlined,
  ToolOutlined,
  WarningOutlined
} from '@ant-design/icons-vue'
import StatusBadge from '../components/StatusBadge.vue'
import PageHeader from '../components/PageHeader.vue'
import { assetCategoryApi } from '../api/assetCategoryApi'
import { locationApi } from '../api/locationApi'
import { reportsApi } from '../api/reportsApi'
import { STATUS, normalizeStatus, statusColor, statusLabel } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import router from '../router'

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
  warrantySoon: [],
  maintenance: [],
  consumables: []
})

const assetStatusOrder = [
  STATUS.AVAILABLE,
  STATUS.BORROWED,
  STATUS.MAINTENANCE_IN_PROGRESS,
  STATUS.UNDER_WARRANTY,
  STATUS.BROKEN
]

const borrowColumns = [
  { title: 'Người mượn', dataIndex: 'user', key: 'user', width: 150, ellipsis: true },
  { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment', width: 220, ellipsis: true },
  { title: 'Hạn trả', key: 'expectedReturnDate', width: 130 },
  { title: 'Trạng thái', key: 'status', width: 130 }
]
const maintenanceColumns = [
  { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment', width: 220, ellipsis: true },
  { title: 'Ngày thực hiện', key: 'maintenanceDate', width: 140 },
  { title: 'Người thực hiện', dataIndex: 'performedBy', key: 'performedBy', width: 170, ellipsis: true },
  { title: 'Chi phí', key: 'cost', width: 130 },
  { title: 'Trạng thái', key: 'status', width: 150 }
]
const consumableColumns = [
  { title: 'Vật tư', dataIndex: 'name', key: 'name', width: 280, ellipsis: true },
  { title: 'Tồn kho', key: 'quantity', width: 150 },
  { title: 'Mức tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity', width: 150 },
  { title: 'Trạng thái', key: 'status', width: 140 }
]

const summaryCards = computed(() => [
  {
    label: 'Tổng tài sản',
    value: formatNumber(report.value.totals.assets),
    description: 'Theo điều kiện đang áp dụng',
    icon: AppstoreOutlined,
    tone: 'primary'
  },
  {
    label: 'Đang mượn / Quá hạn',
    value: `${formatNumber(report.value.totals.borrowed)} / ${formatNumber(report.value.totals.overdue)}`,
    description: 'Đang mượn / cần nhắc trả',
    icon: ClockCircleOutlined,
    tone: 'info'
  },
  {
    label: 'Đang hỏng / Bảo hành',
    value: `${formatNumber(report.value.totals.broken)} / ${formatNumber(report.value.totals.underWarranty)}`,
    description: 'Hỏng / đang bảo hành',
    icon: ToolOutlined,
    tone: 'warning'
  },
  {
    label: 'Chi phí bảo trì',
    value: formatCurrency(report.value.totals.maintenanceCost),
    description: 'Tổng chi phí trong kỳ',
    icon: DollarOutlined,
    tone: 'success'
  }
])

const appliedFilterLabels = computed(() => {
  const value = appliedFilters.value
  const labels = []
  if (value.from) labels.push(`Từ ${formatInputDate(value.from)}`)
  if (value.to) labels.push(`Đến ${formatInputDate(value.to)}`)
  if (value.categoryId) {
    labels.push(`Danh mục: ${categories.value.find(item => item.id === value.categoryId)?.name || value.categoryId}`)
  }
  if (value.locationNodeId) {
    const location = locations.value.find(item => item.id === value.locationNodeId)
    labels.push(`Vị trí: ${location ? `${location.code} — ${location.name}` : value.locationNodeId}`)
  }
  return labels.length ? labels : ['Tất cả dữ liệu']
})

const statusRows = computed(() => {
  if (!report.value.byStatus.length) return []
  const counts = new Map(
    report.value.byStatus.map(item => [normalizeStatus(item.status), Number(item.count || 0)])
  )
  return assetStatusOrder.map(value => ({ value, count: counts.get(value) || 0 }))
})

const statusTotal = computed(() => statusRows.value.reduce((total, item) => total + item.count, 0))

const alertCards = computed(() => [
  {
    key: 'overdue',
    title: 'Tài sản đang mượn quá hạn',
    count: report.value.borrowed.filter(item => item.overdue).length,
    description: 'Các phiếu mượn đã quá hạn trả.',
    icon: WarningOutlined,
    tone: 'danger',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'low-stock',
    title: 'Vật tư dưới mức tối thiểu',
    count: report.value.lowStock.length,
    description: 'Vật tư cần được kiểm tra và bổ sung.',
    icon: AppstoreOutlined,
    tone: 'warning',
    route: { name: 'ConsumableRequests' }
  },
  {
    key: 'warranty',
    title: 'Thiết bị sắp hết bảo hành',
    count: report.value.warrantySoon.length,
    description: 'Thiết bị hết hạn bảo hành trong 30 ngày.',
    icon: ToolOutlined,
    tone: 'info',
    route: { name: 'Devices', query: { status: 'warranty' } }
  }
])

const activeAlerts = computed(() => alertCards.value.filter(item => item.count > 0))

const formatNumber = value => Number(value || 0).toLocaleString('vi-VN')
const formatCurrency = value => `${Number(value || 0).toLocaleString('vi-VN')} ₫`
const formatDate = value => value ? new Date(value).toLocaleDateString('vi-VN') : '—'
const formatInputDate = value => {
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}
const cellText = value => value === null || value === undefined || value === '' ? '—' : String(value)
const statusPercent = count => statusTotal.value ? (Number(count || 0) / statusTotal.value) * 100 : 0

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
      warrantySoon: Array.isArray(result?.warrantySoon) ? result.warrantySoon : [],
      maintenance: Array.isArray(result?.maintenance) ? result.maintenance : [],
      consumables: Array.isArray(result?.consumables) ? result.consumables : []
    }
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được báo cáo.'))
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  appliedFilters.value = { ...filterForm.value }
  load()
}

const resetFilters = () => {
  filterForm.value = filters()
  appliedFilters.value = filters()
  load()
}

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
.reports-page { display: flex; flex-direction: column; gap: 18px; padding: 0; }
.reports-page :deep(.page-header) { margin-bottom: 0; }
.filter-card, .report-card, .summary-card { border: 1px solid var(--color-border, #e5e7eb); border-radius: 10px; box-shadow: none; }
.filter-card :deep(.ant-card-body) { padding: 14px 16px; }
.filter-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)) auto; align-items: end; gap: 12px; }
.filter-field { min-width: 0; }
.filter-field label { display: block; margin-bottom: 6px; color: var(--color-ink); font-size: 13px; font-weight: 600; }
.filter-actions { display: flex; gap: 8px; }
.filter-actions .ant-btn { white-space: nowrap; }
.applied-filters { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; margin-top: 12px; padding-top: 11px; border-top: 1px solid #f1f1f1; color: var(--color-secondary); font-size: 13px; }
.applied-filters .ant-tag { margin-inline-end: 0; color: var(--color-ink); background: #fff7f3; border-color: rgba(217, 119, 87, .28); }
.applied-filters-label { font-weight: 600; }
.reports-spin { display: flex; flex-direction: column; gap: 18px; }
.reports-spin :deep(.ant-spin-container) { display: flex; flex-direction: column; gap: 18px; }
.overview-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
.summary-card :deep(.ant-card-body) { display: flex; align-items: flex-start; gap: 12px; padding: 17px; }
.summary-icon { display: grid; width: 36px; height: 36px; flex: 0 0 36px; place-items: center; border-radius: 8px; font-size: 18px; }
.summary-icon--primary { color: var(--color-primary); background: #fff1eb; }
.summary-icon--info { color: #2563eb; background: #eff6ff; }
.summary-icon--warning { color: #d97706; background: #fffbeb; }
.summary-icon--success { color: #059669; background: #ecfdf5; }
.summary-copy { min-width: 0; }
.summary-label, .summary-copy small { display: block; }
.summary-label { color: var(--color-secondary); font-size: 13px; line-height: 1.35; }
.summary-value { display: block; margin-top: 5px; overflow-wrap: anywhere; color: var(--color-ink); font-size: 23px; line-height: 1.2; }
.summary-copy small { margin-top: 5px; color: var(--color-secondary); font-size: 12px; line-height: 1.35; }
.main-grid { display: grid; grid-template-columns: minmax(0, 1.25fr) minmax(320px, .9fr); gap: 18px; }
.report-card :deep(.ant-card-head) { min-height: 53px; padding: 0 18px; border-bottom-color: #f0f0f0; }
.report-card :deep(.ant-card-head-title) { padding: 16px 0; color: var(--color-ink); font-size: 16px; }
.report-card :deep(.ant-card-body) { padding: 18px; }
.status-list { display: flex; flex-direction: column; gap: 16px; }
.status-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 7px; }
.status-name { color: var(--color-ink); font-size: 14px; }
.status-heading strong { color: var(--color-ink); font-size: 14px; }
.status-track { height: 7px; overflow: hidden; border-radius: 999px; background: #f1f5f9; }
.status-fill { display: block; height: 100%; border-radius: inherit; transition: width .2s ease; }
.status-fill--green { background: #22c55e; }
.status-fill--blue { background: #3b82f6; }
.status-fill--red { background: #ef4444; }
.status-fill--orange { background: #f59e0b; }
.status-fill--purple { background: #8b5cf6; }
.status-fill--default { background: var(--color-primary); }
.attention-list { display: flex; flex-direction: column; gap: 10px; }
.attention-item { display: grid; grid-template-columns: 32px minmax(0, 1fr) auto auto; align-items: center; gap: 10px; padding: 11px 0; border-bottom: 1px solid #f0f0f0; }
.attention-item:last-child { padding-bottom: 0; border-bottom: 0; }
.attention-icon { display: grid; width: 32px; height: 32px; place-items: center; border-radius: 8px; font-size: 16px; }
.attention-icon--danger { color: #dc2626; background: #fef2f2; }
.attention-icon--warning { color: #d97706; background: #fffbeb; }
.attention-icon--info { color: #2563eb; background: #eff6ff; }
.attention-copy { min-width: 0; }
.attention-copy strong, .attention-copy span { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.attention-copy strong { color: var(--color-ink); font-size: 13px; }
.attention-copy span { margin-top: 3px; color: var(--color-secondary); font-size: 12px; }
.attention-count { color: var(--color-primary); font-size: 18px; }
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

@media (max-width: 1199px) {
  .filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .filter-actions { grid-column: 1 / -1; }
  .overview-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 767px) {
  .reports-page { gap: 14px; }
  .filter-grid, .main-grid { grid-template-columns: 1fr; gap: 12px; }
  .filter-actions { grid-column: auto; }
  .filter-actions .ant-btn { flex: 1; }
  .overview-grid { grid-template-columns: 1fr; gap: 12px; }
  .summary-card :deep(.ant-card-body), .report-card :deep(.ant-card-body) { padding: 15px; }
  .report-card :deep(.ant-card-head) { padding: 0 15px; }
  .attention-item { grid-template-columns: 32px minmax(0, 1fr) auto; }
  .attention-action { grid-column: 2 / -1; justify-self: start; }
  .applied-filters { align-items: flex-start; }
}

@media (max-width: 479px) {
  .filter-actions { flex-direction: column; }
  .filter-actions .ant-btn { width: 100%; }
  .summary-value { font-size: 21px; }
}
</style>
