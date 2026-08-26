<template>
  <div class="reports-page">
    <PageHeader title="Báo cáo vận hành" subtitle="Tổng hợp tài sản, mượn trả, bảo trì và vật tư.">
      <template #actions>
        <a-button type="primary" :loading="exporting" @click="exportReport">
          Xuất Excel
        </a-button>
        <a-button :loading="exportingPdf" @click="exportPdf">
          Xuất PDF
        </a-button>
      </template>
    </PageHeader>

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
          <label>Vị trí/phòng</label>
          <a-select v-model:value="filterForm.locationNodeId" allow-clear placeholder="Tất cả vị trí">
            <a-select-option v-for="location in locations" :key="location.id" :value="location.id">
              {{ location.code }} — {{ location.name }}
            </a-select-option>
          </a-select>
        </div>
        <div class="filter-field">
          <label>Trạng thái tài sản</label>
          <a-select v-model:value="filterForm.status" allow-clear placeholder="Tất cả trạng thái">
            <a-select-option v-for="option in statusOptions" :key="option.value" :value="option.value">
              {{ option.label }}
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
        <span>Đang áp dụng:</span>
        <a-tag v-for="item in appliedFilterLabels" :key="item">{{ item }}</a-tag>
      </div>
    </a-card>

    <a-spin :spinning="loading" class="reports-spin">
      <section class="overview-grid" aria-label="Tổng quan báo cáo">
        <a-card v-for="item in summaryCards" :key="item.label" :bordered="false" class="summary-card">
          <div class="summary-icon" :class="`summary-icon--${item.tone}`">
            <component :is="item.icon" />
          </div>
          <div class="summary-copy">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </div>
        </a-card>
      </section>

      <section class="analysis-grid" aria-label="Phân tích tài sản">
        <a-card :bordered="false" class="report-card">
          <template #title>Tài sản theo trạng thái</template>
          <div v-if="report.byStatus.length" class="status-bars">
            <div v-for="item in report.byStatus" :key="item.status" class="status-row">
              <div class="status-row-heading">
                <StatusBadge :status="item.status" />
                <strong>{{ item.count }}</strong>
              </div>
              <div class="bar-track" aria-hidden="true">
                <span class="bar-fill" :class="`bar-fill--${statusColor(item.status)}`" :style="{ width: `${statusPercent(item.count)}%` }" />
              </div>
            </div>
          </div>
          <a-empty v-else description="Chưa có dữ liệu" />
        </a-card>

        <a-card :bordered="false" class="report-card">
          <template #title>
            <div class="card-title-row">
              <span>Tài sản theo vị trí</span>
              <span v-if="report.byLocation.length" class="card-count">{{ report.byLocation.length }} vị trí</span>
            </div>
          </template>
          <div v-if="report.byLocation.length" class="location-bars">
            <div v-for="item in visibleLocations" :key="item.location" class="location-row">
              <div class="location-row-heading">
                <span :title="item.location || 'Chưa phân loại'">{{ item.location || 'Chưa phân loại' }}</span>
                <strong>{{ item.count }}</strong>
              </div>
              <div class="bar-track" aria-hidden="true">
                <span class="bar-fill bar-fill--primary" :style="{ width: `${locationPercent(item.count)}%` }" />
              </div>
            </div>
            <a-button v-if="hasMoreLocations" type="link" class="show-more-button" @click="showAllLocations = !showAllLocations">
              {{ showAllLocations ? 'Thu gọn' : 'Xem thêm' }}
            </a-button>
          </div>
          <a-empty v-else description="Chưa có dữ liệu" />
        </a-card>
      </section>

      <section class="alerts-grid" aria-label="Cảnh báo vận hành">
        <a-card v-for="alert in alertCards" :key="alert.key" :bordered="false" class="alert-card">
          <div class="alert-heading">
            <div class="alert-title">
              <span class="alert-icon" :class="`alert-icon--${alert.tone}`">
                <component :is="alert.icon" />
              </span>
              <h3>{{ alert.title }}</h3>
            </div>
            <strong class="alert-count">{{ alert.count }}</strong>
          </div>
          <ul v-if="alert.items.length" class="alert-list">
            <li v-for="item in alert.items.slice(0, 5)" :key="item.key">
              <span>{{ item.text }}</span>
              <small>{{ item.detail }}</small>
            </li>
          </ul>
          <a-empty v-else description="Không có cảnh báo" />
          <a-button type="link" class="alert-link" @click="goTo(alert.route)">
            Xem tất cả
            <ArrowRightOutlined />
          </a-button>
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
                <template v-if="column.key === 'expectedReturnDate'">{{ formatDate(record.expectedReturnDate) }}</template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="record.overdue ? 'red' : 'blue'">{{ record.overdue ? 'Quá hạn' : 'Đang mượn' }}</a-tag>
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
                <template v-if="column.key === 'maintenanceDate'">{{ formatDate(record.maintenanceDate) }}</template>
                <template v-else-if="column.key === 'cost'">{{ formatCurrency(record.cost) }}</template>
                <template v-else-if="column.key === 'status'"><StatusBadge :status="record.status" /></template>
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
                <template v-if="column.key === 'quantity'">{{ record.quantity }} {{ record.unit }}</template>
                <template v-else-if="column.key === 'status'">
                  <a-tag :color="record.quantity <= record.minQuantity ? 'orange' : 'green'">
                    {{ record.quantity <= record.minQuantity ? 'Sắp hết' : 'Đủ tồn' }}
                  </a-tag>
                </template>
              </template>
            </a-table>
            <a-empty v-else description="Chưa có dữ liệu vật tư" />
          </a-tab-pane>
        </a-tabs>
      </a-card>

      <a-card :bordered="false" class="report-card warranty-card">
        <template #title>Bảo hành sắp hết trong 30 ngày</template>
        <a-table
          v-if="report.warrantySoon.length"
          :data-source="report.warrantySoon"
          :columns="warrantyColumns"
          :pagination="{ pageSize: 8, hideOnSinglePage: true }"
          :scroll="{ x: 560 }"
          row-key="id"
          size="small"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'warrantyExpiry'">{{ formatDate(record.warrantyExpiry) }}</template>
          </template>
        </a-table>
        <a-empty v-else description="Không có tài sản sắp hết bảo hành" />
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
import { STATUS, statusColor, statusLabel } from '../constants/business'
import { getApiErrorMessage } from '../utils/apiError'
import router from '../router'

const filters = () => ({ from: '', to: '', categoryId: null, locationNodeId: null, status: '' })
const filterForm = ref(filters())
const appliedFilters = ref(filters())
const categories = ref([])
const locations = ref([])
const loading = ref(false)
const exporting = ref(false)
const exportingPdf = ref(false)
const activeTab = ref('borrow')
const showAllLocations = ref(false)
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

const statusOptions = [
  STATUS.AVAILABLE,
  STATUS.BORROWED,
  STATUS.BROKEN,
  STATUS.UNDER_WARRANTY,
  STATUS.MAINTENANCE_IN_PROGRESS,
  STATUS.MAINTENANCE_COMPLETED
].map(value => ({ value, label: statusLabel(value) }))

const borrowColumns = [
  { title: 'Người mượn', dataIndex: 'user', key: 'user', width: 150 },
  { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment', width: 220 },
  { title: 'Hạn trả', key: 'expectedReturnDate', width: 130 },
  { title: 'Trạng thái', key: 'status', width: 130 }
]
const maintenanceColumns = [
  { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment', width: 220 },
  { title: 'Ngày thực hiện', key: 'maintenanceDate', width: 140 },
  { title: 'Người thực hiện', dataIndex: 'performedBy', key: 'performedBy', width: 170 },
  { title: 'Chi phí', key: 'cost', width: 130 },
  { title: 'Trạng thái', key: 'status', width: 150 }
]
const consumableColumns = [
  { title: 'Tên vật tư', dataIndex: 'name', key: 'name', width: 280 },
  { title: 'Tồn kho', key: 'quantity', width: 150 },
  { title: 'Mức tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity', width: 150 },
  { title: 'Trạng thái', key: 'status', width: 140 }
]
const warrantyColumns = [
  { title: 'Thiết bị', dataIndex: 'name', key: 'name', width: 280 },
  { title: 'Số seri', dataIndex: 'serial', key: 'serial', width: 220 },
  { title: 'Hạn bảo hành', key: 'warrantyExpiry', width: 160 }
]

const summaryCards = computed(() => [
  { label: 'Tổng tài sản', value: formatNumber(report.value.totals.assets), icon: AppstoreOutlined, tone: 'primary' },
  { label: 'Đang mượn', value: formatNumber(report.value.totals.borrowed), icon: ClockCircleOutlined, tone: 'info' },
  { label: 'Quá hạn', value: formatNumber(report.value.totals.overdue), icon: WarningOutlined, tone: 'danger' },
  { label: 'Hỏng/Bảo hành', value: formatNumber(Number(report.value.totals.broken || 0) + Number(report.value.totals.underWarranty || 0)), icon: ToolOutlined, tone: 'warning' },
  { label: 'Chi phí bảo trì', value: formatCurrency(report.value.totals.maintenanceCost), icon: DollarOutlined, tone: 'success' }
])

const appliedFilterLabels = computed(() => {
  const value = appliedFilters.value
  const labels = []
  if (value.from) labels.push(`Từ ${formatInputDate(value.from)}`)
  if (value.to) labels.push(`Đến ${formatInputDate(value.to)}`)
  if (value.categoryId) labels.push(`Danh mục: ${categories.value.find(item => item.id === value.categoryId)?.name || value.categoryId}`)
  if (value.locationNodeId) {
    const location = locations.value.find(item => item.id === value.locationNodeId)
    labels.push(`Vị trí: ${location ? `${location.code} — ${location.name}` : value.locationNodeId}`)
  }
  if (value.status) labels.push(`Trạng thái: ${statusLabel(value.status)}`)
  return labels.length ? labels : ['Tất cả tài sản']
})

const statusTotal = computed(() => report.value.byStatus.reduce((total, item) => total + Number(item.count || 0), 0))
const maxLocationCount = computed(() => Math.max(...report.value.byLocation.map(item => Number(item.count || 0)), 0))
const visibleLocations = computed(() => showAllLocations.value ? report.value.byLocation : report.value.byLocation.slice(0, 8))
const hasMoreLocations = computed(() => report.value.byLocation.length > 8)

const overdueBorrowed = computed(() => report.value.borrowed.filter(item => item.overdue))
const alertCards = computed(() => [
  {
    key: 'overdue',
    title: 'Mượn quá hạn',
    count: overdueBorrowed.value.length,
    items: overdueBorrowed.value.slice(0, 5).map(item => ({
      key: item.id,
      text: item.equipment,
      detail: `${item.user} — hạn ${formatDate(item.expectedReturnDate)}`
    })),
    icon: WarningOutlined,
    tone: 'danger',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'low-stock',
    title: 'Vật tư sắp hết',
    count: report.value.lowStock.length,
    items: report.value.lowStock.slice(0, 5).map(item => ({
      key: item.id,
      text: item.name,
      detail: `Còn ${item.quantity} ${item.unit} — tối thiểu ${item.minQuantity}`
    })),
    icon: AppstoreOutlined,
    tone: 'warning',
    route: { name: 'ConsumableRequests' }
  },
  {
    key: 'warranty',
    title: 'Bảo hành sắp hết trong 30 ngày',
    count: report.value.warrantySoon.length,
    items: report.value.warrantySoon.slice(0, 5).map(item => ({
      key: item.id,
      text: item.name,
      detail: `Hạn bảo hành ${formatDate(item.warrantyExpiry)}`
    })),
    icon: ToolOutlined,
    tone: 'info',
    route: { name: 'Devices', query: { status: 'warranty' } }
  }
])

const formatNumber = value => Number(value || 0).toLocaleString('vi-VN')
const formatCurrency = value => `${Number(value || 0).toLocaleString('vi-VN')} ₫`
const formatDate = value => value ? new Date(value).toLocaleDateString('vi-VN') : '—'
const formatInputDate = value => {
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}
const statusPercent = count => statusTotal.value ? Math.max(4, (Number(count || 0) / statusTotal.value) * 100) : 0
const locationPercent = count => maxLocationCount.value ? Math.max(4, (Number(count || 0) / maxLocationCount.value) * 100) : 0

const loadOptions = async () => {
  const [categoryResult, locationResult] = await Promise.allSettled([assetCategoryApi.getAll(), locationApi.getAll()])
  if (categoryResult.status === 'fulfilled') categories.value = Array.isArray(categoryResult.value) ? categoryResult.value : []
  if (locationResult.status === 'fulfilled') locations.value = Array.isArray(locationResult.value) ? locationResult.value.filter(item => item.isActive !== false) : []
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
    showAllLocations.value = false
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
.filter-card, .report-card, .alert-card, .summary-card { border-radius: 12px; box-shadow: 0 2px 10px rgba(17, 24, 39, .05); }
.filter-grid { display: grid; grid-template-columns: repeat(5, minmax(130px, 1fr)) auto; align-items: end; gap: 14px; }
.filter-field { min-width: 0; }
.filter-field label { display: block; margin-bottom: 7px; color: var(--color-ink); font-size: 13px; font-weight: 600; }
.filter-actions { display: flex; gap: 8px; }
.filter-actions .ant-btn { white-space: nowrap; }
.applied-filters { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; margin-top: 16px; padding-top: 13px; border-top: 1px solid var(--color-border, #e5e7eb); color: var(--color-secondary); font-size: 13px; }
.applied-filters .ant-tag { margin-inline-end: 0; color: var(--color-ink); background: #fff7f3; border-color: rgba(217, 119, 87, .28); }
.reports-spin { display: flex; flex-direction: column; gap: 18px; }
.reports-spin :deep(.ant-spin-container) { display: flex; flex-direction: column; gap: 18px; }
.overview-grid { display: grid; grid-template-columns: repeat(5, minmax(160px, 1fr)); gap: 16px; }
.summary-card :deep(.ant-card-body) { display: flex; min-height: 108px; align-items: center; gap: 14px; padding: 18px; }
.summary-icon { display: grid; width: 44px; height: 44px; flex: 0 0 44px; place-items: center; border-radius: 12px; font-size: 22px; }
.summary-icon--primary { color: var(--color-primary); background: #fff1eb; }
.summary-icon--info { color: #2563eb; background: #eff6ff; }
.summary-icon--danger { color: #dc2626; background: #fef2f2; }
.summary-icon--warning { color: #d97706; background: #fffbeb; }
.summary-icon--success { color: #059669; background: #ecfdf5; }
.summary-copy { min-width: 0; }
.summary-copy span { display: block; color: var(--color-secondary); font-size: 14px; line-height: 1.35; }
.summary-copy strong { display: block; margin-top: 7px; overflow-wrap: anywhere; color: var(--color-ink); font-size: 25px; line-height: 1.2; }
.analysis-grid { display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(0, .95fr); gap: 18px; }
.report-card :deep(.ant-card-head) { min-height: 56px; padding: 0 20px; border-bottom-color: #f0f0f0; }
.report-card :deep(.ant-card-head-title) { padding: 17px 0; color: var(--color-ink); font-size: 17px; }
.report-card :deep(.ant-card-body) { padding: 20px; }
.card-title-row { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.card-count { color: var(--color-secondary); font-size: 12px; font-weight: 400; }
.status-bars, .location-bars { display: flex; flex-direction: column; gap: 15px; }
.status-row-heading, .location-row-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 7px; }
.status-row-heading strong, .location-row-heading strong { flex: 0 0 auto; color: var(--color-ink); font-size: 14px; }
.location-row-heading span { overflow: hidden; color: var(--color-ink); font-size: 14px; text-overflow: ellipsis; white-space: nowrap; }
.bar-track { height: 8px; overflow: hidden; border-radius: 999px; background: #f1f5f9; }
.bar-fill { display: block; height: 100%; min-width: 4px; border-radius: inherit; transition: width .2s ease; }
.bar-fill--green { background: #22c55e; }
.bar-fill--blue { background: #3b82f6; }
.bar-fill--red { background: #ef4444; }
.bar-fill--orange { background: #f59e0b; }
.bar-fill--purple { background: #8b5cf6; }
.bar-fill--default, .bar-fill--primary { background: var(--color-primary); }
.show-more-button { align-self: flex-start; padding: 0; color: var(--color-primary); }
.alerts-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 18px; }
.alert-card :deep(.ant-card-body) { display: flex; min-height: 214px; flex-direction: column; padding: 18px; }
.alert-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.alert-title { display: flex; min-width: 0; align-items: center; gap: 10px; }
.alert-title h3 { margin: 0; overflow: hidden; color: var(--color-ink); font-size: 15px; text-overflow: ellipsis; white-space: nowrap; }
.alert-icon { display: grid; width: 34px; height: 34px; flex: 0 0 34px; place-items: center; border-radius: 10px; font-size: 17px; }
.alert-icon--danger { color: #dc2626; background: #fef2f2; }
.alert-icon--warning { color: #d97706; background: #fffbeb; }
.alert-icon--info { color: #2563eb; background: #eff6ff; }
.alert-count { color: var(--color-primary); font-size: 22px; }
.alert-list { display: flex; flex: 1; flex-direction: column; gap: 9px; margin: 16px 0 10px; padding: 0; list-style: none; }
.alert-list li { display: flex; min-width: 0; flex-direction: column; gap: 2px; padding-left: 10px; border-left: 3px solid rgba(217, 119, 87, .35); }
.alert-list li span, .alert-list li small { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.alert-list li span { color: var(--color-ink); font-size: 13px; }
.alert-list li small { color: var(--color-secondary); font-size: 12px; }
.alert-link { align-self: flex-start; margin-top: auto; padding: 0; color: var(--color-primary); }
.detail-card, .warranty-card { width: 100%; }
.operation-tabs :deep(.ant-tabs-nav) { margin-bottom: 14px; }
.operation-tabs :deep(.ant-tabs-tab-active .ant-tabs-tab-btn) { color: var(--color-primary); }
.operation-tabs :deep(.ant-tabs-ink-bar) { background: var(--color-primary); }
.operation-tabs :deep(.ant-table-wrapper), .warranty-card :deep(.ant-table-wrapper) { overflow: hidden; }
.report-card :deep(.ant-table-thead > tr > th), .warranty-card :deep(.ant-table-thead > tr > th) { color: var(--color-ink); background: #fafafa; font-weight: 600; }
.report-card :deep(.ant-empty), .warranty-card :deep(.ant-empty) { margin: 12px 0; }

@media (max-width: 1199px) {
  .filter-grid { grid-template-columns: repeat(3, minmax(150px, 1fr)); }
  .filter-actions { grid-column: 1 / -1; }
  .overview-grid { grid-template-columns: repeat(3, minmax(170px, 1fr)); }
  .alerts-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 767px) {
  .reports-page { gap: 14px; }
  .filter-grid, .overview-grid, .analysis-grid, .alerts-grid { grid-template-columns: 1fr; gap: 14px; }
  .filter-actions { grid-column: auto; }
  .filter-actions .ant-btn { flex: 1; }
  .summary-card :deep(.ant-card-body) { min-height: 92px; padding: 15px; }
  .report-card :deep(.ant-card-head) { padding: 0 16px; }
  .report-card :deep(.ant-card-body) { padding: 16px; }
  .alert-card :deep(.ant-card-body) { min-height: 190px; }
  .applied-filters { align-items: flex-start; }
}

@media (max-width: 479px) {
  .filter-actions { flex-direction: column; }
  .filter-actions .ant-btn { width: 100%; }
  .summary-copy strong { font-size: 23px; }
}
</style>
