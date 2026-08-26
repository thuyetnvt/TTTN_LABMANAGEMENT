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
          <a-input id="reports-from" v-model:value="filters.from" type="date" />
        </div>
        <div class="filter-field">
          <label for="reports-to">Đến ngày</label>
          <a-input id="reports-to" v-model:value="filters.to" type="date" />
        </div>
        <div class="filter-actions">
          <a-button :disabled="loading" @click="resetFilters">Đặt lại</a-button>
        </div>
      </div>
    </a-card>

    <div class="summary-grid">
      <a-card v-for="item in summaryCards" :key="item.label" :bordered="false" class="summary-card">
        <span>{{ item.label }}</span>
        <strong>{{ item.value }}</strong>
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
const summaryCards = computed(() => [
  { label: 'Tổng tài sản', value: report.value.totals.assets || 0 },
  { label: 'Đang mượn', value: report.value.totals.borrowed || 0 },
  { label: 'Quá hạn', value: report.value.totals.overdue || 0 },
  { label: 'Hỏng', value: report.value.totals.broken || 0 },
  { label: 'Bảo hành', value: report.value.totals.underWarranty || 0 },
  { label: 'Chi phí bảo trì', value: `${Number(report.value.totals.maintenanceCost || 0).toLocaleString('vi-VN')} ₫` }
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
.filter-fields { display: grid; grid-template-columns: minmax(180px, 1fr) minmax(180px, 1fr) auto; align-items: end; gap: 16px; }
.filter-field label { display: block; margin-bottom: 7px; color: var(--color-ink); font-size: 14px; font-weight: 600; }
.filter-actions { display: flex; gap: 10px; }
.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 20px; }
.summary-card { padding: 4px; }
.summary-card span, .summary-card strong { display: block; }
.summary-card span { color: var(--color-secondary); font-size: 15px; line-height: 1.4; }
.summary-card strong { margin-top: 10px; color: var(--color-ink); font-size: 28px; line-height: 1.2; font-weight: 700; }
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
