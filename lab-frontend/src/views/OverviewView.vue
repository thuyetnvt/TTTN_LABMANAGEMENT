<template>
  <div class="overview-container">
    <section v-if="initialLoading" class="dashboard-loading-shell" aria-label="Đang tải dữ liệu tổng quan">
      <a-skeleton active :title="{ width: '34%' }" :paragraph="{ rows: 1, width: ['52%'] }" />
      <div class="dashboard-loading-kpis">
        <a-skeleton-button v-for="index in 4" :key="index" active block />
      </div>
      <div class="dashboard-loading-panels">
        <a-card v-for="index in 2" :key="index" :bordered="false">
          <a-skeleton active :title="{ width: '42%' }" :paragraph="{ rows: 5 }" />
        </a-card>
      </div>
    </section>

    <template v-else-if="isManager">
      <header class="manager-header">
        <div>
          <h2>Tổng quan quản trị</h2>
          <p class="subtitle">Các công việc và cảnh báo cần xử lý trong Phòng Lab IoT.</p>
        </div>
        <div class="manager-header-actions">
          <a-button :loading="refreshing" @click="refreshStats(true)">
            <template #icon><ReloadOutlined /></template>
            Làm mới
          </a-button>
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
            </div>
          </div>
        </div>
      </section>

      <section class="manager-two-column manager-section manager-primary-grid">
        <a-card :bordered="false" class="manager-panel manager-chart-panel">
          <template #title>Trạng thái thiết bị</template>
          <div v-if="hasManagerStatusData" class="manager-donut-layout">
            <apexchart type="donut" height="230" :options="managerDonutOptions" :series="managerDonutSeries" />
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
              </span>
              <strong class="manager-attention-value">{{ item.value }}</strong>
              <a-button type="text" class="manager-attention-action" :aria-label="`Xem ${item.label}`" @click="navigateTo(item.route)">
                <ArrowRightOutlined />
              </a-button>
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
            height="230"
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
                Xem tất cả
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

    <template v-else-if="isTeacher">
      <header class="teacher-header">
        <div>
          <span class="teacher-eyebrow">Không gian giảng viên</span>
          <h2>Chào {{ teacherDisplayName }}</h2>
          <p class="subtitle">Theo dõi yêu cầu bảo lãnh và việc mượn thiết bị của bạn tại một nơi.</p>
        </div>
        <a-button :loading="refreshing" class="teacher-refresh-button" @click="refreshStats(true)">
          <template #icon><ReloadOutlined /></template>
          Làm mới
        </a-button>
      </header>

      <section class="teacher-kpi-grid" aria-label="Công việc của giảng viên">
        <button
          v-for="item in teacherKpis"
          :key="item.key"
          type="button"
          class="teacher-kpi-card"
          :class="`teacher-tone-${item.tone}`"
          @click="navigateTo(item.route)"
        >
          <span class="teacher-kpi-icon" aria-hidden="true"><component :is="item.icon" /></span>
          <span class="teacher-kpi-copy">
            <small>{{ item.label }}</small>
            <strong>{{ item.value }}</strong>
            <span>{{ item.hint }}</span>
          </span>
          <ArrowRightOutlined class="teacher-kpi-arrow" />
        </button>
      </section>

      <section class="teacher-main-grid">
        <a-card :bordered="false" class="teacher-panel teacher-task-panel">
          <template #title>
            <div class="teacher-panel-title">
              <span>Việc cần làm</span>
              <small>{{ teacherTasks.length ? `${teacherTasks.length} nhóm việc` : 'Đã xử lý hết' }}</small>
            </div>
          </template>
          <div v-if="teacherTasks.length" class="teacher-task-list">
            <button v-for="task in teacherTasks" :key="task.key" type="button" class="teacher-task-item" @click="navigateTo(task.route)">
              <span class="teacher-task-icon" :class="`teacher-tone-${task.tone}`"><component :is="task.icon" /></span>
              <span class="teacher-task-copy"><strong>{{ task.title }}</strong><small>{{ task.description }}</small></span>
              <span class="teacher-task-count">{{ task.value }}</span>
              <ArrowRightOutlined class="teacher-task-arrow" />
            </button>
          </div>
          <div v-else class="teacher-clear-state">
            <span><CheckCircleOutlined /></span>
            <div><strong>Không có việc tồn đọng</strong><p>Các yêu cầu và hạn trả hiện đều ổn.</p></div>
          </div>
        </a-card>

        <a-card :bordered="false" class="teacher-panel teacher-return-panel">
          <template #title>Mốc trả gần nhất</template>
          <div v-if="teacherSummary.nextReturnDate" class="teacher-return-content">
            <div class="teacher-return-date">
              <span>{{ teacherReturnDay }}</span>
              <small>{{ teacherReturnMonth }}</small>
            </div>
            <div class="teacher-return-copy">
              <span>Thiết bị cần trả</span>
              <strong>{{ teacherSummary.nextReturnEquipment || 'Thiết bị đang mượn' }}</strong>
              <small>{{ teacherReturnState }}</small>
            </div>
            <a-button block @click="navigateTo({ name: 'BorrowHistory' })">Xem lịch sử mượn <ArrowRightOutlined /></a-button>
          </div>
          <div v-else class="teacher-no-return">
            <span><CheckCircleOutlined /></span>
            <strong>Chưa có lịch trả sắp tới</strong>
            <p>Bạn hiện không giữ thiết bị nào cần theo dõi hạn trả.</p>
            <a-button @click="navigateTo({ name: 'Devices' })">Xem thiết bị có thể mượn</a-button>
          </div>
        </a-card>
      </section>

      <a-card :bordered="false" class="teacher-panel teacher-activity-panel">
        <template #title>
          <div class="teacher-panel-title">
            <span>Hoạt động liên quan đến bạn</span>
            <a-button type="link" @click="navigateTo({ name: 'BorrowHistory' })">Xem lịch sử</a-button>
          </div>
        </template>
        <div v-if="teacherActivities.length" class="teacher-activity-list">
          <div v-for="(activity, index) in teacherActivities" :key="`${activity.date}-${index}`" class="teacher-activity-item">
            <span class="teacher-activity-dot" :class="`activity-dot-${activity.color || 'blue'}`" />
            <div><p>{{ activity.message }}</p><small>{{ formatDateTime(activity.date) }}</small></div>
          </div>
        </div>
        <div v-else class="teacher-inline-empty">Chưa có hoạt động mượn hoặc bảo lãnh gần đây.</div>
      </a-card>

      <section class="teacher-quick-section" aria-labelledby="teacher-quick-heading">
        <h3 id="teacher-quick-heading">Thao tác nhanh</h3>
        <div class="teacher-quick-grid">
          <button v-for="item in teacherQuickActions" :key="item.key" type="button" class="teacher-quick-action" @click="navigateTo(item.route)">
            <span><component :is="item.icon" /></span>
            <strong>{{ item.label }}</strong>
            <small>{{ item.description }}</small>
            <ArrowRightOutlined />
          </button>
        </div>
      </section>
    </template>

    <template v-else-if="isStudent">
      <header class="student-header">
        <div>
          <span class="student-eyebrow">Không gian sinh viên</span>
          <h2>Xin chào {{ studentDisplayName }}</h2>
          <p class="subtitle">Theo dõi các phiếu mượn và thiết bị đang thuộc trách nhiệm của bạn.</p>
        </div>
        <a-button :loading="refreshing" class="student-refresh-button" @click="refreshStats(true)">
          <template #icon><ReloadOutlined /></template>
          Làm mới
        </a-button>
      </header>

      <section class="student-kpi-grid" aria-label="Chỉ số cá nhân">
        <button
          v-for="item in studentStats"
          :key="item.key"
          type="button"
          class="student-kpi-card"
          :class="`student-tone-${item.tone}`"
          @click="navigateTo(item.route)"
        >
          <span class="student-kpi-icon" aria-hidden="true"><component :is="item.icon" /></span>
          <span class="student-kpi-copy">
            <small>{{ item.label }}</small>
            <strong>{{ item.value }}</strong>
            <span>{{ item.hint }}</span>
          </span>
          <ArrowRightOutlined class="student-kpi-arrow" />
        </button>
      </section>

      <section class="student-main-grid">
        <a-card :bordered="false" class="student-panel student-activity-panel">
          <template #title>
            <div class="student-panel-title">
              <span>Hoạt động của bạn</span>
              <a-button type="link" @click="navigateTo({ name: 'BorrowHistory' })">Xem lịch sử</a-button>
            </div>
          </template>
          <a-timeline v-if="stats.activities.length">
            <a-timeline-item v-for="(activity, index) in stats.activities" :key="index" :color="activity.color">
              <p class="timeline-date">{{ formatDateTime(activity.date) }}</p>
              <p class="timeline-content">{{ activity.message }}</p>
            </a-timeline-item>
          </a-timeline>
          <a-empty v-else description="Chưa có hoạt động mượn nào" />
        </a-card>

        <a-card :bordered="false" class="student-panel student-return-panel">
          <template #title>Mốc trả gần nhất</template>
          <div v-if="studentSummary.nextReturnDate" class="student-return-content">
            <div class="student-return-date">
              <span>{{ studentReturnDay }}</span>
              <small>{{ studentReturnMonth }}</small>
            </div>
            <div class="student-return-copy">
              <span>Thiết bị cần trả</span>
              <strong>{{ studentSummary.nextReturnEquipment || 'Thiết bị đang mượn' }}</strong>
              <small>{{ studentReturnState }}</small>
            </div>
            <a-button block @click="navigateTo({ name: 'BorrowHistory' })">Xem lịch sử mượn <ArrowRightOutlined /></a-button>
          </div>
          <div v-else class="student-no-return">
            <span><CheckCircleOutlined /></span>
            <strong>Chưa có lịch trả sắp tới</strong>
            <p>Bạn hiện không giữ thiết bị nào cần theo dõi hạn trả.</p>
            <a-button @click="navigateTo({ name: 'Devices' })">Xem thiết bị có thể mượn</a-button>
          </div>
        </a-card>
      </section>

      <section class="student-bottom-grid">
        <a-card :bordered="false" class="student-panel">
          <template #title>Tình trạng phiếu mượn của bạn</template>
          <apexchart
            v-if="hasStudentStatusData"
            type="donut"
            height="220"
            :options="studentDonutOptions"
            :series="studentDonutSeries"
          />
          <a-empty v-else description="Chưa có phiếu mượn" />
        </a-card>

        <a-card :bordered="false" class="student-panel">
          <template #title>Cảnh báo của bạn</template>
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
          <a-empty v-if="!stats.alerts.length" description="Không có cảnh báo cá nhân" />
        </a-card>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, defineAsyncComponent, onMounted, ref } from 'vue'
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
import { dashboardApi } from '../api/dashboardApi'
import { useAuthStore } from '../stores/authStore'
import { isAdminRole, isManagerRole, isStudentRole, isTeacherRole } from '../constants/business'
import { getDashboardAlertTarget } from '../utils/dashboardAlerts'
import { getApiErrorMessage } from '../utils/apiError'
import { formatVietnamDateTime } from '../utils/dateTime.js'

const VueApexCharts = defineAsyncComponent(() => import('vue3-apexcharts'))

const router = useRouter()
const authStore = useAuthStore()
const role = computed(() => authStore.role)
const isManager = computed(() => isManagerRole(role.value))
const isTeacher = computed(() => isTeacherRole(role.value))
const isStudent = computed(() => isStudentRole(role.value))
const canViewAuditLogs = computed(() => isAdminRole(role.value))
const apexchart = VueApexCharts
const refreshing = ref(false)
const initialLoading = ref(true)

const stats = ref({
  updatedAt: null,
  pendingBorrowRequests: 0,
  pendingConsumableRequests: 0,
  borrowRequestsToProcess: 0,
  consumableRequestsToProcess: 0,
  overdueBorrowRecords: 0,
  lowStockConsumables: 0,
  warrantyExpiringSoon: 0,
  maintenanceInProgress: 0,
  counts: { total: 0, available: 0, borrowPending: 0, maintenance: 0, borrowed: 0, broken: 0, missing: 0, warranty: 0 },
  activities: [],
  alerts: [],
  advanced: { pendingRequests: 0, lowStockConsumables: [], borrowTrends: [] },
  teacherSummary: { pendingApprovals: 0, pendingOwnRequests: 0, activeBorrows: 0, nextReturnDate: null, nextReturnEquipment: '' },
  studentSummary: {
    pendingRequests: 0,
    approvedRequests: 0,
    activeBorrows: 0,
    returnedBorrows: 0,
    nextReturnDate: null,
    nextReturnEquipment: '',
    statusCounts: { pending: 0, approved: 0, active: 0, returned: 0, rejected: 0, cancelled: 0, expired: 0 }
  }
})

const formatNumber = value => Number(value || 0).toLocaleString('vi-VN')
const formatDateTime = value => formatVietnamDateTime(value)

const managerKpis = computed(() => [
  {
    key: 'total-equipment',
    label: 'Tổng thiết bị',
    value: formatNumber(stats.value.counts.total),
    icon: AppstoreOutlined,
    tone: 'primary'
  },
  {
    key: 'borrowed-equipment',
    label: 'Thiết bị đang được mượn',
    value: formatNumber(stats.value.counts.borrowed),
    icon: ClockCircleOutlined,
    tone: 'success'
  },
  {
    key: 'pending-work',
    label: 'Chờ xử lý',
    value: formatNumber(stats.value.advanced?.pendingRequests ?? stats.value.pendingBorrowRequests),
    icon: FileSearchOutlined,
    tone: 'warning'
  },
  {
    key: 'broken-warranty',
    label: 'Hỏng/Bảo hành',
    value: formatNumber(Number(stats.value.counts.broken || 0) + Number(stats.value.counts.warranty || 0)),
    icon: ToolOutlined,
    tone: 'danger'
  }
])

const managerStatusRows = computed(() => [
  { key: 'available', label: 'Rảnh', value: Number(stats.value.counts.available || 0), tone: 'success' },
  { key: 'borrow-pending', label: 'Đã giữ chỗ', value: Number(stats.value.counts.borrowPending || 0), tone: 'warning' },
  { key: 'borrowed', label: 'Đang mượn', value: Number(stats.value.counts.borrowed || 0), tone: 'info' },
  { key: 'maintenance', label: 'Bảo trì', value: Number(stats.value.counts.maintenance || 0), tone: 'purple' },
  { key: 'warranty', label: 'Bảo hành', value: Number(stats.value.counts.warranty || 0), tone: 'warning' },
  { key: 'broken', label: 'Hỏng', value: Number(stats.value.counts.broken || 0), tone: 'danger' },
  { key: 'missing', label: 'Thất lạc', value: Number(stats.value.counts.missing || 0), tone: 'danger' }
])

const hasManagerStatusData = computed(() => managerStatusRows.value.some(item => item.value > 0))
const managerDonutSeries = computed(() => managerStatusRows.value.map(item => item.value))
const managerDonutOptions = computed(() => ({
  chart: { type: 'donut', toolbar: { show: false }, fontFamily: 'inherit' },
  labels: managerStatusRows.value.map(item => item.label),
  colors: ['#7FBD68', '#F2B24B', '#4D91D8', '#8B5CF6', '#EAB308', '#E35F4E', '#991B1B'],
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
          name: { show: true, color: '#64748b', fontSize: '14px', offsetY: 20 },
          value: { show: true, color: '#10233F', fontSize: '28px', fontWeight: 700, offsetY: -12, formatter: value => formatNumber(value) },
          total: { show: true, showAlways: true, label: 'Thiết bị', color: '#64748b', formatter: chartContext => formatNumber(chartContext.globals.seriesTotals.reduce((sum, value) => sum + value, 0)) }
        }
      }
    }
  }
}))

const recentActivities = computed(() => stats.value.activities.slice(0, 4))

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
    key: 'pending-borrow-requests', label: 'Phiếu mượn cần xử lý',
    value: formatNumber(stats.value.borrowRequestsToProcess),
    icon: FileSearchOutlined, tone: 'warning', route: { name: 'BorrowRequests' }
  },
  {
    key: 'pending-consumable-requests', label: 'Cấp phát cần xử lý',
    value: formatNumber(stats.value.consumableRequestsToProcess),
    icon: AppstoreOutlined, tone: 'warning', route: { name: 'ConsumableRequests' }
  },
  {
    key: 'overdue-borrow-records', label: 'Mượn quá hạn',
    value: formatNumber(stats.value.overdueBorrowRecords),
    icon: ClockCircleOutlined, tone: 'danger', route: { name: 'BorrowHistory', query: { status: 'OVERDUE' } }
  },
  {
    key: 'low-stock-consumables', label: 'Vật tư sắp hết',
    value: formatNumber(stats.value.lowStockConsumables),
    icon: AppstoreOutlined, tone: 'warning', route: { name: 'Devices', query: { tab: 'consumables', stock: 'LOW_STOCK' } }
  },
  {
    key: 'warranty-expiring-soon', label: 'Bảo hành sắp hết',
    value: formatNumber(stats.value.warrantyExpiringSoon),
    icon: ToolOutlined, tone: 'info', route: { name: 'Devices', query: { status: 'warranty-soon' } }
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
    labels: { style: { colors: '#64748b', fontSize: '13px' } },
    axisBorder: { color: '#d9dee3' },
    axisTicks: { color: '#d9dee3' }
  },
  yaxis: { min: 0, forceNiceScale: true, labels: { style: { colors: '#64748b', fontSize: '13px' }, formatter: value => Math.round(value) } },
  tooltip: { y: { formatter: value => `${formatNumber(value)} lượt` } }
}))

const managerQuickActions = computed(() => [
  ...(canViewAuditLogs.value ? [{ key: 'users', label: 'Quản lý người dùng', icon: TeamOutlined, route: { name: 'AdminUsers' } }] : []),
  { key: 'borrow-requests', label: 'Duyệt phiếu', icon: FileSearchOutlined, route: { name: 'BorrowRequests' } },
  { key: 'add-equipment', label: 'Thêm tài sản', icon: PlusOutlined, route: { name: 'Devices' } },
  { key: 'reports', label: 'Xem báo cáo', icon: AppstoreOutlined, route: { name: 'Reports' } }
])

const teacherSummary = computed(() => stats.value.teacherSummary || {})
const teacherDisplayName = computed(() => authStore.user?.fullName?.trim() || 'Giảng viên')
const teacherKpis = computed(() => [
  {
    key: 'teacher-approvals', label: 'Chờ bạn bảo lãnh',
    value: formatNumber(teacherSummary.value.pendingApprovals),
    hint: 'Yêu cầu của sinh viên', icon: FileSearchOutlined, tone: 'coral', route: { name: 'TeacherApproval' }
  },
  {
    key: 'own-pending', label: 'Phiếu đang xử lý',
    value: formatNumber(teacherSummary.value.pendingOwnRequests),
    hint: 'Yêu cầu mượn của bạn', icon: ClockCircleOutlined, tone: 'amber', route: { name: 'BorrowHistory' }
  },
  {
    key: 'active-borrows', label: 'Đang mượn',
    value: formatNumber(teacherSummary.value.activeBorrows),
    hint: 'Phiếu đang sử dụng', icon: AppstoreOutlined, tone: 'blue', route: { name: 'BorrowHistory' }
  },
  {
    key: 'overdue', label: 'Đã quá hạn',
    value: formatNumber(stats.value.overdueBorrowRecords),
    hint: 'Cần xử lý sớm', icon: WarningOutlined, tone: 'red', route: { name: 'BorrowHistory', query: { status: 'OVERDUE' } }
  }
])

const teacherTasks = computed(() => [
  Number(teacherSummary.value.pendingApprovals || 0) > 0 && {
    key: 'approvals', title: 'Duyệt yêu cầu bảo lãnh',
    description: 'Sinh viên đang chờ quyết định của bạn.', value: teacherSummary.value.pendingApprovals,
    icon: FileSearchOutlined, tone: 'coral', route: { name: 'TeacherApproval' }
  },
  Number(stats.value.overdueBorrowRecords || 0) > 0 && {
    key: 'overdue', title: 'Xử lý thiết bị quá hạn',
    description: 'Kiểm tra và hoàn tất thủ tục trả thiết bị.', value: stats.value.overdueBorrowRecords,
    icon: WarningOutlined, tone: 'red', route: { name: 'BorrowHistory', query: { status: 'OVERDUE' } }
  },
  Number(teacherSummary.value.pendingOwnRequests || 0) > 0 && {
    key: 'pending', title: 'Theo dõi phiếu mượn',
    description: 'Phiếu của bạn đang chờ các bước phê duyệt hoặc bàn giao.', value: teacherSummary.value.pendingOwnRequests,
    icon: ClockCircleOutlined, tone: 'amber', route: { name: 'BorrowHistory' }
  }
].filter(Boolean))

const teacherActivities = computed(() => stats.value.activities.slice(0, 5))
const teacherQuickActions = computed(() => [
  { key: 'approval', label: 'Duyệt bảo lãnh', description: 'Xử lý yêu cầu sinh viên', icon: FileSearchOutlined, route: { name: 'TeacherApproval' } },
  { key: 'equipment', label: 'Mượn thiết bị', description: 'Xem tài sản đang sẵn sàng', icon: AppstoreOutlined, route: { name: 'Devices' } },
  { key: 'history', label: 'Lịch sử mượn', description: 'Theo dõi phiếu của bạn', icon: ClockCircleOutlined, route: { name: 'BorrowHistory' } },
  { key: 'consumable', label: 'Yêu cầu vật tư', description: 'Đăng ký và theo dõi cấp phát', icon: PlusOutlined, route: { name: 'ConsumableRequests' } }
])

const teacherReturnParts = computed(() => {
  if (!teacherSummary.value.nextReturnDate) return { day: '', month: '' }
  const parts = new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', timeZone: 'Asia/Ho_Chi_Minh'
  }).formatToParts(new Date(teacherSummary.value.nextReturnDate))
  const getPart = type => parts.find(part => part.type === type)?.value || ''
  return { day: getPart('day'), month: `Tháng ${getPart('month')}` }
})
const teacherReturnDay = computed(() => teacherReturnParts.value.day)
const teacherReturnMonth = computed(() => teacherReturnParts.value.month)
const teacherReturnState = computed(() => {
  if (!teacherSummary.value.nextReturnDate) return ''
  const milliseconds = new Date(teacherSummary.value.nextReturnDate).getTime() - Date.now()
  const days = Math.ceil(milliseconds / 86400000)
  if (days < 0) return `Đã quá hạn ${Math.abs(days)} ngày`
  if (days === 0) return 'Đến hạn hôm nay'
  return `Còn ${days} ngày để hoàn trả`
})

const studentSummary = computed(() => stats.value.studentSummary || {})
const studentDisplayName = computed(() => authStore.user?.fullName?.trim() || 'Sinh viên')
const studentStats = computed(() => [
  {
    key: 'pending',
    label: 'Phiếu đang xử lý',
    value: formatNumber(studentSummary.value.pendingRequests),
    hint: 'Chờ duyệt hoặc bảo lãnh',
    icon: FileSearchOutlined,
    tone: 'coral',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'approved',
    label: 'Chờ bàn giao',
    value: formatNumber(studentSummary.value.approvedRequests),
    hint: 'Đã duyệt, chưa nhận thiết bị',
    icon: ClockCircleOutlined,
    tone: 'amber',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'active',
    label: 'Đang mượn',
    value: formatNumber(studentSummary.value.activeBorrows),
    hint: 'Thiết bị thuộc trách nhiệm của bạn',
    icon: AppstoreOutlined,
    tone: 'blue',
    route: { name: 'BorrowHistory' }
  },
  {
    key: 'returned',
    label: 'Đã hoàn tất',
    value: formatNumber(studentSummary.value.returnedBorrows),
    hint: 'Lịch sử đã trả',
    icon: CheckCircleOutlined,
    tone: 'green',
    route: { name: 'BorrowHistory' }
  }
])

const studentStatusRows = computed(() => [
  { key: 'pending', label: 'Chờ xử lý', value: Number(studentSummary.value.statusCounts?.pending || 0) },
  { key: 'approved', label: 'Chờ bàn giao', value: Number(studentSummary.value.statusCounts?.approved || 0) },
  { key: 'active', label: 'Đang mượn', value: Number(studentSummary.value.statusCounts?.active || 0) },
  { key: 'returned', label: 'Đã trả', value: Number(studentSummary.value.statusCounts?.returned || 0) },
  { key: 'rejected', label: 'Từ chối', value: Number(studentSummary.value.statusCounts?.rejected || 0) },
  { key: 'cancelled', label: 'Đã hủy', value: Number(studentSummary.value.statusCounts?.cancelled || 0) },
  { key: 'expired', label: 'Hết hạn', value: Number(studentSummary.value.statusCounts?.expired || 0) }
])
const hasStudentStatusData = computed(() => studentStatusRows.value.some(item => item.value > 0))
const studentDonutSeries = computed(() => studentStatusRows.value.map(item => item.value))
const studentDonutOptions = computed(() => ({
  chart: { type: 'donut', toolbar: { show: false }, fontFamily: 'inherit' },
  labels: studentStatusRows.value.map(item => item.label),
  colors: ['#DF7657', '#F2B24B', '#4D91D8', '#7FBD68', '#E35F4E', '#94A3B8', '#8B5CF6'],
  legend: { position: 'bottom' },
  stroke: { width: 3, colors: ['#fff'] },
  dataLabels: { enabled: false },
  tooltip: { y: { formatter: value => formatNumber(value) } }
}))

const studentReturnParts = computed(() => {
  if (!studentSummary.value.nextReturnDate) return { day: '', month: '' }
  const parts = new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', timeZone: 'Asia/Ho_Chi_Minh'
  }).formatToParts(new Date(studentSummary.value.nextReturnDate))
  const getPart = type => parts.find(part => part.type === type)?.value || ''
  return { day: getPart('day'), month: `Tháng ${getPart('month')}` }
})
const studentReturnDay = computed(() => studentReturnParts.value.day)
const studentReturnMonth = computed(() => studentReturnParts.value.month)
const studentReturnState = computed(() => {
  if (!studentSummary.value.nextReturnDate) return ''
  const milliseconds = new Date(studentSummary.value.nextReturnDate).getTime() - Date.now()
  const days = Math.ceil(milliseconds / 86400000)
  if (days < 0) return `Đã quá hạn ${Math.abs(days)} ngày`
  if (days === 0) return 'Đến hạn hôm nay'
  return `Còn ${days} ngày để hoàn trả`
})

const navigateTo = route => router.push(route)

const handleAlertClick = alert => {
  const target = getDashboardAlertTarget(alert?.type)
  if (target) navigateTo(target)
}

const getAlertIcon = level => level === 'info' ? ClockCircleOutlined : WarningOutlined

const countFromAlert = (alerts, type) => {
  const matchingAlerts = alerts.filter(alert => alert?.type === type)
  if (!matchingAlerts.length) return 0
  const countInMessage = Number(String(matchingAlerts[0]?.message || '').match(/\d+/)?.[0])
  return Number.isFinite(countInMessage) ? countInMessage : matchingAlerts.length
}

const normalizeDashboardStats = result => {
  const payload = result || {}
  const alerts = Array.isArray(payload.alerts) ? payload.alerts : []
  const advanced = payload.advanced || {}
  const lowStockItems = Array.isArray(advanced.lowStockConsumables) ? advanced.lowStockConsumables : []

  return {
    ...stats.value,
    ...payload,
    counts: { ...stats.value.counts, ...(payload.counts || {}) },
    activities: Array.isArray(payload.activities) ? payload.activities : [],
    alerts,
    advanced: { ...stats.value.advanced, ...advanced },
    teacherSummary: { ...stats.value.teacherSummary, ...(payload.teacherSummary || {}) },
    studentSummary: {
      ...stats.value.studentSummary,
      ...(payload.studentSummary || {}),
      statusCounts: {
        ...stats.value.studentSummary.statusCounts,
        ...(payload.studentSummary?.statusCounts || {})
      }
    },
    pendingBorrowRequests: Number(payload.pendingBorrowRequests ?? countFromAlert(alerts, 'pending-borrow-requests')),
    pendingConsumableRequests: Number(payload.pendingConsumableRequests ?? countFromAlert(alerts, 'pending-consumable-requests')),
    borrowRequestsToProcess: Number(payload.borrowRequestsToProcess ?? payload.pendingBorrowRequests ?? 0),
    consumableRequestsToProcess: Number(payload.consumableRequestsToProcess ?? payload.pendingConsumableRequests ?? 0),
    overdueBorrowRecords: Number(payload.overdueBorrowRecords ?? alerts.filter(alert => alert?.type === 'overdue').length),
    lowStockConsumables: Number(payload.lowStockConsumables ?? lowStockItems.length),
    warrantyExpiringSoon: Number(payload.warrantyExpiringSoon ?? alerts.filter(alert => alert?.type === 'warranty-soon').length),
    maintenanceInProgress: Number(payload.maintenanceInProgress ?? payload.counts?.maintenance ?? 0)
  }
}

const refreshStats = async (forceRefresh = false) => {
  refreshing.value = true
  try {
    const result = await dashboardApi.getStats(forceRefresh)
    stats.value = normalizeDashboardStats(result)
  } catch (error) {
    message.error(getApiErrorMessage(error, 'Không tải được dữ liệu tổng quan.'))
  } finally {
    refreshing.value = false
    initialLoading.value = false
  }
}

onMounted(() => refreshStats(false))
</script>

<style scoped>
.overview-container { max-width: 1600px; padding: 4px 0 24px; }
.dashboard-loading-shell { display: grid; gap: 18px; }
.dashboard-loading-kpis { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.dashboard-loading-kpis :deep(.ant-skeleton-button) { height: 112px; border-radius: 14px; }
.dashboard-loading-panels { display: grid; grid-template-columns: minmax(0, 1.7fr) minmax(300px, 0.9fr); gap: 18px; }
.dashboard-loading-panels :deep(.ant-card) { min-height: 270px; border: 1px solid #e7eaee; border-radius: 14px; }
.manager-header, .panel-title-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.manager-header { align-items: center; margin-bottom: 24px; }
.manager-header h2, .header h2 { margin: 0; color: #10233f; font-family: var(--font-serif); font-size: 36px; font-weight: 650; line-height: 1.15; letter-spacing: -0.025em; }
.subtitle { margin: 8px 0 0; color: #64748b; font-size: 16px; line-height: 1.5; }
.manager-header-actions { display: flex; flex-direction: column; align-items: flex-end; gap: 6px; }
.manager-header-actions :deep(.ant-btn) { height: 42px; padding-inline: 18px; border-color: #dfe4e8; border-radius: 9px; color: #10233f; font-size: 15px; }
.manager-header-actions :deep(.ant-btn:hover), .manager-header-actions :deep(.ant-btn:focus) { border-color: #df7657; color: #df7657; }
.manager-section { margin-top: 20px; }
.manager-kpi-grid, .quick-actions-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
.manager-kpi-card { display: flex; align-items: center; min-width: 0; gap: 16px; min-height: 112px; padding: 22px 24px; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; box-shadow: 0 5px 18px rgba(16, 35, 63, .055); }
.manager-kpi-icon, .manager-attention-icon { display: inline-flex; align-items: center; justify-content: center; flex: 0 0 auto; border-radius: 50%; }
.manager-kpi-icon { width: 54px; height: 54px; font-size: 24px; }
.manager-kpi-copy { display: flex; min-width: 0; flex-direction: column; gap: 7px; }
.manager-kpi-label { overflow: hidden; color: #526276; font-size: 15px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.manager-kpi-copy strong { color: #10233f; font-size: 36px; line-height: 1; letter-spacing: -.02em; }
.tone-primary { color: #2376c5; background: #eaf4ff; }
.tone-success { color: #4d9b3b; background: #edf8e9; }
.tone-warning { color: #d98b18; background: #fff6e5; }
.tone-danger { color: #d84c43; background: #fff0ee; }
.tone-info { color: #4d91d8; background: #edf5ff; }
.manager-two-column { display: grid; gap: 20px; }
.manager-primary-grid { grid-template-columns: minmax(0, 1.6fr) minmax(340px, .9fr); }
.manager-secondary-grid { grid-template-columns: minmax(0, 1.25fr) minmax(380px, 1fr); }
.manager-panel { min-width: 0; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; box-shadow: 0 5px 18px rgba(16, 35, 63, .055); }
.manager-panel :deep(.ant-card-head) { min-height: 58px; padding: 0 22px; border-bottom: 1px solid #eef1f3; }
.manager-panel :deep(.ant-card-head-title) { padding: 17px 0; color: #10233f; font-size: 18px; font-weight: 700; }
.manager-panel :deep(.ant-card-body) { padding: 20px 22px; }
.manager-chart-panel :deep(.ant-card-body) { min-height: 230px; }
.manager-donut-layout { display: grid; grid-template-columns: minmax(220px, 1.15fr) minmax(180px, 1fr); align-items: center; gap: 16px; }
.manager-status-legend { display: flex; flex-direction: column; gap: 16px; padding-right: 12px; }
.manager-status-legend-item { display: grid; grid-template-columns: 10px 1fr auto; align-items: center; gap: 11px; color: #526276; font-size: 15px; }
.manager-status-legend-item strong { color: #10233f; font-size: 17px; }
.status-dot { width: 10px; height: 10px; flex: 0 0 10px; border-radius: 50%; }
.status-dot--success { background: #7fbd68; }
.status-dot--info { background: #4d91d8; }
.status-dot--warning { background: #f2b24b; }
.status-dot--danger { background: #e35f4e; }
.status-dot--purple { background: #8b5cf6; }
.manager-attention-list { display: flex; flex-direction: column; gap: 12px; }
.manager-attention-item { display: grid; grid-template-columns: 42px minmax(0, 1fr) auto 34px; align-items: center; gap: 12px; min-height: 58px; padding: 9px 10px; border: 1px solid #e9edf0; border-radius: 10px; }
.manager-attention-icon { width: 42px; height: 42px; border-radius: 10px; font-size: 19px; }
.manager-attention-copy { display: flex; min-width: 0; flex-direction: column; gap: 2px; }
.manager-attention-copy strong { overflow: hidden; color: #10233f; font-size: 15px; text-overflow: ellipsis; white-space: nowrap; }
.manager-attention-value { color: #10233f; font-size: 22px; }
.manager-attention-action { width: 34px; height: 34px; padding: 0; border-radius: 8px; color: #2d82c8; font-size: 14px; }
.manager-attention-action:hover { background: #edf5ff; color: #2376c5; }
.panel-title-row { align-items: center; }
.panel-title-row .ant-btn { padding-inline: 0; color: #df7657; font-size: 14px; }
.activity-list { display: flex; flex-direction: column; gap: 16px; }
.activity-item { display: flex; align-items: flex-start; gap: 12px; min-width: 0; }
.activity-icon { display: inline-flex; align-items: center; justify-content: center; width: 36px; height: 36px; margin-top: 1px; flex: 0 0 36px; border-radius: 9px; background: #f8fafc; font-size: 16px; }
.activity-icon--green { color: #4d9b3b; background: #edf8e9; }
.activity-icon--blue, .activity-icon--info { color: #4d91d8; background: #edf5ff; }
.activity-icon--orange, .activity-icon--warning { color: #d98b18; background: #fff6e5; }
.activity-icon--red, .activity-icon--error { color: #d84c43; background: #fff0ee; }
.activity-icon--purple { color: #7652b6; background: #f3effc; }
.activity-copy { min-width: 0; }
.activity-copy p { display: -webkit-box; margin: 0; overflow: hidden; color: #334155; font-size: 14px; line-height: 1.45; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.activity-meta { display: block; margin-top: 4px; overflow: hidden; color: #94a3b8; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.manager-section-title { margin: 0 0 12px; color: #10233f; font-size: 18px; font-weight: 700; }
.quick-action-button { display: flex; align-items: center; justify-content: flex-start; width: 100%; height: 52px; padding-inline: 16px; border-color: #e4e8ec; border-radius: 10px; color: #10233f; font-size: 15px; text-align: left; }
.quick-action-button:hover, .quick-action-button:focus { border-color: #df7657; color: #df7657; }
.quick-action-button > span:not(.ant-btn-icon) { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.quick-action-arrow { margin-left: auto; color: #94a3b8; font-size: 11px; }
.teacher-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; margin-bottom: 22px; }
.teacher-header h2 { margin: 8px 0 0; color: #10233f; font-family: var(--font-serif); font-size: 34px; font-weight: 650; line-height: 1.15; letter-spacing: -.025em; }
.teacher-eyebrow { display: inline-flex; align-items: center; gap: 7px; color: #c85f42; font-size: 13px; font-weight: 700; letter-spacing: .045em; text-transform: uppercase; }
.teacher-eyebrow::before { width: 7px; height: 7px; border-radius: 50%; background: #df7657; content: ''; }
.teacher-refresh-button { height: 40px; border-color: #dfe4e8; border-radius: 9px; color: #334155; }
.teacher-kpi-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.teacher-kpi-card { position: relative; display: grid; grid-template-columns: 46px minmax(0, 1fr) 16px; align-items: center; min-width: 0; min-height: 116px; gap: 13px; padding: 18px; overflow: hidden; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; color: inherit; font: inherit; text-align: left; cursor: pointer; transition: border-color .2s ease, transform .2s ease, box-shadow .2s ease; }
.teacher-kpi-card:hover, .teacher-kpi-card:focus-visible { border-color: #d7b3a7; box-shadow: 0 8px 22px rgba(16, 35, 63, .075); outline: none; transform: translateY(-2px); }
.teacher-kpi-icon { display: inline-flex; align-items: center; justify-content: center; width: 46px; height: 46px; border-radius: 12px; font-size: 20px; }
.teacher-kpi-copy { display: flex; min-width: 0; flex-direction: column; }
.teacher-kpi-copy small { overflow: hidden; color: #526276; font-size: 13px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.teacher-kpi-copy strong { margin-top: 3px; color: #10233f; font-size: 29px; line-height: 1.05; }
.teacher-kpi-copy span { margin-top: 5px; overflow: hidden; color: #94a3b8; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.teacher-kpi-arrow { color: #b3bdc8; font-size: 12px; }
.teacher-tone-coral { color: #d26548; background: #fff1ec; }
.teacher-tone-amber { color: #c77a0a; background: #fff7e7; }
.teacher-tone-blue { color: #347fc1; background: #edf6ff; }
.teacher-tone-red { color: #d34a43; background: #fff0ef; }
.teacher-main-grid { display: grid; grid-template-columns: minmax(0, 1.6fr) minmax(300px, .8fr); gap: 16px; margin-top: 16px; }
.teacher-panel { min-width: 0; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; box-shadow: none; }
.teacher-panel :deep(.ant-card-head) { min-height: 56px; padding: 0 20px; border-bottom: 1px solid #eef1f3; }
.teacher-panel :deep(.ant-card-head-title) { padding: 16px 0; color: #10233f; font-size: 17px; font-weight: 700; }
.teacher-panel :deep(.ant-card-body) { padding: 18px 20px; }
.teacher-panel-title { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.teacher-panel-title > small { color: #94a3b8; font-size: 12px; font-weight: 500; }
.teacher-panel-title :deep(.ant-btn) { height: auto; padding: 0; color: #d26548; }
.teacher-task-list { display: flex; flex-direction: column; gap: 9px; }
.teacher-task-item { display: grid; grid-template-columns: 42px minmax(0, 1fr) auto 14px; align-items: center; width: 100%; gap: 12px; padding: 10px 12px; border: 1px solid #edf0f2; border-radius: 11px; background: #fff; color: inherit; font: inherit; text-align: left; cursor: pointer; }
.teacher-task-item:hover, .teacher-task-item:focus-visible { border-color: #dfc0b5; background: #fffcfb; outline: none; }
.teacher-task-icon { display: inline-flex; align-items: center; justify-content: center; width: 42px; height: 42px; border-radius: 10px; font-size: 18px; }
.teacher-task-copy { display: flex; min-width: 0; flex-direction: column; gap: 3px; }
.teacher-task-copy strong { color: #20334d; font-size: 14px; }
.teacher-task-copy small { overflow: hidden; color: #7c8999; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.teacher-task-count { min-width: 28px; color: #10233f; font-size: 20px; font-weight: 750; text-align: right; }
.teacher-task-arrow { color: #a8b2bd; font-size: 11px; }
.teacher-clear-state { display: flex; align-items: center; min-height: 124px; gap: 15px; padding: 18px; border: 1px dashed #cfe4ca; border-radius: 12px; background: #f7fbf5; }
.teacher-clear-state > span { display: inline-flex; align-items: center; justify-content: center; width: 46px; height: 46px; flex: 0 0 46px; border-radius: 50%; background: #e7f6e3; color: #4d9b3b; font-size: 22px; }
.teacher-clear-state strong { color: #24452a; }
.teacher-clear-state p { margin: 3px 0 0; color: #6d806f; font-size: 13px; }
.teacher-return-panel :deep(.ant-card-body) { height: calc(100% - 57px); }
.teacher-return-content { display: grid; grid-template-columns: 70px minmax(0, 1fr); align-items: center; gap: 14px; }
.teacher-return-date { display: flex; align-items: center; justify-content: center; width: 70px; height: 76px; flex-direction: column; border-radius: 13px; background: #fff1ec; color: #c85f42; }
.teacher-return-date span { font-size: 28px; font-weight: 750; line-height: 1; }
.teacher-return-date small { margin-top: 7px; font-size: 12px; font-weight: 650; }
.teacher-return-copy { display: flex; min-width: 0; flex-direction: column; gap: 4px; }
.teacher-return-copy > span { color: #94a3b8; font-size: 12px; }
.teacher-return-copy strong { display: -webkit-box; overflow: hidden; color: #20334d; font-size: 15px; line-height: 1.35; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.teacher-return-copy small { color: #c56b24; font-size: 12px; }
.teacher-return-content :deep(.ant-btn) { grid-column: 1 / -1; height: 38px; margin-top: 4px; border-color: #e1e5e9; border-radius: 8px; }
.teacher-no-return { display: flex; align-items: center; min-height: 180px; flex-direction: column; justify-content: center; text-align: center; }
.teacher-no-return > span { display: inline-flex; align-items: center; justify-content: center; width: 48px; height: 48px; margin-bottom: 10px; border-radius: 50%; background: #edf8e9; color: #4d9b3b; font-size: 22px; }
.teacher-no-return strong { color: #20334d; }
.teacher-no-return p { max-width: 270px; margin: 5px 0 13px; color: #7c8999; font-size: 13px; line-height: 1.45; }
.teacher-no-return :deep(.ant-btn) { border-radius: 8px; }
.teacher-activity-panel { margin-top: 16px; }
.teacher-activity-list { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 28px; }
.teacher-activity-item { display: grid; grid-template-columns: 10px minmax(0, 1fr); gap: 10px; min-width: 0; padding: 11px 0; border-bottom: 1px solid #f0f2f4; }
.teacher-activity-item:nth-last-child(-n+2) { border-bottom: 0; }
.teacher-activity-dot { width: 8px; height: 8px; margin-top: 6px; border-radius: 50%; background: #4d91d8; }
.activity-dot-orange, .activity-dot-warning { background: #e5a12f; }
.activity-dot-red, .activity-dot-error { background: #d95048; }
.activity-dot-green { background: #63a850; }
.activity-dot-purple { background: #8b5cf6; }
.teacher-activity-item p { display: -webkit-box; margin: 0; overflow: hidden; color: #334155; font-size: 13px; line-height: 1.45; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.teacher-activity-item small { display: block; margin-top: 4px; color: #9aa5b1; font-size: 11px; }
.teacher-inline-empty { padding: 26px 0; color: #94a3b8; text-align: center; }
.teacher-quick-section { margin-top: 18px; }
.teacher-quick-section h3 { margin: 0 0 11px; color: #10233f; font-size: 17px; }
.teacher-quick-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.teacher-quick-action { display: grid; grid-template-columns: 38px minmax(0, 1fr) 12px; grid-template-rows: auto auto; align-items: center; min-width: 0; gap: 1px 10px; padding: 13px 14px; border: 1px solid #e4e8ec; border-radius: 11px; background: #fff; color: inherit; font: inherit; text-align: left; cursor: pointer; }
.teacher-quick-action:hover, .teacher-quick-action:focus-visible { border-color: #dfb8aa; outline: none; }
.teacher-quick-action > span { display: inline-flex; grid-row: 1 / 3; align-items: center; justify-content: center; width: 38px; height: 38px; border-radius: 9px; background: #f5f7f9; color: #d26548; font-size: 17px; }
.teacher-quick-action strong { overflow: hidden; color: #26384f; font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.teacher-quick-action small { overflow: hidden; color: #94a3b8; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.teacher-quick-action > .anticon { grid-column: 3; grid-row: 1 / 3; color: #aeb7c1; font-size: 10px; }
.student-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; margin-bottom: 22px; }
.student-header h2 { margin: 8px 0 0; color: #10233f; font-family: var(--font-serif); font-size: 34px; font-weight: 650; line-height: 1.15; letter-spacing: -.025em; }
.student-eyebrow { color: #d26548; font-size: 13px; font-weight: 700; letter-spacing: .04em; text-transform: uppercase; }
.student-refresh-button { height: 42px; border-color: #dfe4e8; border-radius: 9px; color: #10233f; }
.student-refresh-button:hover, .student-refresh-button:focus { border-color: #df7657; color: #df7657; }
.student-kpi-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.student-kpi-card { display: grid; grid-template-columns: 48px minmax(0, 1fr) 12px; align-items: center; min-width: 0; gap: 13px; min-height: 108px; padding: 17px; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; color: inherit; font: inherit; text-align: left; cursor: pointer; transition: border-color .2s ease, transform .2s ease; }
.student-kpi-card:hover, .student-kpi-card:focus-visible { border-color: #dfb8aa; outline: none; transform: translateY(-1px); }
.student-kpi-icon { display: inline-flex; align-items: center; justify-content: center; width: 48px; height: 48px; border-radius: 12px; font-size: 20px; }
.student-kpi-copy { display: flex; min-width: 0; flex-direction: column; gap: 4px; }
.student-kpi-copy small { overflow: hidden; color: #526276; font-size: 13px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.student-kpi-copy strong { color: #10233f; font-size: 28px; line-height: 1; }
.student-kpi-copy span { overflow: hidden; color: #94a3b8; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.student-kpi-arrow { color: #a8b2bd; font-size: 11px; }
.student-tone-coral .student-kpi-icon { color: #d26548; background: #fff1ec; }
.student-tone-amber .student-kpi-icon { color: #d98b18; background: #fff6e5; }
.student-tone-blue .student-kpi-icon { color: #4d91d8; background: #edf5ff; }
.student-tone-green .student-kpi-icon { color: #4d9b3b; background: #edf8e9; }
.student-main-grid, .student-bottom-grid { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(320px, .9fr); gap: 18px; margin-top: 18px; }
.student-panel { min-width: 0; border: 1px solid #e4e8ec; border-radius: 14px; background: #fff; box-shadow: none; }
.student-panel :deep(.ant-card-head) { min-height: 56px; padding: 0 20px; border-bottom: 1px solid #eef1f3; }
.student-panel :deep(.ant-card-head-title) { padding: 16px 0; color: #10233f; font-size: 17px; font-weight: 700; }
.student-panel :deep(.ant-card-body) { padding: 18px 20px; }
.student-panel-title { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.student-panel-title :deep(.ant-btn) { height: auto; padding: 0; color: #d26548; }
.student-return-content { display: grid; grid-template-columns: 70px minmax(0, 1fr); align-items: center; gap: 14px; }
.student-return-date { display: flex; align-items: center; justify-content: center; width: 70px; height: 76px; flex-direction: column; border-radius: 13px; background: #fff1ec; color: #c85f42; }
.student-return-date span { font-size: 28px; font-weight: 750; line-height: 1; }
.student-return-date small { margin-top: 7px; font-size: 12px; font-weight: 650; }
.student-return-copy { display: flex; min-width: 0; flex-direction: column; gap: 4px; }
.student-return-copy > span { color: #94a3b8; font-size: 12px; }
.student-return-copy strong { display: -webkit-box; overflow: hidden; color: #20334d; font-size: 15px; line-height: 1.35; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.student-return-copy small { color: #c56b24; font-size: 12px; }
.student-return-content :deep(.ant-btn) { grid-column: 1 / -1; height: 38px; margin-top: 4px; border-color: #e1e5e9; border-radius: 8px; }
.student-no-return { display: flex; align-items: center; min-height: 180px; flex-direction: column; justify-content: center; text-align: center; }
.student-no-return > span { display: inline-flex; align-items: center; justify-content: center; width: 48px; height: 48px; margin-bottom: 10px; border-radius: 50%; background: #edf8e9; color: #4d9b3b; font-size: 22px; }
.student-no-return strong { color: #20334d; }
.student-no-return p { max-width: 270px; margin: 5px 0 13px; color: #7c8999; font-size: 13px; line-height: 1.45; }
.student-no-return :deep(.ant-btn) { border-radius: 8px; }
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
  .manager-kpi-grid, .quick-actions-grid, .teacher-kpi-grid, .teacher-quick-grid, .student-kpi-grid, .dashboard-loading-kpis { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .manager-two-column, .teacher-main-grid, .dashboard-loading-panels { grid-template-columns: 1fr; }
  .student-main-grid, .student-bottom-grid { grid-template-columns: 1fr; }
}
@media (max-width: 767px) {
  .manager-header, .teacher-header, .student-header { align-items: flex-start; flex-direction: column; }
  .manager-header-actions { align-items: flex-start; }
  .manager-kpi-grid, .quick-actions-grid, .borrower-content-grid, .teacher-kpi-grid, .teacher-quick-grid, .student-kpi-grid, .teacher-activity-list, .dashboard-loading-kpis { grid-template-columns: 1fr; }
  .manager-donut-layout { grid-template-columns: 1fr; }
  .manager-status-legend { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); padding: 0 10px 7px; }
  .manager-attention-item { grid-template-columns: 30px minmax(0, 1fr) auto auto; }
  .borrower-status-panel { grid-column: auto; }
  .manager-header h2, .teacher-header h2, .student-header h2, .header h2 { font-size: 30px; }
  .subtitle { font-size: 15px; }
  .manager-kpi-card { min-height: 96px; padding: 18px; }
  .manager-kpi-copy strong { font-size: 32px; }
  .manager-attention-item { grid-template-columns: 42px minmax(0, 1fr) auto 34px; }
  .teacher-refresh-button { width: 100%; }
  .student-refresh-button { width: 100%; }
  .teacher-kpi-card { min-height: 104px; }
  .teacher-activity-item:nth-last-child(2) { border-bottom: 1px solid #f0f2f4; }
}
</style>
