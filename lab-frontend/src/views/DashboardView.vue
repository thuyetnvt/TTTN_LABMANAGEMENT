<template>
  <a-layout style="height: 100vh; overflow: hidden; background: var(--color-canvas-cream);">
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
      
      <a-menu 
        :selectedKeys="[selectedKey]"
        theme="light" 
        mode="inline"
        class="ladi-menu"
      >
        <a-menu-item key="0" @click="$router.push({ name: 'Overview' })">
          <appstore-filled />
          <span>{{ $t('menu.overview') }}</span>
        </a-menu-item>
        
        <a-menu-item key="1" @click="$router.push({ name: 'Devices' })">
          <desktop-outlined />
          <span>{{ $t('menu.devices') }}</span>
        </a-menu-item>
        
        <a-menu-item key="3" @click="$router.push({ name: 'BorrowHistory' })">
          <history-outlined />
          <span>{{ $t('menu.borrowHistory') }}</span>
        </a-menu-item>

        <a-menu-item key="profile" @click="$router.push({ name: 'Profile' })">
          <user-outlined />
          <span>Hồ sơ cá nhân</span>
        </a-menu-item>

        <!-- Divider/Group cho tính năng Quản lý -->
        <a-menu-divider v-if="isManagerRole(role)" />
        

        <!-- Menu riêng cho Giảng viên -->
        <a-menu-item v-if="role === 'Giảng viên'" key="m_teacher" @click="$router.push({ name: 'TeacherApproval' })">
          <solution-outlined />
          <span>{{ $t('menu.teacherApproval') }}</span>
        </a-menu-item>

        <!-- Menu vận hành lab -->
        <a-menu-item v-if="isManagerRole(role)" key="m3" @click="$router.push({ name: 'Maintenance' })">
          <tool-outlined />
          <span>{{ $t('menu.maintenanceHistory') }}</span>
        </a-menu-item>
        <a-menu-item v-if="isManagerRole(role)" key="m_schedule" @click="$router.push({ name: 'MaintenanceSchedules' })">
          <calendar-outlined />
          <span>Bảo trì định kỳ</span>
        </a-menu-item>
        <a-menu-item v-if="isManagerRole(role)" key="m_location" @click="$router.push({ name: 'Locations' })">
          <environment-outlined />
          <span>Vị trí tài sản</span>
        </a-menu-item>
        <a-menu-item v-if="isManagerRole(role)" key="m_inventory" @click="$router.push({ name: 'Inventory' })">
          <scan-outlined />
          <span>Kiểm kê tài sản</span>
        </a-menu-item>
        <a-menu-item v-if="isManagerRole(role)" key="m_reports" @click="$router.push({ name: 'Reports' })">
          <bar-chart-outlined />
          <span>Báo cáo</span>
        </a-menu-item>
        
        <!-- Menu cho Đền bù -->
        <a-menu-item key="m4" @click="$router.push({ name: 'Penalty' })">
          <pay-circle-outlined />
          <span>{{ $t('menu.penalty') }}</span>
        </a-menu-item>

        <!-- Nhóm Admin / Trưởng lab / Phó lab -->
        <a-menu-divider v-if="isManagerRole(role)" />
        
        <a-menu-item v-if="isManagerRole(role)" key="g1_1" @click="$router.push({ name: 'BorrowRequests' })">
          <solution-outlined />
          <span>{{ $t('menu.borrowRequests') }}</span>
        </a-menu-item>
        <a-menu-item v-if="isManagerRole(role)" key="g1_2" @click="$router.push({ name: 'ConsumableRequests' })">
          <experiment-outlined />
          <span>{{ $t('menu.consumableRequests') }}</span>
        </a-menu-item>
        <a-menu-item v-if="isAdminRole(role)" key="g1_3" @click="$router.push({ name: 'AdminUsers' })">
          <team-outlined />
          <span>{{ $t('menu.userManagement') }}</span>
        </a-menu-item>
        <a-menu-item v-if="isAdminRole(role)" key="g1_4" @click="$router.push({ name: 'AuditLogs' })">
          <history-outlined />
          <span>{{ $t('menu.auditLogs') }}</span>
        </a-menu-item>

        <!-- Nhóm Sinh viên / Giảng viên (History) -->
        <a-menu-divider v-if="isBorrowerRole(role)" />
        <a-menu-item v-if="isBorrowerRole(role)" key="g2_2" @click="$router.push({ name: 'ConsumableRequests' })">
          <history-outlined />
          <span>{{ $t('menu.studentConsumableHistory') }}</span>
        </a-menu-item>

        <a-menu-item key="logout" @click="handleLogout" class="logout-item">
          <logout-outlined />
          <span>{{ $t('menu.logout') }} ({{ roleLabel(role) }})</span>
        </a-menu-item>
      </a-menu>
    </a-layout-sider>

    <!-- Khu Vực Nội Dung Bên Phải -->
    <a-layout>
      <!-- Thanh Header -->
      <a-layout-header class="ladi-header">
        <div class="header-left">
          <a-button type="text" class="sidebar-toggle" @click="collapsed = !collapsed">
            <template #icon>
              <menu-unfold-outlined v-if="collapsed" />
              <menu-fold-outlined v-else />
            </template>
          </a-button>
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

            <a-popover v-model:open="notificationOpen" trigger="click" placement="bottomRight">
              <template #content>
                <div class="notification-popover">
                  <div class="notification-popover-header"><strong>Thông báo</strong><a @click="router.push({ name: 'Notifications' }); notificationOpen = false">Xem tất cả</a></div>
                  <a-list :data-source="notifications.slice(0, 5)" size="small">
                    <template #renderItem="{ item }">
                      <a-list-item :class="{ unread: !item.isRead }" @click="openNotification(item)">
                        <a-list-item-meta :title="item.title" :description="item.message" />
                      </a-list-item>
                    </template>
                  </a-list>
                  <a-empty v-if="!notifications.length" description="Chưa có thông báo" :image-style="{ height: '40px' }" />
                </div>
              </template>
              <a-badge :count="unreadCount" :overflow-count="99">
                <a-button type="text" class="header-icon-btn" title="Thông báo">
                  <template #icon><bell-outlined /></template>
                </a-button>
              </a-badge>
            </a-popover>
          </div>
          <div class="user-profile" title="Đổi mật khẩu" @click="changePasswordVisible = true">
            <a-avatar :size="38" class="user-avatar">
              {{ role.charAt(0).toUpperCase() }}
            </a-avatar>
          </div>
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
        <close-outlined class="cmd-close" @click="searchVisible = false" />
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
                <div>{{ item.status }}</div>
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
  BellOutlined,
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
import { notificationApi } from '../api/notificationApi'
import { isAdminRole, isBorrowerRole, isManagerRole, roleLabel } from '../constants/business'

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
const role = computed(() => authStore.role)
const collapsed = ref(false)
const searchShortcut = computed(() => {
  if (typeof navigator !== 'undefined' && /win/i.test(navigator.userAgent)) {
    return 'Ctrl K'
  }
  return '⌘ K'
})
const routeMenuKeys = {
  Overview: '0',
  Devices: '1',
  BorrowHistory: '3',
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
const notifications = ref([])
const unreadCount = ref(0)
const notificationOpen = ref(false)

const loadNotifications = async () => {
  try {
    notifications.value = await notificationApi.getAll() || []
    const result = await notificationApi.getUnreadCount()
    unreadCount.value = result?.count || 0
  } catch (error) { console.error('Lỗi tải thông báo', error) }
}

const openNotification = async item => {
  if (!item.isRead) {
    await notificationApi.markRead(item.id)
    item.isRead = true
    unreadCount.value = Math.max(0, unreadCount.value - 1)
  }
  notificationOpen.value = false
  if (item.url) router.push(item.url)
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
  loadNotifications()
  // Kết nối SignalR
  const signalRUrl = import.meta.env.VITE_SIGNALR_URL || 'http://localhost:5248/notificationHub'
  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(signalRUrl, {
      accessTokenFactory: () => localStorage.getItem('token') || sessionStorage.getItem('token') || ''
    })
    .withAutomaticReconnect()
    .build()

  hubConnection.on('ReceiveNotification', (payload) => {
    const text = typeof payload === 'string' ? payload : payload?.message
    notification.info({
      message: 'Thông báo mới',
      description: text,
      placement: 'topRight',
      duration: 5
    })
    loadNotifications()
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
  router.push('/')
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

/* Menu Customization to match LadiPage */
.ladi-menu {
  border-right: none;
  padding: 14px 10px;
  background: var(--color-canvas-cream);
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

/* Logout Button Placement */
.logout-item {
  margin-top: 40px !important;
  color: #ef4444 !important;
}
.logout-item:hover {
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
  font-size: 16px;
  color: #9ca3af;
  cursor: pointer;
  padding: 4px;
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


