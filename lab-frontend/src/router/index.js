import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  { path: '/login', name: 'Login', component: () => import('../views/LoginView.vue'), meta: { requiresAuth: false } },
  { path: '/forgot-password', name: 'ForgotPassword', component: () => import('../views/ForgotPasswordView.vue'), meta: { requiresAuth: false } },
  { path: '/reset-password', name: 'ResetPassword', component: () => import('../views/ResetPasswordView.vue'), meta: { requiresAuth: false } },
  { path: '/', name: 'Landing', component: () => import('../views/LandingView.vue'), meta: { requiresAuth: false } },
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: () => import('../views/DashboardView.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', name: 'Overview', component: () => import('../views/OverviewView.vue') },
      { path: 'devices', name: 'Devices', component: () => import('../views/DevicesView.vue') },
      { path: 'locations', name: 'Locations', component: () => import('../views/LocationsView.vue'), meta: { allowedRoles: ['Admin', 'Trưởng lab', 'Phó lab'] } },
      { path: 'admin/users', name: 'AdminUsers', component: () => import('../views/AdminUsersView.vue'), meta: { allowedRoles: ['Admin'] } },
      { path: 'admin/audit-logs', name: 'AuditLogs', component: () => import('../views/AuditLogsView.vue'), meta: { allowedRoles: ['Admin'] } },
      { path: 'borrow-requests', name: 'BorrowRequests', component: () => import('../views/BorrowRequestsView.vue'), meta: { allowedRoles: ['Admin', 'Trưởng lab', 'Phó lab'] } },
      { path: 'borrow-history', name: 'BorrowHistory', component: () => import('../views/BorrowHistoryView.vue') },
      { path: 'maintenance', name: 'Maintenance', component: () => import('../views/MaintenanceView.vue'), meta: { allowedRoles: ['Admin', 'Trưởng lab', 'Phó lab'] } },
      { path: 'consumable-requests', name: 'ConsumableRequests', component: () => import('../views/ConsumableRequestsView.vue'), meta: { allowedRoles: ['Admin', 'Trưởng lab', 'Phó lab', 'Sinh viên', 'Giảng viên'] } },
      { path: 'penalty', name: 'Penalty', component: () => import('../views/PenaltyView.vue'), meta: { allowedRoles: ['Admin', 'Trưởng lab', 'Phó lab', 'Sinh viên', 'Giảng viên'] } },
      { path: 'teacher-approval', name: 'TeacherApproval', component: () => import('../views/TeacherApprovalView.vue'), meta: { allowedRoles: ['Giảng viên'] } }
    ]
  },
  { path: '/:pathMatch(.*)*', name: 'NotFound', component: () => import('../views/NotFoundView.vue'), meta: { requiresAuth: false } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token') || sessionStorage.getItem('token')
  const userRole = localStorage.getItem('role') || sessionStorage.getItem('role')

  if (to.meta.requiresAuth !== false && !token) {
    return next({ name: 'Login' })
  }

  if (to.meta.allowedRoles && !to.meta.allowedRoles.includes(userRole)) {
    window.alert('Bạn không có quyền truy cập tính năng này!')
    return next({ name: 'Devices' })
  }

  next()
})

export default router


