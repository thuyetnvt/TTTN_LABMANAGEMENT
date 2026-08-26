<template>
  <div class="overview-container">
    <template v-if="isManager">
      <header class="manager-header">
        <div>
          <h2>Tổng quan vận hành</h2>
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

      <section class="manager-section" aria-labelledby="action-heading">
        <div class="section-heading">
          <div>
            <h3 id="action-heading">Cần xử lý</h3>
            <p>Những việc đang chờ quản trị viên kiểm tra.</p>
          </div>
        </div>
        <div class="action-grid">
          <div
            v-for="item in managerActionCards"
            :key="item.key"
            class="manager-action-card"
            role="button"
            tabindex="0"
            :aria-label="`${item.label}: ${item.value}`"
            @click="navigateTo(item.route)"
            @keydown.enter.prevent="navigateTo(item.route)"
            @keydown.space.prevent="navigateTo(item.route)"
          >
            <span class="manager-action-icon" :class="`tone-${item.tone}`">
              <component :is="item.icon" />
            </span>
            <span class="manager-action-copy">
              <strong>{{ item.label }}</strong>
              <span class="manager-action-value">{{ item.value }}</span>
              <small>{{ item.description }}</small>
            </span>
            <ArrowRightOutlined class="manager-action-arrow" />
          </div>
        </div>
      </section>

      <section class="manager-section" aria-labelledby="equipment-heading">
        <div class="section-heading">
          <div>
            <h3 id="equipment-heading">Tình hình thiết bị</h3>
            <p>{{ equipmentStatusSummary }}</p>
          </div>
          <a-button type="link" @click="navigateTo({ name: 'Devices' })">Xem danh sách thiết bị</a-button>
        </div>
        <a-card :bordered="false" class="equipment-status-card">
          <div class="equipment-status-list">
            <div v-for="item in equipmentStatusRows" :key="item.key" class="equipment-status-row">
              <div class="equipment-status-label">
                <span class="status-dot" :class="`status-dot--${item.tone}`" aria-hidden="true" />
                <span>{{ item.label }}</span>
              </div>
              <div class="equipment-status-progress" aria-hidden="true">
                <span :class="`status-progress--${item.tone}`" :style="{ width: `${item.percent}%` }" />
              </div>
              <strong>{{ item.value }}</strong>
            </div>
          </div>
        </a-card>
      </section>

      <section class="manager-two-column manager-section">
        <a-card :bordered="false" class="manager-panel">
          <template #title>
            <div class="panel-title-row">
              <span>Hoạt động gần đây</span>
              <a-button v-if="canViewAuditLogs" type="link" @click="navigateTo({ name: 'AuditLogs' })">Xem tất cả</a-button>
            </div>
          </template>
          <div v-if="recentActivities.length" class="activity-list">
            <div v-for="(activity, index) in recentActivities" :key="`${activity.date}-${index}`" class="activity-item">
              <span class="activity-dot" :class="`activity-dot--${activity.color || 'default'}`" aria-hidden="true" />
              <div class="activity-copy">
                <span class="activity-time">{{ formatDateTime(activity.date) }}</span>
                <p>{{ activity.message }}</p>
              </div>
            </div>
          </div>
          <a-empty v-else description="Chưa có hoạt động nào" />
        </a-card>

        <a-card :bordered="false" class="manager-panel">
          <template #title>Cảnh báo cần xử lý</template>
          <div v-if="managerAlerts.length" class="manager-alert-list">
            <div v-for="alert in managerAlerts" :key="alert.key" class="manager-alert" :class="`alert-${alert.tone}`">
              <span class="manager-alert-icon"><component :is="alert.icon" /></span>
              <div class="manager-alert-copy">
                <div class="manager-alert-heading">
                  <strong>{{ alert.title }}</strong>
                  <a-tag :color="alert.tone === 'danger' ? 'red' : 'orange'">{{ alert.severity }}</a-tag>
                </div>
                <p>{{ alert.message }}</p>
              </div>
              <a-tooltip title="Xử lý">
                <a-button type="text" class="alert-action" :aria-label="`Xử lý ${alert.title}`" @click="navigateTo(alert.route)">
                  <ArrowRightOutlined />
                </a-button>
              </a-tooltip>
            </div>
          </div>
          <a-empty v-else description="Không có cảnh báo cần xử lý" />
        </a-card>
      </section>

      <section class="manager-section" aria-labelledby="quick-actions-heading">
        <div class="section-heading">
          <div>
            <h3 id="quick-actions-heading">Thao tác nhanh</h3>
            <p>Đi tới các chức năng quản trị thường dùng.</p>
          </div>
        </div>
        <div class="quick-actions-grid">
          <a-button v-for="item in quickActions" :key="item.key" class="quick-action-button" @click="navigateTo(item.route)">
            <template #icon><component :is="item.icon" /></template>
            {{ item.label }}
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
  CalendarOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  FileSearchOutlined,
  PlusOutlined,
  ReloadOutlined,
  ScanOutlined,
  ToolOutlined,
  WarningOutlined
} from '@ant-design/icons-vue'
import VueApexCharts from 'vue3-apexcharts'
import { dashboardApi } from '../api/dashboardApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isManagerRole } from '../constants/business'
import { getDashboardAlertTarget } from '../utils/dashboardAlerts'
import { getApiErrorMessage } from '../utils/apiError'

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
const formatDateTime = value => value ? new Date(value).toLocaleString('vi-VN') : '—'
const formatUpdatedAt = value => value ? new Date(value).toLocaleString('vi-VN') : 'Chưa cập nhật'

const managerActionCards = computed(() => [
  {
    key: 'pending-borrow-requests',
    label: 'Phiếu chờ duyệt',
    value: formatNumber(stats.value.pendingBorrowRequests),
    description: 'Phiếu mượn cần kiểm tra',
    icon: FileSearchOutlined,
    tone: 'warning',
    route: { name: 'BorrowRequests' }
  },
  {
    key: 'overdue-borrow-records',
    label: 'Mượn quá hạn',
    value: formatNumber(stats.value.overdueBorrowRecords),
    description: 'Phiếu cần nhắc trả',
    icon: ClockCircleOutlined,
    tone: 'danger',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'low-stock-consumables',
    label: 'Vật tư dưới mức tối thiểu',
    value: formatNumber(stats.value.lowStockConsumables),
    description: 'Mặt hàng cần bổ sung',
    icon: AppstoreOutlined,
    tone: 'warning',
    route: { name: 'ConsumableRequests' }
  },
  {
    key: 'warranty-expiring-soon',
    label: 'Thiết bị sắp hết bảo hành',
    value: formatNumber(stats.value.warrantyExpiringSoon),
    description: 'Trong vòng 30 ngày',
    icon: ToolOutlined,
    tone: 'info',
    route: { name: 'Devices', query: { status: 'warranty' } }
  }
])

const equipmentStatusRows = computed(() => {
  const total = Number(stats.value.counts.total || 0)
  return [
    { key: 'total', label: 'Tổng thiết bị', value: total, tone: 'primary' },
    { key: 'available', label: 'Rảnh', value: Number(stats.value.counts.available || 0), tone: 'success' },
    { key: 'borrowed', label: 'Đang mượn', value: Number(stats.value.counts.borrowed || 0), tone: 'info' },
    { key: 'maintenance', label: 'Đang bảo trì', value: Number(stats.value.maintenanceInProgress || 0), tone: 'warning' },
    { key: 'broken', label: 'Hỏng', value: Number(stats.value.counts.broken || 0), tone: 'danger' },
    { key: 'warranty', label: 'Bảo hành', value: Number(stats.value.counts.warranty || 0), tone: 'purple' }
  ].map(item => ({ ...item, percent: total ? Math.max(item.key === 'total' ? 100 : 3, Math.round((item.value / total) * 100)) : 0 }))
})

const equipmentStatusSummary = computed(() => `${formatNumber(stats.value.counts.total)} thiết bị trong hệ thống`)
const recentActivities = computed(() => stats.value.activities.slice(0, 5))
const formattedUpdatedAt = computed(() => formatUpdatedAt(stats.value.updatedAt))

const alertMessage = (count, items, fallback) => {
  if (!count) return fallback
  if (count === 1 && items[0]?.message) return items[0].message
  return `Có ${formatNumber(count)} mục cần xử lý.`
}

const managerAlerts = computed(() => {
  const alerts = stats.value.alerts || []
  const byType = type => alerts.filter(alert => alert.type === type)
  const overdue = byType('overdue')
  const pending = byType('pending-borrow-requests')
  const lowStock = byType('low-stock')
  const warranty = byType('warranty-soon')
  return [
    {
      key: 'overdue', title: 'Mượn quá hạn', count: stats.value.overdueBorrowRecords,
      message: alertMessage(stats.value.overdueBorrowRecords, overdue, 'Không có phiếu mượn quá hạn.'),
      severity: 'Khẩn cấp', tone: 'danger', icon: WarningOutlined, route: { name: 'BorrowHistory' }
    },
    {
      key: 'pending', title: 'Phiếu chờ duyệt', count: stats.value.pendingBorrowRequests,
      message: alertMessage(stats.value.pendingBorrowRequests, pending, 'Không có phiếu chờ duyệt.'),
      severity: 'Cần xử lý', tone: 'warning', icon: FileSearchOutlined, route: { name: 'BorrowRequests' }
    },
    {
      key: 'low-stock', title: 'Vật tư sắp hết', count: stats.value.lowStockConsumables,
      message: alertMessage(stats.value.lowStockConsumables, lowStock, 'Không có vật tư dưới mức tối thiểu.'),
      severity: 'Cần xử lý', tone: 'warning', icon: AppstoreOutlined, route: { name: 'ConsumableRequests' }
    },
    {
      key: 'warranty', title: 'Bảo hành sắp hết', count: stats.value.warrantyExpiringSoon,
      message: alertMessage(stats.value.warrantyExpiringSoon, warranty, 'Không có thiết bị sắp hết bảo hành.'),
      severity: 'Theo dõi', tone: 'info', icon: ToolOutlined, route: { name: 'Devices', query: { status: 'warranty' } }
    }
  ].filter(alert => alert.count > 0)
})

const quickActions = [
  { key: 'borrow-requests', label: 'Duyệt phiếu mượn', icon: FileSearchOutlined, route: { name: 'BorrowRequests' } },
  { key: 'add-equipment', label: 'Thêm tài sản', icon: PlusOutlined, route: { name: 'Devices' } },
  { key: 'inventory', label: 'Tạo đợt kiểm kê', icon: ScanOutlined, route: { name: 'Inventory' } },
  { key: 'maintenance-schedule', label: 'Tạo kế hoạch bảo trì', icon: CalendarOutlined, route: { name: 'MaintenanceSchedules' } }
]

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
.manager-header, .section-heading, .panel-title-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.manager-header { margin-bottom: 24px; }
.manager-header h2, .header h2 { margin: 0; color: var(--color-ink); font-family: var(--font-serif); font-size: 32px; font-weight: 400; line-height: 1.15; letter-spacing: -0.02em; }
.subtitle, .section-heading p { margin: 6px 0 0; color: #64748b; font-size: 15px; }
.manager-header-actions { display: flex; flex-direction: column; align-items: flex-end; gap: 7px; }
.updated-at { color: #64748b; font-size: 12px; white-space: nowrap; }
.manager-section { margin-top: 22px; }
.section-heading { align-items: center; margin-bottom: 12px; }
.section-heading h3 { margin: 0; color: var(--color-ink); font-size: 19px; font-weight: 650; }
.action-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.manager-action-card { display: flex; align-items: center; gap: 11px; min-width: 0; padding: 16px; border: 1px solid var(--color-border); border-radius: 10px; background: #fff; cursor: pointer; transition: border-color .18s ease, box-shadow .18s ease; }
.manager-action-card:hover, .manager-action-card:focus-visible { border-color: var(--color-primary); box-shadow: 0 5px 14px rgba(217, 119, 87, .12); outline: none; }
.manager-action-icon { display: inline-flex; align-items: center; justify-content: center; width: 34px; height: 34px; flex: 0 0 34px; border-radius: 8px; font-size: 17px; }
.tone-warning { color: #b45309; background: #fff7ed; }
.tone-danger { color: #dc2626; background: #fef2f2; }
.tone-info { color: #2563eb; background: #eff6ff; }
.manager-action-copy { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 3px; }
.manager-action-copy strong { overflow: hidden; color: var(--color-ink); font-size: 14px; text-overflow: ellipsis; white-space: nowrap; }
.manager-action-value { color: var(--color-ink); font-size: 24px; font-weight: 700; line-height: 1; }
.manager-action-copy small { overflow: hidden; color: #64748b; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.manager-action-arrow { color: #94a3b8; flex: 0 0 auto; }
.equipment-status-card, .manager-panel { border: 1px solid var(--color-border); border-radius: 10px; background: #fff; box-shadow: none; }
.equipment-status-card :deep(.ant-card-body) { padding: 16px 20px; }
.equipment-status-list { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 16px; }
.equipment-status-row { display: grid; grid-template-columns: auto 1fr auto; align-items: center; gap: 8px; min-width: 0; }
.equipment-status-label { display: flex; align-items: center; gap: 6px; color: #475569; font-size: 13px; white-space: nowrap; }
.equipment-status-row > strong { color: var(--color-ink); font-size: 16px; }
.status-dot { width: 8px; height: 8px; flex: 0 0 8px; border-radius: 50%; }
.status-dot--primary, .status-progress--primary { background: var(--color-primary); }
.status-dot--success, .status-progress--success { background: #16a34a; }
.status-dot--info, .status-progress--info { background: #2563eb; }
.status-dot--warning, .status-progress--warning { background: #d97706; }
.status-dot--danger, .status-progress--danger { background: #dc2626; }
.status-dot--purple, .status-progress--purple { background: #7c3aed; }
.equipment-status-progress { min-width: 24px; height: 5px; overflow: hidden; border-radius: 99px; background: #f1f5f9; }
.equipment-status-progress span { display: block; height: 100%; min-width: 0; border-radius: inherit; }
.manager-two-column { display: grid; grid-template-columns: minmax(0, 1.2fr) minmax(360px, .8fr); gap: 16px; }
.manager-panel :deep(.ant-card-head) { min-height: 52px; padding: 0 18px; border-bottom: 1px solid #eef2f5; }
.manager-panel :deep(.ant-card-head-title) { padding: 15px 0; color: var(--color-ink); font-size: 17px; font-weight: 650; }
.manager-panel :deep(.ant-card-body) { padding: 16px 18px; }
.panel-title-row { align-items: center; }
.activity-list, .manager-alert-list { display: flex; flex-direction: column; gap: 13px; }
.activity-item { display: flex; align-items: flex-start; gap: 10px; }
.activity-dot { width: 8px; height: 8px; margin-top: 5px; flex: 0 0 8px; border-radius: 50%; background: #94a3b8; }
.activity-dot--green { background: #16a34a; }
.activity-dot--blue, .activity-dot--info { background: #2563eb; }
.activity-dot--orange, .activity-dot--warning { background: #d97706; }
.activity-dot--red, .activity-dot--error { background: #dc2626; }
.activity-copy { min-width: 0; }
.activity-time { color: #94a3b8; font-size: 12px; }
.activity-copy p { margin: 3px 0 0; color: #334155; font-size: 14px; line-height: 1.4; }
.manager-alert { display: flex; align-items: center; gap: 10px; padding: 11px 10px; border-left: 3px solid; border-radius: 7px; background: #fafafa; }
.manager-alert.alert-danger { border-left-color: #dc2626; background: #fff7f7; }
.manager-alert.alert-warning { border-left-color: #d97706; background: #fffbeb; }
.manager-alert.alert-info { border-left-color: #2563eb; background: #f5f9ff; }
.manager-alert-icon { display: inline-flex; align-items: center; justify-content: center; width: 28px; height: 28px; flex: 0 0 28px; border-radius: 7px; background: #fff; }
.alert-danger .manager-alert-icon { color: #dc2626; }
.alert-warning .manager-alert-icon { color: #d97706; }
.alert-info .manager-alert-icon { color: #2563eb; }
.manager-alert-copy { min-width: 0; flex: 1; }
.manager-alert-heading { display: flex; align-items: center; gap: 7px; }
.manager-alert-heading strong { color: var(--color-ink); font-size: 13px; }
.manager-alert-copy p { margin: 4px 0 0; overflow: hidden; color: #475569; font-size: 12px; line-height: 1.35; text-overflow: ellipsis; white-space: nowrap; }
.alert-action { color: var(--color-primary); }
.quick-actions-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.quick-action-button { height: 42px; border-color: var(--color-border); color: var(--color-ink); text-align: left; }
.quick-action-button:hover, .quick-action-button:focus { border-color: var(--color-primary); color: var(--color-primary); }
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
  .action-grid, .quick-actions-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .equipment-status-list { grid-template-columns: repeat(3, minmax(0, 1fr)); }
  .manager-two-column { grid-template-columns: 1fr; }
}
@media (max-width: 767px) {
  .manager-header, .section-heading { align-items: flex-start; flex-direction: column; }
  .manager-header-actions { align-items: flex-start; }
  .action-grid, .quick-actions-grid, .equipment-status-list, .borrower-content-grid { grid-template-columns: 1fr; }
  .manager-action-copy strong { white-space: normal; }
  .equipment-status-row { grid-template-columns: 132px 1fr auto; }
  .borrower-status-panel { grid-column: auto; }
  .manager-header h2, .header h2 { font-size: 27px; }
}
</style>
