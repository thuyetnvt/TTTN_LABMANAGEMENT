import { test, expect } from '@playwright/test'

test('public pages render in Vietnamese and protected route redirects', async ({ page }) => {
  test.setTimeout(60_000)

  await page.goto('/', { waitUntil: 'domcontentloaded' })
  await expect(page.getByRole('button', { name: /Đăng nhập/i }).first()).toBeVisible()
  await expect(page.getByText(/Chuyển đổi số toàn diện/i)).toBeVisible()

  await page.goto('/login', { waitUntil: 'domcontentloaded' })
  await expect(page.getByRole('heading', { name: 'Chào mừng trở lại' })).toBeVisible()
  await page.goto('/forgot-password', { waitUntil: 'domcontentloaded' })
  await expect(page.getByRole('heading', { name: 'Quên mật khẩu' })).toBeVisible()

  await page.goto('/dashboard/inventory', { waitUntil: 'domcontentloaded' })
  await expect(page).toHaveURL(/\/login$/)
})

test('login form exposes concrete validation messages', async ({ page }) => {
  await page.goto('/login')
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page.getByText(/nhập tài khoản/i)).toBeVisible()
})

test('seeded admin can access management route when credentials are provided', async ({ page }) => {
  const username = process.env.E2E_ADMIN_USERNAME
  const password = process.env.E2E_ADMIN_PASSWORD
  test.skip(!username || !password, 'Cần E2E_ADMIN_USERNAME và E2E_ADMIN_PASSWORD để chạy flow có dữ liệu thật.')

  await page.goto('/login')
  await page.getByLabel(/tài khoản/i).fill(username)
  await page.getByLabel(/mật khẩu/i).fill(password)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/dashboard/)
  await page.goto('/dashboard/admin/users')
  await expect(page.getByRole('heading', { name: 'Quản lý người dùng' })).toBeVisible()
})
