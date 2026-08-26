<template>
  <div class="overview-container">
    <div class="header">
      <h2 class="serif-title">Tổng quan hệ thống</h2>
      <p class="subtitle">Theo dõi nhanh thiết bị, yêu cầu và cảnh báo cần xử lý.</p>
    </div>

    <a-row v-if="isManager && stats.advanced" :gutter="[12, 12]" class="stat-grid">
      <a-col v-for="item in managerStats" :key="item.label" :xs="24" :sm="12" :lg="6">
        <a-card class="stat-card is-clickable" :class="item.tone" @click="openEquipmentList(item.filter)">
          <div class="stat-icon">
            <component :is="item.icon" />
          </div>
          <div class="stat-info">
            <span class="label">{{ item.label }}</span>
            <span class="value">{{ item.value }}</span>
            <span v-if="item.note" class="note">{{ item.note }}</span>
          </div>
        </a-card>
      </a-col>
    </a-row>

    <a-row v-else :gutter="[12, 12]" class="stat-grid">
      <a-col v-for="item in studentStats" :key="item.label" :xs="24" :sm="12">
        <a-card class="stat-card" :class="item.tone">
          <div class="stat-icon">
            <component :is="item.icon" />
          </div>
          <div class="stat-info">
            <span class="label">{{ item.label }}</span>
            <span class="value">{{ item.value }}</span>
          </div>
        </a-card>
      </a-col>
    </a-row>

    <div v-if="!isManager" class="overview-content-grid">
      <a-card title="Hoạt động gần đây" :bordered="false" class="timeline-card overview-activity-card">
        <a-timeline>
          <a-timeline-item v-for="(act, index) in stats.activities" :key="index" :color="act.color">
            <p class="timeline-date">{{ new Date(act.date).toLocaleString('vi-VN') }}</p>
            <p class="timeline-content">{{ act.message }}</p>
          </a-timeline-item>
          <a-empty v-if="!stats.activities.length" description="Chưa có hoạt động nào" />
        </a-timeline>
      </a-card>

      <a-card title="Cảnh báo cần xử lý" :bordered="false" class="alert-card overview-alert-card">
        <div
          v-for="(alert, index) in stats.alerts"
          :key="index"
          class="compact-alert is-clickable"
          :class="alert.level"
          role="button"
          tabindex="0"
          :aria-label="`${alert.title}: ${alert.message}`"
          @click="handleAlertClick(alert)"
          @keydown.enter.prevent="handleAlertClick(alert)"
          @keydown.space.prevent="handleAlertClick(alert)"
        >
          <div class="compact-alert-icon">
            <component :is="getAlertIcon(alert.level)" />
          </div>
          <div class="compact-alert-content">
            <div class="compact-alert-title">{{ alert.title }}</div>
            <div class="compact-alert-desc">{{ alert.message }}</div>
          </div>
          <a-tooltip title="Xem và xử lý cảnh báo">
            <arrow-right-outlined class="compact-alert-arrow" />
          </a-tooltip>
        </div>
        <a-empty v-if="!stats.alerts.length" description="Không có cảnh báo" />
      </a-card>

      <a-card title="Trạng thái thiết bị" :bordered="false" class="chart-card overview-status-card">
        <apexchart type="donut" height="220" :options="pieOptions" :series="pieSeries"></apexchart>
      </a-card>
    </div>

    <template v-else>
      <a-row :gutter="[16, 16]" class="priority-row">
        <a-col :xs="24" :xl="16">
          <a-card title="Xu hướng mượn thiết bị (6 tháng qua)" :bordered="false" class="chart-card trend-card">
            <apexchart type="bar" height="226" :options="barOptions" :series="barSeries"></apexchart>
          </a-card>
        </a-col>

        <a-col :xs="24" :xl="8">
          <a-card title="Cảnh báo cần xử lý" :bordered="false" class="alert-card priority-alert">
            <div
              v-for="(alert, index) in stats.alerts"
              :key="index"
              class="compact-alert is-clickable"
              :class="alert.level"
              role="button"
              tabindex="0"
              :aria-label="`${alert.title}: ${alert.message}`"
              @click="handleAlertClick(alert)"
              @keydown.enter.prevent="handleAlertClick(alert)"
              @keydown.space.prevent="handleAlertClick(alert)"
            >
              <div class="compact-alert-icon">
                <component :is="getAlertIcon(alert.level)" />
              </div>
              <div class="compact-alert-content">
                <div class="compact-alert-title">{{ alert.title }}</div>
                <div class="compact-alert-desc">{{ alert.message }}</div>
              </div>
              <a-tooltip title="Xem và xử lý cảnh báo">
                <arrow-right-outlined class="compact-alert-arrow" />
              </a-tooltip>
            </div>
            <a-empty v-if="!stats.alerts.length" description="Không có cảnh báo" />
          </a-card>
        </a-col>
      </a-row>

      <a-card title="Thông tin quản trị" :bordered="false" class="admin-info-card">
        <div class="admin-info-list">
          <div class="admin-info-item">
            <team-outlined />
            <span>Tổng số người dùng</span>
            <strong>{{ stats.advanced.totalUsers }}</strong>
          </div>
          <div class="admin-info-item">
            <pay-circle-outlined />
            <span>Bồi thường đã thu</span>
            <strong>{{ formatCurrency(stats.advanced.totalPenalties) }}</strong>
          </div>
        </div>
      </a-card>

      <a-row :gutter="[16, 16]" class="secondary-row">
        <a-col :xs="24" :xl="16">
          <a-card title="Hoạt động gần đây" :bordered="false" class="timeline-card">
            <a-timeline>
              <a-timeline-item v-for="(act, index) in stats.activities" :key="index" :color="act.color">
                <p class="timeline-date">{{ new Date(act.date).toLocaleString('vi-VN') }}</p>
                <p class="timeline-content">{{ act.message }}</p>
              </a-timeline-item>
              <a-empty v-if="!stats.activities.length" description="Chưa có hoạt động nào" />
            </a-timeline>
          </a-card>
        </a-col>

        <a-col :xs="24" :xl="8">
          <a-card title="Trạng thái thiết bị" :bordered="false" class="chart-card">
            <apexchart type="donut" height="220" :options="pieOptions" :series="pieSeries"></apexchart>
          </a-card>
        </a-col>
      </a-row>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { message } from 'ant-design-vue'
import {
  ArrowRightOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  DesktopOutlined,
  PayCircleOutlined,
  TeamOutlined,
  ToolOutlined,
  WarningOutlined,
} from '@ant-design/icons-vue'
import VueApexCharts from 'vue3-apexcharts'
import { dashboardApi } from '../api/dashboardApi'
import { useAuthStore } from '../stores/authStore'
import { isManagerRole } from '../constants/business'
import { getDashboardAlertTarget } from '../utils/dashboardAlerts'

const authStore = useAuthStore()
const router = useRouter()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))
const apexchart = VueApexCharts

const stats = ref({
  counts: { total: 0, available: 0, maintenance: 0, borrowed: 0, broken: 0, warranty: 0 },
  activities: [],
  alerts: [],
  advanced: { totalUsers: 0, totalPenalties: 0, pendingRequests: 0, lowStockConsumables: [], borrowTrends: [] }
})

const formatCurrency = (value) => `${Number(value || 0).toLocaleString('vi-VN')} ₫`

const managerStats = computed(() => [
  { label: 'Tổng thiết bị', value: stats.value.counts.total, icon: DesktopOutlined, tone: 'coral', filter: 'all' },
  { label: 'Thiết bị rảnh', value: stats.value.counts.available, icon: CheckCircleOutlined, tone: 'green', filter: 'Rảnh' },
  { label: 'Đang mượn', value: stats.value.counts.borrowed, icon: ClockCircleOutlined, tone: 'amber', filter: 'Đang mượn' },
  {
    label: 'Hỏng/Bảo hành',
    value: (stats.value.counts.broken || 0) + (stats.value.counts.warranty || 0),
    note: `Hỏng ${stats.value.counts.broken || 0} / Bảo hành ${stats.value.counts.warranty || 0}`,
    icon: ToolOutlined,
    tone: 'red',
    filter: 'problem'
  }
])

const studentStats = computed(() => [
  { label: 'Đang mượn', value: stats.value.counts.borrowed, icon: ClockCircleOutlined, tone: 'amber' },
  { label: 'Thiết bị rảnh', value: stats.value.counts.available, icon: CheckCircleOutlined, tone: 'green' }
])

const barOptions = ref({
  chart: { id: 'borrow-trends', toolbar: { show: false }, parentHeightOffset: 0 },
  grid: { borderColor: 'rgba(0,0,0,0.05)', padding: { top: 0, right: 8, bottom: -4, left: 4 } },
  xaxis: { categories: [] },
  colors: ['var(--color-primary)'],
  plotOptions: { bar: { borderRadius: 6, columnWidth: '34%', dataLabels: { position: 'top' } } },
  dataLabels: { enabled: true, offsetY: -18, style: { colors: ['var(--color-ink)'], fontSize: '11px', fontWeight: 700 } },
  yaxis: { labels: { style: { colors: '#64748b', fontSize: '11px' } } }
})
const barSeries = ref([{ name: 'Lượt mượn', data: [] }])

const pieOptions = ref({
  chart: { type: 'donut' },
  labels: ['Rảnh', 'Đang mượn', 'Bảo hành', 'Hỏng'],
  colors: ['#52c41a', '#1890ff', '#faad14', '#f5222d'],
  legend: { position: 'bottom' },
  dataLabels: { enabled: true }
})
const pieSeries = ref([0, 0, 0, 0])

const openEquipmentList = (filter) => {
  router.push({
    name: 'Devices',
    query: filter && filter !== 'all' ? { status: filter } : {}
  })
}

const handleAlertClick = (alert) => {
  const target = getDashboardAlertTarget(alert?.type)
  if (target) router.push(target)
}

const getAlertIcon = (level) => {
  if (level === 'info') return ClockCircleOutlined
  return WarningOutlined
}

onMounted(async () => {
  try {
    const res = await dashboardApi.getStats()
    stats.value = res

    pieSeries.value = [
      res.counts.available,
      res.counts.borrowed,
      res.counts.warranty ?? 0,
      res.counts.broken ?? 0
    ]

    if (res.advanced?.borrowTrends) {
      barOptions.value = {
        ...barOptions.value,
        xaxis: { categories: res.advanced.borrowTrends.map(t => t.month) }
      }
      barSeries.value = [{
        name: 'Lượt mượn',
        data: res.advanced.borrowTrends.map(t => t.count)
      }]
    }
  } catch (err) {
    message.error('Lỗi lấy dữ liệu thống kê')
  }
})
</script>

<style scoped>
.overview-container {
  padding: 0;
}

.header {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 16px;
}

.header h2 {
  font-family: var(--font-serif);
  font-size: 32px;
  font-weight: 400;
  color: var(--color-ink);
  margin: 0;
  letter-spacing: -0.02em;
  line-height: 1.15;
}

.subtitle {
  color: #64748b;
  font-size: 15px;
  margin: 0;
}

.stat-grid {
  margin-bottom: 16px;
}

.stat-card {
  border: 1px solid rgba(0,0,0,0.05);
  border-radius: 12px;
  box-shadow: none;
  background: #ffffff;
  transition: transform 0.18s ease, box-shadow 0.18s ease;
}

.stat-card.is-clickable {
  cursor: pointer;
}

.stat-card.is-clickable:hover {
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.04);
  transform: translateY(-2px);
}

.stat-card :deep(.ant-card-body) {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 20px 22px;
}

.stat-icon {
  width: 54px;
  height: 54px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 54px;
  font-size: 24px;
}

.stat-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.stat-info .label {
  color: #334155;
  font-size: 14px;
  font-weight: 600;
  line-height: 1.25;
}

.stat-info .value {
  color: #0f172a;
  font-size: 28px;
  font-weight: 700;
  line-height: 1.05;
  margin-top: 6px;
}

.stat-info .note {
  color: #64748b;
  font-size: 12px;
  font-weight: 500;
  line-height: 1.2;
  margin-top: 6px;
}

.stat-card.coral .stat-icon { color: var(--color-primary); background: rgba(217, 119, 87, 0.1); }
.stat-card.green .stat-icon { color: #059669; background: #ecfdf5; }
.stat-card.amber .stat-icon { color: #d97706; background: #fffbeb; }
.stat-card.red .stat-icon { color: #dc2626; background: #fef2f2; }

.priority-row {
  align-items: stretch;
}

.priority-row :deep(.ant-col) {
  display: flex;
}

.chart-card,
.timeline-card,
.alert-card,
.admin-info-card {
  width: 100%;
  border: 1px solid rgba(0,0,0,0.05);
  border-radius: 12px;
  box-shadow: none;
  background: #ffffff;
  overflow: hidden;
}

.chart-card :deep(.ant-card-head),
.timeline-card :deep(.ant-card-head),
.alert-card :deep(.ant-card-head),
.admin-info-card :deep(.ant-card-head) {
  padding: 0 20px;
  border-bottom: 1px solid rgba(0,0,0,0.05);
}

.chart-card :deep(.ant-card-head-title),
.timeline-card :deep(.ant-card-head-title),
.alert-card :deep(.ant-card-head-title),
.admin-info-card :deep(.ant-card-head-title) {
  color: #0f172a;
  font-size: 18px;
  font-weight: 600;
  padding: 15px 0;
}

.chart-card :deep(.ant-card-body),
.timeline-card :deep(.ant-card-body),
.alert-card :deep(.ant-card-body),
.admin-info-card :deep(.ant-card-body) {
  padding: 16px 20px;
}

.trend-card :deep(.ant-card-body) {
  padding: 16px 20px 8px;
}

.compact-alert {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 12px 12px 14px;
  margin-bottom: 10px;
  border: 1px solid rgba(0,0,0,0.05);
  border-left-width: 4px;
  border-radius: 12px;
  background: #ffffff;
}

.compact-alert.is-clickable {
  cursor: pointer;
}

.compact-alert.is-clickable:hover,
.compact-alert.is-clickable:focus-visible {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(15, 58, 90, 0.1);
  outline: none;
}

.compact-alert.warning {
  border-left-color: #f59e0b;
  background: #fffcf2;
}

.compact-alert.info {
  border-left-color: #2563eb;
  background: #f5f9ff;
}

.compact-alert.error {
  border-left-color: #dc2626;
  background: #fff7f7;
}

.compact-alert.success {
  border-left-color: #059669;
  background: #f7fffb;
}

.compact-alert-icon {
  width: 34px;
  height: 34px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 34px;
  font-size: 18px;
  background: #ffffff;
}

.compact-alert.warning .compact-alert-icon { color: #d97706; }
.compact-alert.info .compact-alert-icon { color: #2563eb; }
.compact-alert.error .compact-alert-icon { color: #dc2626; }
.compact-alert.success .compact-alert-icon { color: #059669; }

.compact-alert-content {
  flex: 1;
  min-width: 0;
}

.compact-alert-title {
  color: #0f172a;
  font-size: 14px;
  font-weight: 600;
  line-height: 1.25;
}

.compact-alert-desc {
  color: #475569;
  font-size: 13px;
  line-height: 1.35;
  margin-top: 5px;
}

.compact-alert-arrow {
  color: #94a3b8;
  flex: 0 0 auto;
}

.admin-info-card {
  margin-top: 16px;
}

.admin-info-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.admin-info-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #f8fafc;
  color: #475569;
  font-size: 14px;
  font-weight: 500;
}

.admin-info-item .anticon {
  color: #315efb;
  font-size: 18px;
}

.admin-info-item strong {
  margin-left: auto;
  color: #0f172a;
  font-size: 16px;
  font-weight: 700;
  text-decoration: none;
  white-space: nowrap;
}

.timeline-date {
  color: #9ca3af;
  font-size: 12px;
  margin-bottom: 4px;
}

.timeline-content {
  color: #374151;
  font-weight: 500;
}

.overview-content-grid {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(280px, 1fr);
  gap: 16px;
  align-items: start;
}

.overview-content-grid > .ant-card {
  min-width: 0;
  align-self: start;
}

.overview-status-card {
  grid-column: 1 / -1;
}

@media (max-width: 991px) {
  .header h2 {
    font-size: 26px;
  }

  .priority-row :deep(.ant-col) {
    display: block;
  }

  .overview-content-grid {
    grid-template-columns: 1fr;
  }

  .overview-status-card {
    grid-column: 1;
  }
}

@media (max-width: 575px) {
  .stat-card :deep(.ant-card-body) {
    padding: 16px;
  }

  .stat-icon {
    width: 48px;
    height: 48px;
    flex-basis: 48px;
    font-size: 22px;
  }

  .admin-info-list {
    grid-template-columns: 1fr;
  }
}
</style>
