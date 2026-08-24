<template>
  <div class="reports-page">
    <div class="page-header">
      <div><h2>Báo cáo tài sản</h2><p>Tổng hợp tài sản, mượn trả, bảo trì và vật tư từ dữ liệu thật.</p></div>
      <a-space>
        <a-button :loading="exportingPdf" @click="exportPdf">Xuất PDF</a-button>
        <a-button type="primary" :loading="exporting" @click="exportReport">Xuất Excel</a-button>
      </a-space>
    </div>

    <a-card :bordered="false" class="filter-card">
      <a-row :gutter="12" align="middle">
        <a-col :xs="24" :sm="8"><label>Từ ngày</label><a-input v-model:value="filters.from" type="date" /></a-col>
        <a-col :xs="24" :sm="8"><label>Đến ngày</label><a-input v-model:value="filters.to" type="date" /></a-col>
        <a-col :xs="24" :sm="8"><a-button class="load-button" type="primary" :loading="loading" @click="load">Lọc báo cáo</a-button></a-col>
      </a-row>
    </a-card>

    <a-row :gutter="12" class="summary-grid">
      <a-col v-for="item in summaryCards" :key="item.label" :xs="12" :lg="4"><a-card :bordered="false" class="summary-card"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></a-card></a-col>
    </a-row>

    <a-row :gutter="16">
      <a-col :xs="24" :lg="12"><a-card title="Theo trạng thái" :bordered="false"><a-table :data-source="report.byStatus" :columns="statusColumns" row-key="status" size="small" :pagination="false"><template #bodyCell="{ column, record }"><template v-if="column.key === 'status'"><StatusBadge :status="record.status" /></template></template></a-table></a-card></a-col>
      <a-col :xs="24" :lg="12"><a-card title="Theo danh mục" :bordered="false"><a-table :data-source="report.byCategory" :columns="categoryColumns" row-key="category" size="small" :pagination="false" /></a-card></a-col>
    </a-row>

    <a-row :gutter="16" class="report-row">
      <a-col :xs="24" :lg="12"><a-card title="Tài sản đang mượn/quá hạn" :bordered="false"><a-table :data-source="report.borrowed" :columns="borrowColumns" row-key="id" size="small" :pagination="{ pageSize: 8 }"><template #bodyCell="{ column, record }"><template v-if="column.key === 'expectedReturnDate'">{{ formatDate(record.expectedReturnDate) }}</template><template v-if="column.key === 'overdue'"><a-tag :color="record.overdue ? 'red' : 'green'">{{ record.overdue ? 'Quá hạn' : 'Trong hạn' }}</a-tag></template></template></a-table></a-card></a-col>
      <a-col :xs="24" :lg="12"><a-card title="Vật tư sắp hết" :bordered="false"><a-table :data-source="report.lowStock" :columns="stockColumns" row-key="id" size="small" :pagination="false" /></a-card></a-col>
    </a-row>

    <a-card title="Thiết bị sắp hết bảo hành trong 30 ngày" :bordered="false" class="report-row"><a-table :data-source="report.warrantySoon" :columns="warrantyColumns" row-key="id" size="small" :pagination="false"><template #bodyCell="{ column, record }"><template v-if="column.key === 'warrantyExpiry'">{{ formatDate(record.warrantyExpiry) }}</template></template></a-table></a-card>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { message } from 'ant-design-vue'
import StatusBadge from '../components/StatusBadge.vue'
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
onMounted(load)
</script>

<style scoped>
.reports-page { padding: 0; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
h2 { margin: 0; }
.page-header p { margin: 5px 0 0; color: #64748b; }
.filter-card { margin-bottom: 16px; }
label { display: block; margin-bottom: 6px; color: #475569; font-size: 13px; }
.load-button { margin-top: 22px; }
.summary-grid { margin-bottom: 16px; }
.summary-card { min-height: 90px; }
.summary-card span, .summary-card strong { display: block; }
.summary-card span { color: #64748b; }
.summary-card strong { margin-top: 8px; color: #0f172a; font-size: 22px; }
.report-row { margin-top: 16px; }
@media (max-width: 575px) { .page-header { flex-direction: column; gap: 12px; } .load-button { width: 100%; } }
</style>
