<template>
  <a-layout class="dashboard-shell">
    <!-- Thanh Menu Bên Trái (Sider) -->
    <a-layout-sider 
      v-model:collapsed="collapsed" 
      :collapsible="false"
      theme="light" 
      width="270"
      breakpoint="lg"
      :collapsed-width="0"
      :trigger="null"
      class="ladi-sider"
    >
      <div class="logo">
        <div class="logo-icon"><experiment-outlined /></div>
        <span class="logo-text" v-if="!collapsed">LabManagement</span>
      </div>

      <div data-testid="sidebar-menu-scroll" class="sidebar-menu-scroll">
        <a-menu
          :selectedKeys="[selectedKey]"
          theme="light"
          mode="inline"
          class="ladi-menu"
        >
          <a-menu-item key="0" data-testid="menu-overview" @click="$router.push({ name: 'Overview' })">
            <appstore-filled /><span>{{ $t('menu.overview') }}</span>
          </a-menu-item>

          <a-menu-item-group title="Quản lý tài sản">
            <a-menu-item key="1" data-testid="menu-devices" @click="$router.push({ name: 'Devices' })">
              <desktop-outlined /><span>{{ $t('menu.devices') }}</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="m_location" @click="$router.push({ name: 'Locations' })">
              <environment-outlined /><span>Vị trí</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="m_inventory" @click="$router.push({ name: 'Inventory' })">
              <scan-outlined /><span>Kiểm kê</span>
            </a-menu-item>
          </a-menu-item-group>

          <a-menu-item-group title="Mượn và trả">
            <a-menu-item key="3" @click="$router.push({ name: 'BorrowHistory' })">
              <history-outlined /><span>{{ $t('menu.borrowHistory') }}</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="g1_1" @click="$router.push({ name: 'BorrowRequests' })">
              <solution-outlined /><span>Phiếu chờ duyệt</span>
            </a-menu-item>
            <a-menu-item v-if="isBorrowerRole(role)" key="g2_2" @click="$router.push({ name: 'ConsumableRequests' })">
              <history-outlined /><span>{{ $t('menu.studentConsumableHistory') }}</span>
            </a-menu-item>
          </a-menu-item-group>

          <a-menu-item-group title="Vận hành">
            <a-menu-item v-if="isManagerRole(role)" key="m3" @click="$router.push({ name: 'Maintenance' })">
              <tool-outlined /><span>{{ $t('menu.maintenanceHistory') }}</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="m_schedule" @click="$router.push({ name: 'MaintenanceSchedules' })">
              <calendar-outlined /><span>Lịch bảo trì</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="g1_2" @click="$router.push({ name: 'ConsumableRequests' })">
              <experiment-outlined /><span>Yêu cầu cấp phát</span>
            </a-menu-item>
            <a-menu-item key="m4" @click="$router.push({ name: 'Penalty' })">
              <pay-circle-outlined /><span>{{ $t('menu.penalty') }}</span>
            </a-menu-item>
            <a-menu-item v-if="isManagerRole(role)" key="m_reports" @click="$router.push({ name: 'Reports' })">
              <bar-chart-outlined /><span>Báo cáo</span>
            </a-menu-item>
            <a-menu-item v-if="isTeacherRole(role)" key="m_teacher" @click="$router.push({ name: 'TeacherApproval' })">
              <solution-outlined /><span>{{ $t('menu.teacherApproval') }}</span>
            </a-menu-item>
          </a-menu-item-group>

          <a-menu-item-group v-if="isAdminRole(role)" title="Quản trị hệ thống">
            <a-menu-item key="g1_3" data-testid="menu-admin-users" @click="$router.push({ name: 'AdminUsers' })">
              <team-outlined /><span>{{ $t('menu.userManagement') }}</span>
            </a-menu-item>
            <a-menu-item key="g1_4" @click="$router.push({ name: 'AuditLogs' })">
              <history-outlined /><span>{{ $t('menu.auditLogs') }}</span>
            </a-menu-item>
          </a-menu-item-group>
        </a-menu>
      </div>

      <div v-if="!collapsed" data-testid="sidebar-account-footer" class="sidebar-account-footer">
        <AccountMenu
          :display-name="accountDisplayName"
          :role="role"
          :avatar-url="accountAvatarUrl"
          :avatar-updated-at="accountAvatarUpdatedAt"
          placement="topRight"
          test-id="sidebar-account-menu-trigger"
          @profile="router.push({ name: 'Profile' })"
          @password="changePasswordVisible = true"
          @logout="handleLogout"
        >
          <template #trigger>
            <div class="sidebar-account-trigger">
              <UserAvatar
                :name="accountDisplayName"
                :avatar-url="accountAvatarUrl"
                :avatar-updated-at="accountAvatarUpdatedAt"
                :size="34"
              />
              <span class="sidebar-account-copy"><strong>{{ accountDisplayName }}</strong><small>{{ roleLabel(role) }}</small></span>
            </div>
          </template>
        </AccountMenu>
      </div>
    </a-layout-sider>

    <!-- Khu Vực Nội Dung Bên Phải -->
    <a-layout>
      <!-- Thanh Header -->
      <a-layout-header class="ladi-header">
        <div class="header-left">
          <a-tooltip :title="collapsed ? 'Mở menu' : 'Thu gọn menu'">
            <a-button
              type="text"
              class="sidebar-toggle"
              data-testid="sidebar-toggle"
              :aria-label="collapsed ? 'Mở menu' : 'Thu gọn menu'"
              @click="collapsed = !collapsed"
            >
              <template #icon>
                <menu-unfold-outlined v-if="collapsed" />
                <menu-fold-outlined v-else />
              </template>
            </a-button>
          </a-tooltip>
          <div class="workspace-selector">
            <span class="workspace-icon"><experiment-outlined /></span>
            <span class="workspace-name">{{ $t('header.workspace') }}</span>
          </div>
        </div>

        <div class="header-center">
          <div class="search-bar" @click="searchVisible = true">
            <search-outlined class="search-icon" />
            <span class="search-placeholder">{{ $t('header.search') }}</span>
            <div class="search-shortcut">{{ searchShortcut }}</div>
          </div>
        </div>

        <div class="header-right">
          <div class="action-icons">
            <!-- Dark mode removed -->

            <a-popover
              v-model:open="notificationOpen"
              trigger="click"
              placement="bottomRight"
              overlay-class-name="notification-overlay"
            >
              <template #content>
                <div class="notification-popover" data-testid="notification-popover">
                  <div class="notification-popover-header" data-testid="notification-popover-header">
                    <strong>Thông báo</strong>
                    <a-button v-if="notificationStore.hasUnread" type="link" size="small" @click.stop="markAllNotifications">Đánh dấu tất cả đã đọc</a-button>
                  </div>
                  <div class="notification-popover-list" data-testid="notification-popover-list">
                    <a-skeleton v-if="notificationStore.loading" active :paragraph="{ rows: 4 }" />
                    <a-alert v-else-if="notificationStore.error" type="error" show-icon :message="notificationStore.error">
                      <template #action><a-button size="small" @click="notificationStore.fetchRecent(true)">Thử lại</a-button></template>
                    </a-alert>
                    <a-list v-else-if="notificationStore.recentItems.length" :data-source="notificationStore.recentItems" size="small">
                    <template #renderItem="{ item }">
                      <a-list-item :class="['notification-item', { 'notification-item-unread': !item.isRead }]" tabindex="0" role="button" @click="openNotification(item)" @keydown.enter="openNotification(item)">
                        <template #extra><span class="notification-dot" :aria-hidden="item.isRead" /></template>
                        <a-list-item-meta>
                          <template #avatar><span class="notification-type-icon"><component :is="notificationIcon(item.type)" /></span></template>
                          <template #title>
                            <span class="notification-item-title">{{ item.title }}</span>
                            <a-tag class="notification-type-tag">{{ notificationTypeLabel(item.type) }}</a-tag>
                          </template>
                          <template #description>
                            <span class="notification-item-description">{{ item.message }}</span>
                            <span class="notification-item-time">{{ formatRelativeTime(item.createdAt) }}</span>
                          </template>
                        </a-list-item-meta>
                      </a-list-item>
                    </template>
                    </a-list>
                    <a-empty v-else description="Chưa có thông báo" :image-style="{ height: '40px' }" />
                  </div>
                  <div class="notification-popover-footer" data-testid="notification-popover-footer">
                    <a-button type="link" @click="goToNotifications">Xem tất cả thông báo</a-button>
                  </div>
                </div>
              </template>
              <NotificationBell :unread-count="notificationStore.unreadCount" @open="handleNotificationOpen" />
            </a-popover>
          </div>
          <AccountMenu
            :display-name="accountDisplayName"
            :role="role"
            :avatar-url="accountAvatarUrl"
            :avatar-updated-at="accountAvatarUpdatedAt"
            @profile="router.push({ name: 'Profile' })"
            @password="changePasswordVisible = true"
            @logout="handleLogout"
          >
            <template #trigger>
              <div class="user-profile" title="Tài khoản" data-testid="account-menu-trigger">
                <UserAvatar
                  :name="accountDisplayName"
                  :avatar-url="accountAvatarUrl"
                  :avatar-updated-at="accountAvatarUpdatedAt"
                  :size="38"
                />
              </div>
            </template>
          </AccountMenu>
        </div>
      </a-layout-header>

      <!-- Nội dung chính (Router View) -->
      <a-layout-content class="dashboard-content">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view> 
      </a-layout-content>
    </a-layout>

    <!-- Command Palette Modal -->
    <a-modal 
      v-model:open="searchVisible" 
      :footer="null" 
      :closable="false" 
      width="720px"
      :bodyStyle="{ padding: 0 }"
      wrapClassName="cmd-palette"
    >
      <div class="cmd-header">
        <search-outlined class="cmd-search-icon" />
        <input type="text" v-model="searchQuery" placeholder="Tìm thiết bị theo tên hoặc số seri" class="cmd-input" autofocus />
        <div class="cmd-scope">
          <desktop-outlined />
          <span>Thiết bị</span>
        </div>
        <a-tooltip title="Đóng tìm kiếm">
          <button type="button" class="cmd-close" aria-label="Đóng tìm kiếm" @click="searchVisible = false">
            <close-outlined />
          </button>
        </a-tooltip>
      </div>
      
      <div class="cmd-body">
        <div v-if="filteredSearchData.length === 0" class="empty-state">
          <div class="empty-icon-wrapper"><desktop-outlined /></div>
          <h3 v-if="searchQuery">Không tìm thấy thiết bị phù hợp</h3>
          <h3 v-else>Tìm kiếm thiết bị</h3>
          <p>Nhập tên thiết bị, số seri hoặc mã tài sản để tìm nhanh trong kho.</p>
          <div class="example-tags">
            <span class="tag-label">Ví dụ</span>
            <span class="tag" @click="searchQuery = 'Kính hiển vi'">Kính hiển vi</span>
            <span class="tag" @click="searchQuery = 'SP01'">SP01</span>
            <span class="tag" @click="searchQuery = 'Máy đo'">Máy đo</span>
          </div>
        </div>
        <div v-else class="search-results">
          <a-list item-layout="horizontal" :data-source="filteredSearchData">
            <template #renderItem="{ item }">
              <a-list-item class="search-result-item" @click="handleSelectSearchResult(item)" style="cursor: pointer;">
                <a-list-item-meta :description="item.serial + ' - ' + item.location">
                  <template #title>
                    <a>{{ item.name }}</a>
                  </template>
                  <template #avatar>
                    <a-avatar style="background-color: var(--color-primary);"><desktop-outlined style="color: white;"/></a-avatar>
                  </template>
                </a-list-item-meta>
                <div>{{ getEquipmentStatusLabel(item.status) }}</div>
              </a-list-item>
            </template>
          </a-list>
        </div>
      </div>
      
      <div class="cmd-footer">
        <div class="shortcut-hints">
          <span class="hint-key">↑</span> <span class="hint-key">↓</span> <span class="hint-text">Di chuyển</span>
          <span class="hint-key">↵</span> <span class="hint-text">Chọn</span>
          <span class="hint-key">esc</span> <span class="hint-text">Đóng</span>
        </div>
      </div>
    </a-modal>

    <a-modal
      v-model:open="changePasswordVisible"
      title="Đổi mật khẩu"
      okText="Đổi mật khẩu"
      cancelText="Hủy"
      :confirmLoading="changingPassword"
      @ok="submitChangePassword"
    >
      <a-form layout="vertical">
        <a-form-item label="Mật khẩu hiện tại" required>
          <a-input-password v-model:value="passwordForm.currentPassword" />
        </a-form-item>
        <a-form-item label="Mật khẩu mới (ít nhất 8 ký tự)" required>
          <a-input-password v-model:value="passwordForm.newPassword" />
        </a-form-item>
        <a-form-item label="Nhập lại mật khẩu mới" required>
          <a-input-password v-model:value="passwordForm.confirmPassword" />
        </a-form-item>
      </a-form>
    </a-modal>
  </a-layout>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { 
  DesktopOutlined, 
  LogoutOutlined, 
  AppstoreFilled, 
  SolutionOutlined, 
  ToolOutlined, 
  EnvironmentOutlined,
  HistoryOutlined,
  ExperimentOutlined,
  PayCircleOutlined,
  TeamOutlined,
  UserOutlined,
  SearchOutlined,
  CloseOutlined,
  BulbOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  ScanOutlined,
  CalendarOutlined,
  BarChartOutlined,
} from '@ant-design/icons-vue'
import { useAuthStore } from '../stores/authStore'
import { notification } from 'ant-design-vue'
import * as signalR from '@microsoft/signalr'
import { equipmentApi } from '../api/equipmentApi'
import { userApi } from '../api/userApi'
import { isAdminRole, isBorrowerRole, isManagerRole, isTeacherRole, roleLabel } from '../constants/business'
import { getEquipmentStatusLabel } from '../utils/statusLabels'
import NotificationBell from '../components/NotificationBell.vue'
import AccountMenu from '../components/AccountMenu.vue'
import UserAvatar from '../components/UserAvatar.vue'
import { useNotificationStore } from '../stores/notificationStore'
import { formatRelativeTime, notificationIcon, notificationTypeLabel } from '../utils/notificationUtils'

// Dark mode logic removed

const handleGlobalKeydown = (e) => {
  if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
    e.preventDefault()
    searchVisible.value = true
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalKeydown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
})

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const notificationStore = useNotificationStore()
const role = computed(() => authStore.role)
const collapsed = ref(false)
const searchShortcut = computed(() => {
  if (typeof navigator === 'undefined') return 'Ctrl K'
  const platform = navigator.userAgentData?.platform || navigator.platform || navigator.userAgent
  return /mac|iphone|ipad|ipod/i.test(platform) ? '⌘ K' : 'Ctrl K'
})
const routeMenuKeys = {
  Overview: '0',
  Devices: '1',
  BorrowHistory: '3',
  Profile: 'profile',
  TeacherApproval: 'm_teacher',
  Maintenance: 'm3',
  MaintenanceSchedules: 'm_schedule',
  Reports: 'm_reports',
  Locations: 'm_location',
  Inventory: 'm_inventory',
  Notifications: 'notifications',
  Penalty: 'm4',
  BorrowRequests: 'g1_1',
  AdminUsers: 'g1_3',
  AuditLogs: 'g1_4'
}
const selectedKey = computed(() => {
  if (route.name === 'ConsumableRequests') {
    return isManagerRole(role.value) ? 'g1_2' : 'g2_2'
  }
  return routeMenuKeys[route.name] || '0'
})
const searchVisible = ref(false)
const changePasswordVisible = ref(false)
const changingPassword = ref(false)
const passwordForm = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const searchQuery = ref('')
const searchData = ref([])
const notificationOpen = ref(false)
const accountProfile = computed(() => authStore.user || { username: '', fullName: '' })
const accountDisplayName = computed(() => accountProfile.value.fullName || accountProfile.value.username || 'Tài khoản')
const accountAvatarUrl = computed(() => accountProfile.value.hasAvatar ? userApi.avatarUrl() : '')
const accountAvatarUpdatedAt = computed(() => accountProfile.value.avatarUpdatedAt || '')

const loadAccountProfile = async () => {
  try {
    const profile = await userApi.getMe()
    if (profile) authStore.setUser(profile)
  } catch (error) {
    console.error('Lỗi tải thông tin tài khoản', error)
  }
}

const handleNotificationOpen = () => {
  notificationOpen.value = true
  notificationStore.fetchRecent().catch(() => {})
}

const goToNotifications = () => {
  notificationOpen.value = false
  router.push({ name: 'Notifications' })
}

const openNotification = async item => {
  try {
    await notificationStore.markRead(item.id)
  } catch (error) {
    notification.error({ message: 'Không thể cập nhật thông báo', description: error?.message || 'Vui lòng thử lại.' })
    return
  }
  notificationOpen.value = false
  if (typeof item.url === 'string' && item.url.startsWith('/')) router.push(item.url)
}

const markAllNotifications = async () => {
  try {
    await notificationStore.markAllRead()
  } catch (error) {
    notification.error({ message: 'Không thể cập nhật thông báo', description: error?.message || 'Vui lòng thử lại.' })
  }
}

const loadSearchData = async () => {
  try {
    const res = await equipmentApi.getAll()
    searchData.value = res || []
  } catch (error) {
    console.error("Lỗi lấy dữ liệu search", error)
  }
}

watch(searchVisible, (newVal) => {
  if (newVal) {
    searchQuery.value = ''
    loadSearchData()
  }
})

const filteredSearchData = computed(() => {
  if (!searchQuery.value) return []
  const query = searchQuery.value.toLowerCase()
  return searchData.value.filter(item => 
    (item.name && item.name.toLowerCase().includes(query)) || 
    (item.serial && item.serial.toLowerCase().includes(query))
  )
})

const handleSelectSearchResult = (item) => {
  searchVisible.value = false
  // Chuyển hướng hoặc xử lý item (Ví dụ: nhảy đến bảng thiết bị)
  router.push({ name: 'Devices' })
}

let hubConnection = null

onMounted(() => {
  notificationStore.fetchRecent().catch(() => {})
  loadAccountProfile()
  // Kết nối SignalR
  const signalRUrl = import.meta.env.VITE_SIGNALR_URL || '/notificationHub'
  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(signalRUrl, {
      accessTokenFactory: () => localStorage.getItem('token') || sessionStorage.getItem('token') || ''
    })
    .withAutomaticReconnect()
    .build()

  hubConnection.on('ReceiveNotification', (payload) => {
    if (notificationStore.handleRealtimeNotification(payload)) {
      notification.info({
        message: 'Thông báo mới',
        description: typeof payload === 'string' ? payload : payload?.message,
        placement: 'topRight',
        duration: 5
      })
    }
  })

  hubConnection.start()
    .then(() => console.log('SignalR connected'))
    .catch(err => console.error('SignalR connection error: ', err))
})

onUnmounted(() => {
  if (hubConnection) {
    hubConnection.stop()
  }
})

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

const submitChangePassword = async () => {
  const { currentPassword, newPassword, confirmPassword } = passwordForm.value
  if (!currentPassword || newPassword.length < 8) {
    notification.warning({
      message: 'Thông tin chưa hợp lệ',
      description: 'Mật khẩu mới phải có ít nhất 8 ký tự.'
    })
    return
  }
  if (newPassword !== confirmPassword) {
    notification.warning({
      message: 'Mật khẩu không khớp',
      description: 'Vui lòng nhập lại đúng mật khẩu mới.'
    })
    return
  }

  changingPassword.value = true
  try {
    await userApi.changePassword({ currentPassword, newPassword })
    changePasswordVisible.value = false
    passwordForm.value = { currentPassword: '', newPassword: '', confirmPassword: '' }
    authStore.logout()
    notification.success({
      message: 'Đổi mật khẩu thành công',
      description: 'Vui lòng đăng nhập lại bằng mật khẩu mới.'
    })
    router.push('/login')
  } catch (error) {
    notification.error({
      message: 'Không thể đổi mật khẩu',
      description: error?.response?.data?.message || 'Vui lòng kiểm tra mật khẩu hiện tại.'
    })
  } finally {
    changingPassword.value = false
  }
}
</script>

<style scoped>
/* Sidebar Styles */
.ladi-sider {
  background: var(--color-canvas-cream);
  border-right: 1px solid rgba(0,0,0,0.05);
}

.dashboard-shell {
  height: 100vh;
  overflow: hidden;
  background: var(--color-canvas-cream);
}

.ladi-sider :deep(.ant-layout-sider-children) {
  display: flex;
  flex-direction: column;
  min-height: 100%;
  overflow: visible;
}

.ladi-sider :deep(.ant-layout-sider-trigger) {
  display: none;
}
.logo {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 20px;
  border-bottom: 1px solid transparent; /* Hoặc border nếu muốn chia tách */
}
.logo-icon {
  width: 32px;
  height: 32px;
  background: var(--color-primary);
  color: white;
  border-radius: 8px;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 18px;
  margin-right: 12px;
}
.logo-text {
  font-weight: 800;
  font-size: 18px;
  color: #111827;
  letter-spacing: -0.5px;
}

.sidebar-menu-scroll {
  flex: 1 1 auto;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  scrollbar-width: thin;
}

/* Menu Customization to match LadiPage */
.ladi-menu {
  min-height: 100%;
  border-right: none;
  padding: 14px 10px;
  background: var(--color-canvas-cream);
}
.ladi-menu :deep(.ant-menu-item-group-title) {
  padding: 14px 12px 7px;
  color: #8a94a6;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 0.08em;
  line-height: 18px;
  text-transform: uppercase;
}
.ladi-menu :deep(.ant-menu-item) {
  border-radius: 10px;
  margin: 0 0 6px;
  height: 48px;
  line-height: 48px;
  color: var(--color-ink);
  font-weight: 500;
}
.ladi-menu :deep(.ant-menu-item:hover:not(.ant-menu-item-selected)) {
  background-color: rgba(0,0,0,0.03);
  color: var(--color-ink);
}
/* Selected Item Styling (The Blue LadiPage pill) */
.ladi-menu :deep(.ant-menu-item-selected) {
  background-color: var(--color-primary) !important;
  color: #ffffff !important;
  height: 52px;
  line-height: 52px;
}
.ladi-menu :deep(.ant-menu-item-selected .anticon) {
  color: #ffffff !important;
}
.ladi-menu :deep(.ant-menu-item .anticon) {
  font-size: 16px;
}

.sidebar-account-footer {
  flex: 0 0 auto;
  padding: 12px 14px 16px;
  border-top: 1px solid rgba(0, 0, 0, 0.06);
  background: var(--color-canvas-cream);
  overflow: visible;
}
.sidebar-account-footer :deep(.account-menu-trigger) {
  display: block;
  width: 100%;
}
.sidebar-account-trigger {
  display: flex;
  align-items: center;
  width: 100%;
  min-height: 64px;
  gap: 10px;
  padding: 10px 12px;
  box-sizing: border-box;
  border: 1px solid transparent;
  border-radius: 14px;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}
.sidebar-account-trigger:hover,
.sidebar-account-trigger:focus-visible {
  background: rgba(217, 119, 87, 0.08);
  border-color: var(--color-primary);
  outline: none;
}
.sidebar-account-copy { display: flex; min-width: 0; flex-direction: column; gap: 2px; }
.sidebar-account-copy strong,
.sidebar-account-copy small { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sidebar-account-copy strong { color: #111827; font-size: 13px; }
.sidebar-account-copy small { color: #6b7280; font-size: 11px; }
.account-menu-logout:hover {
  background-color: #fef2f2 !important;
  color: #dc2626 !important;
}

/* Header Styles */
.ladi-header {
  background: var(--color-canvas-cream);
  padding: 0 22px;
  height: 64px;
  line-height: normal;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 24px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
  position: relative;
  z-index: 2;
}

.ladi-header :deep(*) {
  border-left: 0;
  border-right: 0;
}

/* Header Left */
.header-left {
  display: flex;
  align-items: center;
  flex: 0 0 auto;
  min-width: 0;
  gap: 10px;
  border: 0;
}

.sidebar-toggle {
  display: none;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  align-items: center;
  justify-content: center;
  color: #4b5563;
  font-size: 17px;
}

.sidebar-toggle:hover,
.sidebar-toggle:focus {
  background: #f3f4f6;
  color: #111827;
}

.workspace-selector {
  display: flex;
  align-items: center;
  min-width: 0;
  gap: 10px;
  font-weight: 600;
  color: #1f2937;
  border: 0;
}

.workspace-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 36px;
  color: var(--color-primary);
  background: rgba(217, 119, 87, 0.1);
  font-size: 18px;
}

.workspace-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Header Center (Search) */
.header-center {
  flex: 1 1 auto;
  display: flex;
  justify-content: center;
  min-width: 220px;
  border: 0;
}

.search-bar {
  display: flex;
  align-items: center;
  background: #ffffff;
  border-radius: 10px;
  padding: 0 10px 0 14px;
  height: 38px;
  width: 100%;
  max-width: 480px;
  border: 1px solid rgba(0,0,0,0.1);
  cursor: pointer;
  transition: background 0.2s, border-color 0.2s, box-shadow 0.2s;
}

.search-bar:hover {
  background: #ffffff;
  border-color: rgba(0,0,0,0.2);
}

.search-bar:focus-within {
  background: #ffffff;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(217, 119, 87, 0.1);
}

.search-icon {
  color: #9ca3af;
  margin-right: 10px;
  font-size: 16px;
}

.search-placeholder {
  color: #9ca3af;
  flex: 1;
  text-align: left;
  font-size: 14px;
}

.search-shortcut {
  font-size: 12px;
  color: #6b7280;
  background: #ffffff;
  height: 28px;
  min-width: 52px;
  padding: 0 8px;
  border-radius: 6px;
  margin-left: 12px;
  border: 1px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  font-family: monospace;
}

/* Header Right */
.header-right {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  flex: 0 0 auto;
  min-width: 0;
  gap: 12px;
  margin-left: auto;
  border: 0;
}

.action-icons {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 18px;
  color: #4b5563;
}

.header-icon-btn {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #4b5563;
  font-size: 17px;
  transition: background 0.2s, color 0.2s;
}

.header-icon-btn:hover,
.header-icon-btn:focus {
  background: #f3f4f6;
  color: #111827;
}

.user-profile {
  width: 38px;
  height: 38px;
  cursor: pointer;
  flex: 0 0 38px;
}

.user-avatar {
  background-color: var(--color-primary);
  cursor: pointer;
  font-weight: 700;
}

.dashboard-content {
  margin: 0;
  padding: 24px 32px;
  background: var(--color-canvas-cream);
  height: calc(100vh - 64px);
  overflow-y: auto;
  min-height: 0;
}

:global(.notification-overlay) { max-width: calc(100vw - 24px); }
:global(.notification-overlay .ant-popover-inner) { padding: 0; overflow: hidden; border-radius: 14px; }
:global(.notification-overlay .ant-popover-inner-content) { padding: 0; }
:global(.notification-popover) {
  width: min(400px, calc(100vw - 24px));
  max-height: min(600px, calc(100vh - 24px));
  display: flex;
  flex-direction: column;
  color: var(--color-ink);
}
:global(.notification-popover-header),
:global(.notification-popover-footer) {
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  background: #fff;
}
:global(.notification-popover-header) { border-bottom: 1px solid var(--color-border); }
:global(.notification-popover-header strong) { font-size: 16px; }
:global(.notification-popover-footer) { justify-content: center; border-top: 1px solid var(--color-border); }
:global(.notification-popover-list) { flex: 1 1 auto; min-height: 0; overflow-y: auto; padding: 4px 8px; }
:global(.notification-popover-list .ant-list-item) { border: 0; }
:global(.notification-item) {
  position: relative;
  display: block !important;
  padding: 11px 10px !important;
  border-radius: 10px;
  cursor: pointer;
  transition: background-color .15s ease;
}
:global(.notification-item:hover),
:global(.notification-item:focus-visible) { background: rgba(217, 119, 87, 0.08); outline: none; }
:global(.notification-item-unread) { background: rgba(217, 119, 87, 0.08); }
:global(.notification-item .ant-list-item-meta) { min-width: 0; }
:global(.notification-item .ant-list-item-meta-title) { margin-bottom: 2px !important; }
:global(.notification-item .ant-list-item-meta-description) { color: var(--color-secondary); }
:global(.notification-type-icon) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 9px;
  color: var(--color-primary);
  background: rgba(217, 119, 87, 0.12);
}
:global(.notification-item-title) {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
}
:global(.notification-type-tag) { margin-inline-start: 6px; color: var(--color-primary); border-color: rgba(217, 119, 87, .25); background: transparent; }
:global(.notification-item-description) {
  display: block;
  display: -webkit-box;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  overflow-wrap: anywhere;
}
:global(.notification-item-time) { display: block; margin-top: 3px; font-size: 11px; color: var(--color-secondary); }
:global(.notification-dot) { display: block; width: 7px; height: 7px; border-radius: 50%; background: var(--color-primary); }
:global(.notification-item:not(.notification-item-unread) .notification-dot) { visibility: hidden; }

@media (max-width: 520px) {
  :global(.notification-overlay) { inset-inline: 12px !important; width: auto !important; }
  :global(.notification-overlay .ant-popover-arrow) { display: none; }
  :global(.notification-popover) { width: calc(100vw - 24px); max-height: calc(100vh - 24px); }
  :global(.notification-popover-header),
  :global(.notification-popover-footer) { padding: 12px; }
}
/* Command Palette Modal Styles */
:global(.cmd-palette .ant-modal-content) {
  padding: 0 !important;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
}
.cmd-header {
  display: flex;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #e5e7eb;
}
.cmd-search-icon {
  font-size: 20px;
  color: #9ca3af;
  margin-right: 16px;
}
.cmd-input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 20px;
  color: #111827;
}
.cmd-input::placeholder {
  color: #9ca3af;
}
.cmd-scope {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #f5f7fb;
  padding: 8px 14px;
  border-radius: 999px;
  margin-right: 16px;
  color: #334155;
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
}
.cmd-close {
  border: 0;
  background: transparent;
  font-size: 16px;
  color: #9ca3af;
  cursor: pointer;
  padding: 4px;
  line-height: 1;
}
.cmd-close:hover {
  color: #111827;
}

@media (max-width: 992px) {
  .ladi-header {
    padding: 0 20px;
    gap: 12px;
  }

  .header-left {
    flex: 0 1 auto;
  }

  .sidebar-toggle {
    display: inline-flex;
  }

  .header-center {
    flex: 1 1 auto;
    min-width: 140px;
  }

  .header-right {
    flex: 0 0 auto;
    gap: 8px;
  }

  .action-icons {
    gap: 6px;
  }

  .search-shortcut {
    display: none;
  }

  .dashboard-content {
    padding: 20px 16px;
  }
}

@media (max-width: 720px) {
  .ladi-header {
    padding: 0 16px;
  }

  .workspace-name {
    display: none;
  }

  .header-left {
    gap: 0;
  }
}

@media (max-width: 520px) {
  .search-placeholder {
    display: none;
  }

  .search-bar {
    justify-content: center;
    min-width: 44px;
    padding: 0 10px;
  }

  .search-icon {
    margin-right: 0;
  }
}

.cmd-body {
  padding: 20px;
  min-height: 300px;
  display: flex;
  flex-direction: column;
}
.empty-state {
  margin: auto;
  text-align: center;
}
.search-results {
  width: 100%;
}
.empty-icon-wrapper {
  width: 64px;
  height: 64px;
  background: #eff6ff;
  color: #3b82f6;
  border-radius: 16px;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 32px;
  margin: 0 auto 24px;
}
.empty-state h3 {
  font-size: 18px;
  font-weight: 700;
  color: #111827;
  margin-bottom: 8px;
}
.empty-state p {
  color: #6b7280;
  font-size: 14px;
  margin-bottom: 24px;
}
.example-tags {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  margin-bottom: 24px;
}
.tag-label {
  font-size: 12px;
  font-weight: 600;
  color: #9ca3af;
  letter-spacing: 1px;
}
.tag {
  background: #f3f4f6;
  color: #374151;
  padding: 4px 12px;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
}
.tag:hover {
  background: #e5e7eb;
}
.empty-hint {
  font-size: 13px;
  color: #9ca3af;
}

.cmd-footer {
  padding: 12px 20px;
  border-top: 1px solid #e5e7eb;
  background: #f9fafb;
}
.shortcut-hints {
  display: flex;
  align-items: center;
  gap: 8px;
}
.hint-key {
  background: #ffffff;
  border: 1px solid #d1d5db;
  border-radius: 4px;
  padding: 2px 6px;
  font-size: 12px;
  color: #6b7280;
  font-family: monospace;
  box-shadow: 0 1px 2px rgba(0,0,0,0.05);
}
.hint-text {
  font-size: 12px;
  color: #6b7280;
  margin-right: 16px;
}
</style>


