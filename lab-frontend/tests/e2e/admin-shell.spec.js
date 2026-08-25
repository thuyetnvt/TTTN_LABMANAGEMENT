import { test, expect } from '@playwright/test'

const loginAsAdmin = async (page) => {
  const username = process.env.E2E_ADMIN_USERNAME
  const password = process.env.E2E_ADMIN_PASSWORD
  test.skip(!username || !password, 'Cần E2E_ADMIN_USERNAME và E2E_ADMIN_PASSWORD để chạy flow Admin.')

  await page.goto('/login')
  await page.getByLabel(/tài khoản/i).fill(username)
  await page.getByLabel(/mật khẩu/i).fill(password)
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  await expect(page).toHaveURL(/\/dashboard/)
}

test('Admin shell groups menu and keeps account actions visible on a short desktop', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 600 })
  await loginAsAdmin(page)

  await expect(page.getByText('Quản lý tài sản', { exact: true })).toBeVisible()
  await expect(page.getByText('Mượn và trả', { exact: true })).toBeVisible()
  await expect(page.getByText('Vận hành', { exact: true })).toBeVisible()
  await expect(page.getByText('Quản trị hệ thống', { exact: true })).toBeVisible()

  const menuScroll = page.getByTestId('sidebar-menu-scroll')
  await expect(menuScroll).toBeVisible()
  const scrollState = await menuScroll.evaluate((element) => ({
    overflowY: getComputedStyle(element).overflowY,
    clientHeight: element.clientHeight,
    scrollHeight: element.scrollHeight
  }))
  expect(scrollState.overflowY).toBe('auto')
  expect(scrollState.scrollHeight).toBeGreaterThanOrEqual(scrollState.clientHeight)
  await expect(page.getByTestId('sidebar-account-footer')).toBeVisible()

  await page.getByTestId('account-menu-trigger').first().click()
  await expect(page.getByRole('menuitem', { name: 'Hồ sơ cá nhân' })).toBeVisible()
  await expect(page.getByRole('menuitem', { name: 'Đổi mật khẩu' })).toBeVisible()
  await expect(page.getByRole('menuitem', { name: 'Đăng xuất' })).toBeVisible()
})

test('Profile is the active menu and logout returns to login', async ({ page }) => {
  await loginAsAdmin(page)
  await page.goto('/dashboard/profile')

  await expect(page).toHaveURL(/\/dashboard\/profile$/)
  await expect(page.locator('[data-testid="menu-overview"].ant-menu-item-selected')).toHaveCount(0)

  await page.getByTestId('account-menu-trigger').first().click()
  await page.getByRole('menuitem', { name: 'Hồ sơ cá nhân' }).click()
  await expect(page).toHaveURL(/\/dashboard\/profile$/)
  await page.getByTestId('account-menu-trigger').first().click()
  await page.getByRole('menuitem', { name: 'Đăng xuất' }).click()
  await expect(page).toHaveURL(/\/login$/)
})

test('Admin shell can open the menu on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await loginAsAdmin(page)

  await page.getByTestId('sidebar-toggle').click()
  await expect(page.getByText('Quản lý tài sản', { exact: true })).toBeVisible()
  await expect(page.getByTestId('sidebar-menu-scroll')).toBeVisible()
  await expect(page.getByTestId('account-menu-trigger').first()).toBeVisible()
})
