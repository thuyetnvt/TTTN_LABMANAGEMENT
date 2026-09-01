import { test, expect } from '@playwright/test'

const fullStackEnabled = process.env.E2E_TEST_DATABASE === '1'
const password = process.env.E2E_ROLE_PASSWORD || process.env.E2E_ADMIN_PASSWORD

const roles = [
  {
    name: 'Quản trị viên',
    username: process.env.E2E_ADMIN_USERNAME || 'admin',
    allowedRoute: '/dashboard/admin/users',
    heading: 'Quản lý người dùng'
  },
  {
    name: 'Trưởng lab',
    username: process.env.E2E_MANAGER_USERNAME || 'truonglab',
    allowedRoute: '/dashboard/borrow-requests',
    heading: 'Duyệt yêu cầu mượn/trả',
    deniedRoute: '/dashboard/admin/users'
  },
  {
    name: 'Phó lab',
    username: process.env.E2E_DEPUTY_USERNAME || 'pholab',
    allowedRoute: '/dashboard/maintenance',
    heading: 'Lịch sử Bảo trì & Hiệu chuẩn',
    deniedRoute: '/dashboard/admin/audit-logs'
  },
  {
    name: 'Giảng viên',
    username: process.env.E2E_TEACHER_USERNAME || 'giangvien1',
    allowedRoute: '/dashboard/teacher-approval',
    heading: 'Duyệt bảo lãnh mượn thiết bị',
    deniedRoute: '/dashboard/borrow-requests'
  },
  {
    name: 'Sinh viên',
    username: process.env.E2E_STUDENT_USERNAME || 'sv1',
    allowedRoute: '/dashboard/borrow-history',
    heading: 'Lịch sử mượn/trả',
    deniedRoute: '/dashboard/inventory'
  }
]

test.beforeEach(() => {
  test.skip(!fullStackEnabled || !password, 'Bài kiểm tra vai trò chỉ chạy với database E2E riêng.')
})

for (const role of roles) {
  test(`${role.name} chỉ mở được màn hình đúng quyền`, async ({ page }) => {
    await login(page, role.username, password)
    await page.goto(role.allowedRoute)
    await expect(page.getByRole('heading', { name: role.heading })).toBeVisible()

    if (role.deniedRoute) {
      page.once('dialog', dialog => dialog.accept())
      await page.goto(role.deniedRoute)
      await expect(page).toHaveURL(/\/dashboard\/devices$/)
    }
  })
}

async function login(page, username, userPassword) {
  await page.goto('/login')
  await page.getByLabel(/tài khoản/i).fill(username)
  await page.getByLabel(/mật khẩu/i).fill(userPassword)
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/auth/login'))
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  const response = await responsePromise
  expect(response.ok(), `${username}: ${await response.text()}`).toBeTruthy()
  await expect(page).toHaveURL(/\/dashboard/)
}
