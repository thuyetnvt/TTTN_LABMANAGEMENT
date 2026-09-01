import { test, expect } from '@playwright/test'

const now = '2026-09-02T08:00:00Z'

const paged = items => ({ items, total: items.length, page: 1, pageSize: 20 })

const responses = {
  '/api/borrow/pending/paged': paged([{
    id: 1,
    student: 'sv1',
    device: 'Bộ kit Arduino Uno R3',
    category: 'IoT',
    serial: 'ARD-001',
    details: [{ id: 1, equipmentName: 'Bộ kit Arduino Uno R3', quantity: 1 }],
    requestDate: now,
    returnDate: '2026-09-10T08:00:00Z',
    daysUntilDue: 8,
    purpose: 'Demo thực hành IoT tuần 3',
    status: 'BORROW_PENDING'
  }]),
  '/api/borrow/history/paged': paged([{
    id: 2,
    student: 'sv2',
    device: 'Module LoRa SX1278',
    serial: 'LORA-002',
    requestDate: now,
    returnDate: '2026-09-12T08:00:00Z',
    returnCondition: null,
    returnInspectionNote: '',
    warrantyAction: '',
    compensationAmount: 0,
    status: 'APPROVED',
    canConfirmHandover: true
  }]),
  '/api/consumablerequest/paged': paged([{
    id: 3,
    consumableName: 'Điện trở 220 Ohm',
    categoryName: 'Linh kiện',
    username: 'sv3',
    quantity: 30,
    reason: 'Làm bài thực hành mạch LED',
    status: 'CONSUMABLE_PENDING',
    requestDate: now
  }]),
  '/api/maintenance/paged': paged([{
    id: 4,
    device: 'Máy hiện sóng Rigol DS1054Z',
    maintenanceDate: now,
    description: 'Hiệu chuẩn định kỳ và kiểm tra đầu đo',
    performedBy: 'Kỹ thuật viên',
    cost: 450000,
    status: 'MAINTENANCE_IN_PROGRESS',
    result: ''
  }]),
  '/api/maintenance-schedules/paged': paged([{
    id: 5,
    device: 'Máy hiện sóng Hantek 6022BE',
    name: 'Hiệu chuẩn máy đo hằng quý',
    intervalDays: 90,
    intervalUnit: 'DAY',
    nextDueAt: '2026-09-10T08:00:00Z',
    isDue: false,
    isActive: true
  }]),
  '/api/penalty/paged': paged([{
    id: 6,
    username: 'sv4',
    equipmentName: 'Nguồn DC lập trình Korad',
    reason: 'Cổng output lỏng sau khi trả thiết bị',
    amount: 350000,
    createdAt: now,
    status: 'UNPAID'
  }])
}

const pages = [
  '/dashboard/borrow-requests',
  '/dashboard/borrow-history',
  '/dashboard/consumable-requests',
  '/dashboard/maintenance',
  '/dashboard/maintenance-schedules',
  '/dashboard/penalty'
]

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('token', 'sticky-column-test-token')
    localStorage.setItem('role', 'Admin')
  })

  await page.route(url => new URL(url).pathname.startsWith('/api/'), async route => {
    const path = new URL(route.request().url()).pathname
    const body = responses[path]
      || (path === '/api/users/me'
        ? { id: 1, username: 'admin', fullName: 'Quản trị viên', role: 'Admin' }
        : paged([]))

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(body)
    })
  })
})

test('cột hành động giữ đúng hàng và đúng mép phải khi kéo ngang', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })

  for (const path of pages) {
    await page.goto(path)

    const table = page.locator('.ant-table-wrapper').filter({
      has: page.locator('th.table-sticky-action-column')
    }).first()
    const headerCell = table.locator('th.table-sticky-action-column')
    const bodyCell = table.locator('.ant-table-tbody > tr.ant-table-row').first().locator('td.table-sticky-action-column')
    const scrollContainer = table.locator('.ant-table-content')

    await expect(headerCell, `${path}: thiếu header hành động được ghim`).toHaveCount(1)
    await expect(bodyCell, `${path}: thiếu ô hành động trong body`).toHaveCount(1)
    await expect(bodyCell.locator('button'), `${path}: nút bị render ngoài ô hành động`).not.toHaveCount(0)

    await scrollContainer.evaluate(element => {
      element.scrollLeft = element.scrollWidth
    })
    await page.waitForTimeout(100)

    const [headerBox, bodyBox, scrollBox] = await Promise.all([
      headerCell.boundingBox(),
      bodyCell.boundingBox(),
      scrollContainer.boundingBox()
    ])

    expect(headerBox, `${path}: không đo được header`).toBeTruthy()
    expect(bodyBox, `${path}: không đo được body`).toBeTruthy()
    expect(scrollBox, `${path}: không đo được vùng cuộn`).toBeTruthy()
    expect(Math.abs(headerBox.x - bodyBox.x), `${path}: header/body lệch trái`).toBeLessThanOrEqual(1)
    expect(Math.abs((headerBox.x + headerBox.width) - (bodyBox.x + bodyBox.width)), `${path}: header/body lệch phải`).toBeLessThanOrEqual(1)
    expect(Math.abs((bodyBox.x + bodyBox.width) - (scrollBox.x + scrollBox.width)), `${path}: cột hành động không bám mép phải`).toBeLessThanOrEqual(2)
  }
})
