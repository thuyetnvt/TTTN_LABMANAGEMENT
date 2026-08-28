<template>
  <div class="reports-page">
    <PageHeader title="Báo cáo tài sản" subtitle="Tổng hợp tài sản, mượn trả, bảo trì và vật tư từ dữ liệu thật.">
      <template #actions>
        <a-button :loading="exportingPdf" @click="exportPdf">Xuất PDF</a-button>
        <a-button type="primary" :loading="exporting" @click="exportReport">Xuất Excel</a-button>
      </template>
    </PageHeader>

    <a-card :bordered="false" class="filter-card">
      <div class="filter-fields">
        <div class="filter-field">
          <label for="reports-from">Từ ngày</label>
          <a-date-picker id="reports-from" v-model:value="filters.from" valueFormat="YYYY-MM-DD" format="DD/MM/YYYY" placeholder="Chọn ngày bắt đầu" style="width: 100%" />
        </div>
        <div class="filter-field">
          <label for="reports-to">Đến ngày</label>
          <a-date-picker id="reports-to" v-model:value="filters.to" valueFormat="YYYY-MM-DD" format="DD/MM/YYYY" placeholder="Chọn ngày kết thúc" style="width: 100%" />
        </div>
        <div class="filter-actions">
          <a-button :disabled="loading" @click="resetFilters">
            <template #icon><ReloadOutlined /></template>
            Đặt lại
          </a-button>
        </div>
      </div>
    </a-card>

    <div class="summary-grid">
      <a-card v-for="item in summaryCards" :key="item.label" :bordered="false" class="stat-card" :class="`tone-${item.tone}`">
        <div class="stat-icon"><component :is="item.icon" /></div>
        <div class="stat-info">
          <span class="label">{{ item.label }}</span>
          <span class="value" :title="item.fullValue || item.value">{{ item.value }}</span>
        </div>
      </a-card>
    </div>

    <div class="reports-grid reports-grid--charts">
      <a-card title="Theo trạng thái" :bordered="false" class="report-panel">
        <a-table class="report-table" :data-source="report.byStatus" :columns="statusColumns" row-key="status" size="small" :pagination="false">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'"><StatusBadge :status="record.status" /></template>
          </template>
        </a-table>
      </a-card>
      <a-card title="Theo danh mục" :bordered="false" class="report-panel">
        <div class="category-table-scroll">
          <a-table class="report-table" :data-source="report.byCategory" :columns="categoryColumns" row-key="category" size="small" :pagination="false" />
        </div>
      </a-card>
    </div>

    <div class="reports-grid reports-grid--details report-row">
      <a-card title="Tài sản đang mượn/quá hạn" :bordered="false" class="report-panel">
        <a-table class="report-table" :data-source="report.borrowed" :columns="borrowColumns" row-key="id" size="small" :pagination="{ pageSize: 8 }">
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'expectedReturnDate'">{{ formatDate(record.expectedReturnDate) }}</template>
            <template v-if="column.key === 'overdue'"><a-tag :color="record.overdue ? 'red' : 'green'">{{ record.overdue ? 'Quá hạn' : 'Trong hạn' }}</a-tag></template>
          </template>
        </a-table>
      </a-card>
      <a-card title="Vật tư sắp hết" :bordered="false" class="report-panel">
        <a-table class="report-table" :data-source="report.lowStock" :columns="stockColumns" row-key="id" size="small" :pagination="false" />
      </a-card>
    </div>

    <a-card title="Thiết bị sắp hết bảo hành trong 30 ngày" :bordered="false" class="report-panel report-row report-panel--full">
      <a-table class="report-table" :data-source="report.warrantySoon" :columns="warrantyColumns" row-key="id" size="small" :pagination="false">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'warrantyExpiry'">{{ formatDate(record.warrantyExpiry) }}</template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { message } from 'ant-design-vue'
import { AppstoreOutlined, ShoppingCartOutlined, WarningOutlined, ToolOutlined, SafetyCertificateOutlined, DollarOutlined, ReloadOutlined } from '@ant-design/icons-vue'
import StatusBadge from '../components/StatusBadge.vue'
import PageHeader from '../components/PageHeader.vue'
import { reportsApi } from '../api/reportsApi'

const filters = ref({ from: '', to: '' })
const loading = ref(false)
const exporting = ref(false)
const exportingPdf = ref(false)
const report = ref({ totals: {}, byStatus: [], byCategory: [], borrowed: [], lowStock: [], warrantySoon: [] })
const statusColumns = [{ title: 'Trạng thái', key: 'status' }, { title: 'Số lượng', dataIndex: 'count', key: 'count' }]
const categoryColumns = [{ title: 'Danh mục', dataIndex: 'category', key: 'category' }, { title: 'Số lượng', dataIndex: 'count', key: 'count' }]
const borrowColumns = [{ title: 'Người mượn', dataIndex: 'user', key: 'user' }, { title: 'Thiết bị', dataIndex: 'equipment', key: 'equipment' }, { title: 'Hạn trả', key: 'expectedReturnDate' }, { title: 'Kết quả', key: 'overdue' }]
const stockColumns = [{ title: 'Vật tư', dataIndex: 'name', key: 'name' }, { title: 'Tồn', dataIndex: 'quantity', key: 'quantity' }, { title: 'Tối thiểu', dataIndex: 'minQuantity', key: 'minQuantity' }]
const warrantyColumns = [{ title: 'Thiết bị', dataIndex: 'name', key: 'name' }, { title: 'Số seri', dataIndex: 'serial', key: 'serial' }, { title: 'Hạn bảo hành', key: 'warrantyExpiry' }]
const formatCompactCurrency = (value) => {
  if (!value) return '0 ₫'
  const num = Number(value)
  if (num >= 1000000000) return (num / 1000000000).toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + ' Tỷ ₫'
  if (num >= 1000000) return (num / 1000000).toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + ' Tr ₫'
  if (num >= 1000) return (num / 1000).toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + ' N ₫'
  return num.toLocaleString('vi-VN') + ' ₫'
}

const summaryCards = computed(() => [
  { label: 'Tổng tài sản', value: report.value.totals.assets || 0, tone: 'primary', icon: AppstoreOutlined },
  { label: 'Đang mượn', value: report.value.totals.borrowed || 0, tone: 'success', icon: ShoppingCartOutlined },
  { label: 'Quá hạn', value: report.value.totals.overdue || 0, tone: 'warning', icon: WarningOutlined },
  { label: 'Hỏng', value: report.value.totals.broken || 0, tone: 'danger', icon: ToolOutlined },
  { label: 'Bảo hành', value: report.value.totals.underWarranty || 0, tone: 'info', icon: SafetyCertificateOutlined },
  { label: 'Chi phí bảo trì', value: formatCompactCurrency(report.value.totals.maintenanceCost), fullValue: `${Number(report.value.totals.maintenanceCost || 0).toLocaleString('vi-VN')} ₫`, tone: 'warning', icon: DollarOutlined }
])

const formatDate = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—'
const load = async () => {
  loading.value = true
  try { report.value = await reportsApi.summary(filters.value) } catch (error) { message.error(error?.response?.data?.message || 'Không tải được báo cáo.') } finally { loading.value = false }
}
const resetFilters = () => {
  filters.value = { from: '', to: '' }
}
const exportReport = async () => {
  exporting.value = true
  try {
    const blob = await reportsApi.export(filters.value)
    const url = URL.createObjectURL(new Blob([blob]))
    const link = document.createElement('a'); link.href = url; link.download = `BaoCaoTaiSan_${Date.now()}.xlsx`; link.click(); URL.revokeObjectURL(url)
    message.success('Đã xuất báo cáo Excel.')
  } catch (error) { message.error(error?.response?.data?.message || 'Không thể xuất báo cáo.') } finally { exporting.value = false }
}
const exportPdf = async () => {
  exportingPdf.value = true
  try {
    const blob = await reportsApi.exportPdf(filters.value)
    const url = URL.createObjectURL(new Blob([blob]))
    const link = document.createElement('a'); link.href = url; link.download = `BaoCaoTaiSan_${Date.now()}.pdf`; link.click(); URL.revokeObjectURL(url)
    message.success('Đã xuất báo cáo PDF.')
  } catch (error) { message.error(error?.response?.data?.message || 'Không thể xuất báo cáo PDF.') } finally { exportingPdf.value = false }
}

watch(filters, load, { deep: true })
onMounted(load)
</script>

<style scoped>
.reports-page { padding: 0; }
.filter-card { margin-bottom: 20px; }
.filter-fields { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 16px; }
.filter-field { width: 240px; flex: 0 0 auto; }
.filter-field label { display: block; margin-bottom: 7px; color: var(--color-ink); font-size: 14px; font-weight: 600; }
.filter-actions { display: flex; gap: 10px; margin-left: auto; }
.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }
.stat-card { border-radius: 12px; transition: all 0.2s ease; box-shadow: 0 1px 2px rgba(0,0,0,0.03); }
.stat-card:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.05); }
.stat-card :deep(.ant-card-body) { padding: 20px; display: flex; align-items: center; gap: 16px; }
.stat-icon { display: flex; align-items: center; justify-content: center; width: 48px; height: 48px; border-radius: 12px; font-size: 24px; flex-shrink: 0; }
.stat-info { display: flex; flex-direction: column; min-width: 0; }
.stat-info .label { color: var(--color-text-secondary, #6b7280); font-size: 13px; font-weight: 500; margin-bottom: 4px; }
.stat-info .value { color: var(--color-ink, #111827); font-size: 20px; font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.tone-primary .stat-icon { color: #2376c5; background: #eaf4ff; }
.tone-success .stat-icon { color: #4d9b3b; background: #edf8e9; }
.tone-warning .stat-icon { color: #d98b18; background: #fff6e5; }
.tone-danger .stat-icon { color: #d84c43; background: #fff0ee; }
.tone-info .stat-icon { color: #4d91d8; background: #edf5ff; }
.reports-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 20px; align-items: stretch; }
.report-panel { display: flex; flex-direction: column; min-width: 0; }
.report-panel--full { width: 100%; }
.report-panel :deep(.ant-card-body) { display: flex; flex: 1; flex-direction: column; min-width: 0; padding: 20px; }
.report-table { min-width: 0; }
.category-table-scroll { max-height: 360px; overflow-y: auto; }
.category-table-scroll :deep(.ant-table-thead > tr > th) { position: sticky; top: 0; z-index: 1; background: var(--color-surface); }
.report-row { margin-top: 20px; }
@media (max-width: 1199px) {
  .reports-grid { gap: 16px; }
  .report-panel :deep(.ant-card-body) { padding: 18px; }
}
@media (max-width: 767px) {
  .filter-fields { grid-template-columns: 1fr; gap: 12px; }
  .filter-actions { grid-column: auto; }
  .filter-actions .ant-btn { flex: 1; }
  .summary-grid { grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; }
  .reports-grid { grid-template-columns: 1fr; gap: 16px; }
  .report-row { margin-top: 16px; }
  .report-panel :deep(.ant-card-body) { padding: 16px; }
  .category-table-scroll { max-height: 300px; }
}
@media (max-width: 479px) {
  .filter-fields { grid-template-columns: 1fr; }
  .filter-actions { grid-column: auto; }
  .summary-grid { grid-template-columns: 1fr 1fr; }
}
</style>
