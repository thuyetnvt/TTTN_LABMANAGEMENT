<template>
  <div class="overview-container">
    <template v-if="isManager">
      <header class="manager-header">
        <div>
          <h2>Tổng quan quản trị</h2>
          <p class="subtitle">Các công việc và cảnh báo cần xử lý trong Phòng Lab IoT.</p>
        </div>
        <div class="manager-header-actions">
          <a-button :loading="refreshing" @click="refreshStats">
            <template #icon><ReloadOutlined /></template>
            Làm mới
          </a-button>
          <span class="updated-at">Cập nhật: {{ formattedUpdatedAt }}</span>
        </div>
      </header>

      <section class="manager-section manager-kpi-section" aria-label="Chỉ số tổng quan">
        <div class="manager-kpi-grid">
          <div v-for="item in managerKpis" :key="item.key" class="manager-kpi-card">
            <span class="manager-kpi-icon" :class="`tone-${item.tone}`" aria-hidden="true">
              <component :is="item.icon" />
            </span>
            <div class="manager-kpi-copy">
              <span class="manager-kpi-label">{{ item.label }}</span>
              <strong>{{ item.value }}</strong>
              <small>{{ item.description }}</small>
            </div>
          </div>
        </div>
      </section>

      <section class="manager-two-column manager-section manager-primary-grid">
        <a-card :bordered="false" class="manager-panel manager-chart-panel">
          <template #title>Trạng thái thiết bị</template>
          <div v-if="hasManagerStatusData" class="manager-donut-layout">
            <apexchart type="donut" height="190" :options="managerDonutOptions" :series="managerDonutSeries" />
            <div class="manager-status-legend">
              <div v-for="item in managerStatusRows" :key="item.key" class="manager-status-legend-item">
                <span class="status-dot" :class="`status-dot--${item.tone}`" aria-hidden="true" />
                <span>{{ item.label }}</span>
                <strong>{{ item.value }}</strong>
              </div>
            </div>
          </div>
          <a-empty v-else description="Chưa có dữ liệu trạng thái thiết bị" />
        </a-card>

        <a-card :bordered="false" class="manager-panel manager-attention-panel">
          <template #title>Cần xử lý ngay</template>
          <div class="manager-attention-list">
            <div v-for="item in managerAttentionItems" :key="item.key" class="manager-attention-item">
              <span class="manager-attention-icon" :class="`tone-${item.tone}`" aria-hidden="true">
                <component :is="item.icon" />
              </span>
              <span class="manager-attention-copy">
                <strong>{{ item.label }}</strong>
                <small>{{ item.description }}</small>
              </span>
              <strong class="manager-attention-value">{{ item.value }}</strong>
              <a-button type="link" class="manager-attention-action" @click="navigateTo(item.route)">Xem</a-button>
            </div>
          </div>
        </a-card>
      </section>

      <section class="manager-two-column manager-section manager-secondary-grid">
        <a-card :bordered="false" class="manager-panel manager-chart-panel">
          <template #title>Lượt mượn 6 tháng gần đây</template>
          <apexchart
            v-if="hasBorrowTrendData"
            type="bar"
            height="190"
            :options="borrowTrendOptions"
            :series="borrowTrendSeries"
          />
          <a-empty v-else description="Chưa có dữ liệu lượt mượn" />
        </a-card>

        <a-card :bordered="false" class="manager-panel manager-activity-panel">
          <template #title>
            <div class="panel-title-row">
              <span>Hoạt động gần đây</span>
              <a-button v-if="canViewAuditLogs" type="link" @click="navigateTo({ name: 'AuditLogs' })">
                Xem toàn bộ nhật ký
              </a-button>
            </div>
          </template>
          <div v-if="recentActivities.length" class="activity-list">
            <div v-for="(activity, index) in recentActivities" :key="`${activity.date}-${index}`" class="activity-item">
              <span class="activity-icon" :class="`activity-icon--${activity.color || 'default'}`" aria-hidden="true">
                <component :is="getActivityIcon(activity.action)" />
              </span>
              <div class="activity-copy">
                <p>{{ activity.message }}</p>
                <span class="activity-meta">{{ activity.performer || 'Hệ thống' }} · {{ formatDateTime(activity.date) }}</span>
              </div>
            </div>
          </div>
          <a-empty v-else description="Chưa có hoạt động nào" />
        </a-card>
      </section>

      <section class="manager-section manager-quick-actions" aria-labelledby="quick-actions-heading">
        <h3 id="quick-actions-heading" class="manager-section-title">Thao tác nhanh</h3>
        <div class="quick-actions-grid">
          <a-button v-for="item in managerQuickActions" :key="item.key" class="quick-action-button" @click="navigateTo(item.route)">
            <template #icon><component :is="item.icon" /></template>
            <span>{{ item.label }}</span>
            <ArrowRightOutlined class="quick-action-arrow" />
          </a-button>
        </div>
      </section>
    </template>

    <template v-else>
      <div class="header">
        <h2 class="serif-title">Tổng quan hệ thống</h2>
        <p class="subtitle">Theo dõi nhanh thiết bị, yêu cầu và cảnh báo cần xử lý.</p>
      </div>

      <a-row :gutter="[12, 12]" class="stat-grid">
        <a-col v-for="item in studentStats" :key="item.label" :xs="24" :sm="12">
          <a-card class="stat-card" :class="item.tone">
            <div class="stat-icon"><component :is="item.icon" /></div>
            <div class="stat-info">
              <span class="label">{{ item.label }}</span>
              <span class="value">{{ item.value }}</span>
            </div>
          </a-card>
        </a-col>
      </a-row>

      <div class="borrower-content-grid">
        <a-card title="Hoạt động gần đây" :bordered="false" class="borrower-panel">
          <a-timeline>
            <a-timeline-item v-for="(activity, index) in stats.activities" :key="index" :color="activity.color">
              <p class="timeline-date">{{ formatDateTime(activity.date) }}</p>
              <p class="timeline-content">{{ activity.message }}</p>
            </a-timeline-item>
          </a-timeline>
          <a-empty v-if="!stats.activities.length" description="Chưa có hoạt động nào" />
        </a-card>

        <a-card title="Cảnh báo cần xử lý" :bordered="false" class="borrower-panel">
          <div
            v-for="(alert, index) in stats.alerts"
            :key="index"
            class="borrower-alert is-clickable"
            :class="alert.level"
            role="button"
            tabindex="0"
            :aria-label="`${alert.title}: ${alert.message}`"
            @click="handleAlertClick(alert)"
            @keydown.enter.prevent="handleAlertClick(alert)"
            @keydown.space.prevent="handleAlertClick(alert)"
          >
            <component :is="getAlertIcon(alert.level)" />
            <span><strong>{{ alert.title }}</strong><small>{{ alert.message }}</small></span>
            <ArrowRightOutlined />
          </div>
          <a-empty v-if="!stats.alerts.length" description="Không có cảnh báo" />
        </a-card>

        <a-card title="Trạng thái thiết bị" :bordered="false" class="borrower-panel borrower-status-panel">
          <apexchart type="donut" height="220" :options="pieOptions" :series="pieSeries" />
        </a-card>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { message } from 'ant-design-vue'
import { useRouter } from 'vue-router'
import {
  AppstoreOutlined,
  ArrowRightOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  FileSearchOutlined,
  PlusOutlined,
  ReloadOutlined,
  TeamOutlined,
  ToolOutlined,
  WarningOutlined
} from '@ant-design/icons-vue'
import VueApexCharts from 'vue3-apexcharts'
import { dashboardApi } from '../api/dashboardApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isManagerRole } from '../constants/business'
import { getDashboardAlertTarget } from '../utils/dashboardAlerts'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDateTime } from '../utils/dateTime.js'

const router = useRouter()
const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))
const canViewAuditLogs = computed(() => isAdminRole(role.value))
const apexchart = VueApexCharts
const refreshing = ref(false)

const stats = ref({
  updatedAt: null,
  pendingBorrowRequests: 0,
  overdueBorrowRecords: 0,
  lowStockConsumables: 0,
  warrantyExpiringSoon: 0,
  maintenanceInProgress: 0,
  counts: { total: 0, available: 0, maintenance: 0, borrowed: 0, broken: 0, warranty: 0 },
  activities: [],
  alerts: [],
  advanced: { pendingRequests: 0, lowStockConsumables: [], borrowTrends: [] }
})

const formatNumber = value => Number(value || 0).toLocaleString('vi-VN')
const formatDateTime = value => formatVietnamDateTime(value)
const formatUpdatedAt = value => formatVietnamDateTime(value, 'Chưa cập nhật')

const managerKpis = computed(() => [
  {
    key: 'total-equipment',
    label: 'Tổng thiết bị',
    value: formatNumber(stats.value.counts.total),
    description: 'Tài sản trong hệ thống',
    icon: AppstoreOutlined,
    tone: 'primary'
  },
  {
    key: 'borrowed-equipment',
    label: 'Đang mượn',
    value: formatNumber(stats.value.counts.borrowed),
    description: 'Tài sản đang được sử dụng',
    icon: ClockCircleOutlined,
    tone: 'success'
  },
  {
    key: 'pending-work',
    label: 'Chờ xử lý',
    value: formatNumber(stats.value.advanced?.pendingRequests ?? stats.value.pendingBorrowRequests),
    description: 'Phiếu cần kiểm tra',
    icon: FileSearchOutlined,
    tone: 'warning'
  },
  {
    key: 'broken-warranty',
    label: 'Hỏng/Bảo hành',
    value: formatNumber(Number(stats.value.counts.broken || 0) + Number(stats.value.counts.warranty || 0)),
    description: 'Tài sản cần theo dõi',
    icon: ToolOutlined,
    tone: 'danger'
  }
])

const managerStatusRows = computed(() => [
  { key: 'available', label: 'Rảnh', value: Number(stats.value.counts.available || 0), tone: 'success' },
  { key: 'borrowed', label: 'Đang mượn', value: Number(stats.value.counts.borrowed || 0), tone: 'info' },
  { key: 'warranty', label: 'Bảo hành', value: Number(stats.value.counts.warranty || 0), tone: 'warning' },
  { key: 'broken', label: 'Hỏng', value: Number(stats.value.counts.broken || 0), tone: 'danger' }
])

const hasManagerStatusData = computed(() => managerStatusRows.value.some(item => item.value > 0))
const managerDonutSeries = computed(() => managerStatusRows.value.map(item => item.value))
const managerDonutOptions = computed(() => ({
  chart: { type: 'donut', toolbar: { show: false }, fontFamily: 'inherit' },
  labels: managerStatusRows.value.map(item => item.label),
  colors: ['#7FBD68', '#4D91D8', '#F2B24B', '#E35F4E'],
  stroke: { width: 3, colors: ['#fff'] },
  legend: { show: false },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: value => formatNumber(value) } },
  plotOptions: {
    pie: {
      donut: {
        size: '68%',
        labels: {
          show: true,
          name: { show: true, color: '#64748b', fontSize: '12px', offsetY: 18 },
          value: { show: true, color: '#10233F', fontSize: '23px', fontWeight: 700, offsetY: -12, formatter: value => formatNumber(value) },
          total: { show: true, showAlways: true, label: 'Thiết bị', color: '#64748b', formatter: chartContext => formatNumber(chartContext.globals.seriesTotals.reduce((sum, value) => sum + value, 0)) }
        }
      }
    }
  }
}))

const recentActivities = computed(() => stats.value.activities.slice(0, 5))
const formattedUpdatedAt = computed(() => formatUpdatedAt(stats.value.updatedAt))

const getActivityIcon = action => ({
  Create: PlusOutlined,
  Update: EditOutlined,
  Delete: DeleteOutlined,
  Approve: CheckCircleOutlined,
  TeacherApprove: CheckCircleOutlined,
  Return: ClockCircleOutlined,
  SendReturnReminder: ClockCircleOutlined,
  LoginSucceeded: CheckCircleOutlined,
  LoginFailed: WarningOutlined
}[action] || FileSearchOutlined)

const managerAttentionItems = computed(() => [
  {
    key: 'pending-borrow-requests', label: 'Phiếu chờ duyệt',
    value: formatNumber(stats.value.pendingBorrowRequests), description: 'Phiếu mượn cần kiểm tra',
    icon: FileSearchOutlined, tone: 'warning', route: { name: 'BorrowRequests' }
  },
  {
    key: 'overdue-borrow-records', label: 'Mượn quá hạn',
    value: formatNumber(stats.value.overdueBorrowRecords), description: 'Phiếu cần nhắc trả',
    icon: ClockCircleOutlined, tone: 'danger', route: { name: 'BorrowHistory' }
  },
  {
    key: 'low-stock-consumables', label: 'Vật tư sắp hết',
    value: formatNumber(stats.value.lowStockConsumables), description: 'Mặt hàng dưới mức tối thiểu',
    icon: AppstoreOutlined, tone: 'warning', route: { name: 'ConsumableRequests' }
  },
  {
    key: 'warranty-expiring-soon', label: 'Bảo hành sắp hết',
    value: formatNumber(stats.value.warrantyExpiringSoon), description: 'Thiết bị trong 30 ngày tới',
    icon: ToolOutlined, tone: 'info', route: { name: 'Devices', query: { status: 'warranty' } }
  }
])

const borrowTrendItems = computed(() => stats.value.advanced?.borrowTrends || [])
const hasBorrowTrendData = computed(() => borrowTrendItems.value.length > 0 && borrowTrendItems.value.some(item => Number(item.count ?? item.Count ?? 0) > 0))
const borrowTrendSeries = computed(() => [{
  name: 'Lượt mượn',
  data: borrowTrendItems.value.map(item => Number(item.count ?? item.Count ?? 0))
}])
const borrowTrendOptions = computed(() => ({
  chart: { type: 'bar', toolbar: { show: false }, fontFamily: 'inherit' },
  colors: ['#DF7657'],
  plotOptions: { bar: { borderRadius: 4, columnWidth: '42%' } },
  dataLabels: { enabled: false },
  grid: { borderColor: '#edf0f2', strokeDashArray: 3 },
  xaxis: {
    categories: borrowTrendItems.value.map(item => item.month ?? item.Month ?? ''),
    labels: { style: { colors: '#64748b', fontSize: '11px' } },
    axisBorder: { color: '#d9dee3' },
    axisTicks: { color: '#d9dee3' }
  },
  yaxis: { min: 0, forceNiceScale: true, labels: { style: { colors: '#64748b', fontSize: '11px' }, formatter: value => Math.round(value) } },
  tooltip: { y: { formatter: value => `${formatNumber(value)} lượt` } }
}))

const managerQuickActions = computed(() => [
  ...(canViewAuditLogs.value ? [{ key: 'users', label: 'Quản lý người dùng', icon: TeamOutlined, route: { name: 'AdminUsers' } }] : []),
  { key: 'borrow-requests', label: 'Duyệt phiếu', icon: FileSearchOutlined, route: { name: 'BorrowRequests' } },
  { key: 'add-equipment', label: 'Thêm tài sản', icon: PlusOutlined, route: { name: 'Devices' } },
  { key: 'reports', label: 'Xem báo cáo', icon: AppstoreOutlined, route: { name: 'Reports' } }
])

const studentStats = computed(() => [
  { label: 'Đang mượn', value: stats.value.counts.borrowed, icon: ClockCircleOutlined, tone: 'amber' },
  { label: 'Thiết bị rảnh', value: stats.value.counts.available, icon: CheckCircleOutlined, tone: 'green' }
])

const pieOptions = ref({
  chart: { type: 'donut' },
  labels: ['Rảnh', 'Đang mượn', 'Bảo hành', 'Hỏng'],
  colors: ['#52c41a', '#1890ff', '#faad14', '#f5222d'],
  legend: { position: 'bottom' },
  dataLabels: { enabled: true }
})
const pieSeries = ref([0, 0, 0, 0])

const navigateTo = route => router.push(route)

const handleAlertClick = alert => {
  const target = getDashboardAlertTarget(alert?.type)
  if (target) navigateTo(target)
}

const getAlertIcon = level => level === 'info' ? ClockCircleOutlined : WarningOutlined

const refreshStats = async () => {
  refreshing.value = true
  try {
    const result = await dashboardApi.getStats()
    stats.value = result
    pieSeries.value = [
      result.counts?.available || 0,
      result.counts?.borrowed || 0,
      result.counts?.warranty || 0,
      result.counts?.broken || 0
    ]
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được dữ liệu tổng quan.'))
  } finally {
    refreshing.value = false
  }
}

onMounted(refreshStats)
</script>

<style scoped>
.overview-container { padding: 0; }
.manager-header, .panel-title-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.manager-header { margin-bottom: 18px; }
.manager-header h2, .header h2 { margin: 0; color: #10233f; font-family: var(--font-serif); font-size: 30px; font-weight: 500; line-height: 1.15; letter-spacing: -0.02em; }
.subtitle { margin: 6px 0 0; color: #64748b; font-size: 14px; }
.manager-header-actions { display: flex; flex-direction: column; align-items: flex-end; gap: 6px; }
.manager-header-actions :deep(.ant-btn) { border-color: #dfe4e8; color: #10233f; }
.manager-header-actions :deep(.ant-btn:hover), .manager-header-actions :deep(.ant-btn:focus) { border-color: #df7657; color: #df7657; }
.updated-at { color: #64748b; font-size: 11px; white-space: nowrap; }
.manager-section { margin-top: 16px; }
.manager-kpi-grid, .quick-actions-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.manager-kpi-card { display: flex; align-items: center; min-width: 0; gap: 12px; padding: 14px 16px; border: 1px solid #e7eaed; border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(16, 35, 63, .04); }
.manager-kpi-icon, .manager-attention-icon { display: inline-flex; align-items: center; justify-content: center; flex: 0 0 auto; border-radius: 50%; }
.manager-kpi-icon { width: 40px; height: 40px; font-size: 19px; }
.manager-kpi-copy { display: flex; min-width: 0; flex-direction: column; gap: 2px; }
.manager-kpi-label { overflow: hidden; color: #526276; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.manager-kpi-copy strong { color: #10233f; font-size: 25px; line-height: 1.05; }
.manager-kpi-copy small { overflow: hidden; color: #94a3b8; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.tone-primary { color: #2376c5; background: #eaf4ff; }
.tone-success { color: #4d9b3b; background: #edf8e9; }
.tone-warning { color: #d98b18; background: #fff6e5; }
.tone-danger { color: #d84c43; background: #fff0ee; }
.tone-info { color: #4d91d8; background: #edf5ff; }
.manager-two-column { display: grid; gap: 16px; }
.manager-primary-grid { grid-template-columns: minmax(0, 1.05fr) minmax(0, 1.45fr); }
.manager-secondary-grid { grid-template-columns: minmax(0, 1fr) minmax(0, 1.35fr); }
.manager-panel { min-width: 0; border: 1px solid #e7eaed; border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(16, 35, 63, .04); }
.manager-panel :deep(.ant-card-head) { min-height: 47px; padding: 0 16px; border-bottom: 1px solid #eef1f3; }
.manager-panel :deep(.ant-card-head-title) { padding: 13px 0; color: #10233f; font-size: 15px; font-weight: 650; }
.manager-panel :deep(.ant-card-body) { padding: 13px 16px; }
.manager-chart-panel :deep(.ant-card-body) { min-height: 190px; }
.manager-donut-layout { display: grid; grid-template-columns: minmax(170px, .95fr) minmax(145px, 1fr); align-items: center; gap: 4px; }
.manager-status-legend { display: flex; flex-direction: column; gap: 11px; padding-right: 8px; }
.manager-status-legend-item { display: grid; grid-template-columns: 8px 1fr auto; align-items: center; gap: 8px; color: #526276; font-size: 12px; }
.manager-status-legend-item strong { color: #10233f; font-size: 13px; }
.status-dot { width: 8px; height: 8px; flex: 0 0 8px; border-radius: 50%; }
.status-dot--success { background: #7fbd68; }
.status-dot--info { background: #4d91d8; }
.status-dot--warning { background: #f2b24b; }
.status-dot--danger { background: #e35f4e; }
.manager-attention-list { display: flex; flex-direction: column; gap: 7px; }
.manager-attention-item { display: grid; grid-template-columns: 30px minmax(0, 1fr) auto auto; align-items: center; gap: 9px; min-height: 39px; padding: 5px 7px; border: 1px solid #eef1f3; border-radius: 7px; }
.manager-attention-icon { width: 28px; height: 28px; border-radius: 8px; font-size: 14px; }
.manager-attention-copy { display: flex; min-width: 0; flex-direction: column; gap: 2px; }
.manager-attention-copy strong { overflow: hidden; color: #10233f; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.manager-attention-copy small { overflow: hidden; color: #94a3b8; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.manager-attention-value { color: #10233f; font-size: 15px; }
.manager-attention-action { padding-inline: 7px; color: #2d82c8; font-size: 12px; }
.panel-title-row { align-items: center; }
.panel-title-row .ant-btn { padding-inline: 0; color: #df7657; font-size: 12px; }
.activity-list { display: flex; flex-direction: column; gap: 10px; }
.activity-item { display: flex; align-items: flex-start; gap: 9px; min-width: 0; }
.activity-icon { display: inline-flex; align-items: center; justify-content: center; width: 27px; height: 27px; margin-top: 1px; flex: 0 0 27px; border-radius: 8px; background: #f8fafc; }
.activity-icon--green { color: #4d9b3b; background: #edf8e9; }
.activity-icon--blue, .activity-icon--info { color: #4d91d8; background: #edf5ff; }
.activity-icon--orange, .activity-icon--warning { color: #d98b18; background: #fff6e5; }
.activity-icon--red, .activity-icon--error { color: #d84c43; background: #fff0ee; }
.activity-icon--purple { color: #7652b6; background: #f3effc; }
.activity-copy { min-width: 0; }
.activity-copy p { display: -webkit-box; margin: 0; overflow: hidden; color: #334155; font-size: 12px; line-height: 1.35; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.activity-meta { display: block; margin-top: 3px; overflow: hidden; color: #94a3b8; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.manager-section-title { margin: 0 0 9px; color: #10233f; font-size: 15px; font-weight: 650; }
.quick-action-button { display: flex; align-items: center; justify-content: flex-start; width: 100%; height: 43px; padding-inline: 13px; border-color: #e7eaed; border-radius: 9px; color: #10233f; text-align: left; }
.quick-action-button:hover, .quick-action-button:focus { border-color: #df7657; color: #df7657; }
.quick-action-button > span:not(.ant-btn-icon) { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.quick-action-arrow { margin-left: auto; color: #94a3b8; font-size: 11px; }
.header { display: flex; flex-direction: column; gap: 4px; margin-bottom: 16px; }
.serif-title { font-family: var(--font-serif); }
.stat-grid { margin-bottom: 16px; }
.stat-card, .borrower-panel { border: 1px solid rgba(0, 0, 0, .05); border-radius: 12px; background: #fff; box-shadow: none; }
.stat-card :deep(.ant-card-body) { display: flex; align-items: center; gap: 14px; padding: 20px 22px; }
.stat-icon { display: flex; align-items: center; justify-content: center; width: 54px; height: 54px; flex: 0 0 54px; border-radius: 12px; font-size: 24px; }
.stat-info { display: flex; flex-direction: column; min-width: 0; }
.stat-info .label { color: #334155; font-size: 14px; font-weight: 600; }
.stat-info .value { margin-top: 6px; color: #0f172a; font-size: 28px; font-weight: 700; line-height: 1.05; }
.stat-card.green .stat-icon { color: #059669; background: #ecfdf5; }
.stat-card.amber .stat-icon { color: #d97706; background: #fffbeb; }
.borrower-content-grid { display: grid; grid-template-columns: minmax(0, 2fr) minmax(280px, 1fr); gap: 16px; align-items: start; }
.borrower-status-panel { grid-column: 1 / -1; }
.borrower-panel :deep(.ant-card-head) { padding: 0 20px; border-bottom: 1px solid rgba(0, 0, 0, .05); }
.borrower-panel :deep(.ant-card-head-title) { padding: 15px 0; color: #0f172a; font-size: 18px; font-weight: 600; }
.borrower-panel :deep(.ant-card-body) { padding: 16px 20px; }
.timeline-date { margin-bottom: 4px; color: #9ca3af; font-size: 12px; }
.timeline-content { color: #374151; font-weight: 500; }
.borrower-alert { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; padding: 12px; border-left: 3px solid #94a3b8; border-radius: 8px; background: #f8fafc; }
.borrower-alert.is-clickable { cursor: pointer; }
.borrower-alert span { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 3px; }
.borrower-alert small { color: #475569; line-height: 1.35; }
.borrower-alert.warning { border-left-color: #d97706; background: #fffbeb; }
.borrower-alert.info { border-left-color: #2563eb; background: #eff6ff; }
.borrower-alert.error { border-left-color: #dc2626; background: #fef2f2; }
@media (max-width: 1199px) {
  .manager-kpi-grid, .quick-actions-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .manager-two-column { grid-template-columns: 1fr; }
}
@media (max-width: 767px) {
  .manager-header { align-items: flex-start; flex-direction: column; }
  .manager-header-actions { align-items: flex-start; }
  .manager-kpi-grid, .quick-actions-grid, .borrower-content-grid { grid-template-columns: 1fr; }
  .manager-donut-layout { grid-template-columns: 1fr; }
  .manager-status-legend { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); padding: 0 10px 7px; }
  .manager-attention-item { grid-template-columns: 30px minmax(0, 1fr) auto auto; }
  .borrower-status-panel { grid-column: auto; }
  .manager-header h2, .header h2 { font-size: 27px; }
}
</style>
