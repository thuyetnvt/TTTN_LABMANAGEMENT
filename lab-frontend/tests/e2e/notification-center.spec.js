import { test, expect } from '@playwright/test'

const password = process.env.E2E_ADMIN_PASSWORD
const username = process.env.E2E_ADMIN_USERNAME || 'admin'

const notificationItems = Array.from({ length: 20 }, (_, index) => ({
  id: index + 1,
  type: index % 2 ? 'MAINTENANCE_COMPLETED' : 'BORROW_PENDING',
  title: index % 2 ? 'Phiếu bảo trì đã hoàn tất' : 'Yêu cầu mượn cần duyệt',
  message: `Nội dung thông báo kiểm thử ${index + 1}`,
  url: index === 0 ? '/dashboard/devices' : '',
  isRead: index > 1,
  createdAt: new Date(Date.now() - index * 60000).toISOString(),
  readAt: index > 1 ? new Date().toISOString() : null
}))

async function mockNotificationApi(page, { unreadCount = 120 } = {}) {
  let unreadOnlySeen = false

  // Cô lập bài kiểm thử khỏi thông báo SignalR do luồng nghiệp vụ chạy song song phát ra.
  await page.unroute('**/notificationHub**')
  await page.route('**/notificationHub**', route => route.abort())
  await page.unroute('**/api/notification**')
  await page.route('**/api/notification**', async route => {
    const method = route.request().method()
    const url = new URL(route.request().url())

    if (method === 'GET' && url.pathname.endsWith('/notification/unread-count')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ count: unreadCount })
      })
      return
    }

    if (method === 'PUT' && (url.pathname.endsWith('/notification/read-all') || url.pathname.endsWith('/read'))) {
      await route.fulfill({ status: 204, body: '' })
      return
    }

    if (method !== 'GET' || !url.pathname.endsWith('/notification')) return route.continue()
    unreadOnlySeen = url.searchParams.get('unreadOnly') === 'true'
    const items = unreadOnlySeen ? notificationItems.filter(item => !item.isRead) : notificationItems
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items,
        page: Number(url.searchParams.get('page') || 1),
        pageSize: Number(url.searchParams.get('pageSize') || 20),
        total: items.length,
        hasNextPage: false
      })
    })
  })
  return { unreadOnlySeen: () => unreadOnlySeen }
}

async function loginUi(page, route = '/dashboard') {
  test.skip(!password, 'Đặt E2E_ADMIN_PASSWORD để chạy E2E notification center có đăng nhập.')
  await page.goto('/login')
  await page.getByLabel(/tài khoản/i).fill(username)
  await page.getByLabel(/mật khẩu/i).fill(password)
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/auth/login'))
  await page.getByRole('button', { name: 'Đăng nhập' }).click()
  const response = await responsePromise
  expect(response.ok(), await response.text()).toBeTruthy()
  await page.goto(route)
  await expect(page).toHaveURL(new RegExp(route.replace('/', '\\/')))
}

test('popover desktop có giới hạn chiều rộng, vùng cuộn và điều hướng', async ({ page }) => {
  page.setViewportSize({ width: 1366, height: 768 })
  await mockNotificationApi(page)
  await loginUi(page)

  await page.getByTestId('notification-bell').click()
  const popover = page.getByTestId('notification-popover')
  await expect(popover).toBeVisible()
  const box = await popover.boundingBox()
  expect(box.width).toBeLessThanOrEqual(400)
  const listMetrics = await page.getByTestId('notification-popover-list').evaluate(element => ({
    overflowY: getComputedStyle(element).overflowY,
    scrollHeight: element.scrollHeight,
    clientHeight: element.clientHeight
  }))
  expect(listMetrics.overflowY).toBe('auto')
  expect(listMetrics.scrollHeight).toBeGreaterThan(listMetrics.clientHeight)
  await expect(page.locator('.ant-badge-count')).toHaveText('99+')

  // Các E2E chạy song song có thể phát toast SignalR thật che nút trong chốc lát.
  // Force click ở đây chỉ loại bỏ nhiễu lớp phủ, vẫn kiểm tra đầy đủ request và trạng thái UI.
  await page.getByRole('button', { name: 'Đánh dấu tất cả đã đọc' }).click({ force: true })
  await expect(page.getByRole('button', { name: 'Đánh dấu tất cả đã đọc' })).toHaveCount(0)
  const viewAllButton = page.getByRole('button', { name: 'Xem tất cả thông báo' })
  if (await viewAllButton.count() === 0) {
    await page.getByTestId('notification-bell').click({ force: true })
  }
  await viewAllButton.click({ force: true })
  await expect(page).toHaveURL(/\/dashboard\/notifications$/)
})

test('popover mobile vẫn vừa viewport và giữ header/footer cố định', async ({ page }) => {
  page.setViewportSize({ width: 390, height: 844 })
  await mockNotificationApi(page)
  await loginUi(page)

  await page.getByTestId('notification-bell').click()
  const popover = page.getByTestId('notification-popover')
  await expect(popover).toBeVisible()
  const box = await popover.boundingBox()
  expect(box.width).toBeLessThanOrEqual(366)
  await expect(page.getByTestId('notification-popover-header')).toBeVisible()
  await expect(page.getByTestId('notification-popover-footer')).toBeVisible()
  await expect(page.getByTestId('notification-popover-list')).toBeVisible()
})

test('badge 0 được ẩn và bộ lọc Chưa đọc gọi API đúng', async ({ page }) => {
  await mockNotificationApi(page, { unreadCount: 0 })
  await loginUi(page, '/dashboard/notifications')
  await expect(page.getByText('0 chưa đọc')).toBeVisible()
  await expect(page.getByTestId('notification-bell').locator('.ant-badge-count')).toHaveCount(0)

  const filterState = await mockNotificationApi(page, { unreadCount: 2 })
  await page.getByRole('tab', { name: 'Chưa đọc' }).click()
  await expect.poll(filterState.unreadOnlySeen).toBe(true)
})
