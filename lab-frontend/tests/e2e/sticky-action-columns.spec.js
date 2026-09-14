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
    borrowerPhone: '0987654321',
    status: 'BORROW_PENDING'
  }, {
    id: 11,
    student: 'sv2',
    device: 'Module LoRa SX1278',
    category: 'IoT',
    serial: 'LORA-011',
    details: [{ id: 11, equipmentName: 'Module LoRa SX1278', quantity: 1 }],
    requestDate: now,
    returnDate: '2026-09-11T08:00:00Z',
    daysUntilDue: 9,
    purpose: 'Kiểm tra biên bản bàn giao',
    borrowerPhone: '0987654322',
    status: 'APPROVED',
    hasHandover: true,
    handoverCode: 'BG-E2E-011'
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
  '/api/equipment/paged': paged([{
    id: 4,
    assetCode: 'TS-E2E-004',
    name: 'Máy hiện sóng E2E',
    categoryName: 'Thiết bị đo',
    model: 'E2E-1000',
    serial: 'E2E-SERIAL-004',
    location: 'Phòng IoT A',
    responsiblePerson: 'Quản trị viên',
    responsibleName: 'Quản trị viên',
    entryDate: now,
    depreciationPercentage: 10,
    status: 'AVAILABLE'
  }]),
  '/api/assetcategory': [{ id: 1, name: 'Thiết bị đo' }],
  '/api/location': [
    { id: 99, code: 'LAB-ROOT', name: 'Phòng Lab IoT', type: 'BUILDING', isActive: true, equipmentCount: 0 },
    { id: 1, code: 'LAB-IOT-A', name: 'Phòng IoT A', type: 'ROOM', isActive: true, equipmentCount: 1 }
  ],
  '/api/users/teachers': [],
  '/api/users/responsible': [{ id: 1, username: 'admin', fullName: 'Quản trị viên', universityCode: 'ADMIN001' }],
}

const pages = [
  '/dashboard/borrow-requests',
  '/dashboard/borrow-history',
  '/dashboard/consumable-requests',
  '/dashboard/devices',
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

test('các ô lọc nhật ký có kích thước hiển thị đúng 150 x 40', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/dashboard/admin/audit-logs')

  const controls = [
    page.locator('.filter-search .ant-input-affix-wrapper'),
    page.locator('.filter-control').nth(0),
    page.locator('.filter-control').nth(1),
    page.locator('.filter-dates')
  ]

  for (const control of controls) {
    await expect(control).toBeVisible()
    const box = await control.boundingBox()
    expect(box).toBeTruthy()
    expect(Math.round(box.width)).toBe(150)
    expect(Math.round(box.height)).toBe(40)
  }
})

test('các ô lọc quản lý người dùng có kích thước hiển thị đúng 150 x 40', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/dashboard/admin/users')

  const controls = [
    page.locator('.user-filter-search .ant-input-affix-wrapper'),
    page.locator('.user-filter-control').nth(0),
    page.locator('.user-filter-control').nth(1)
  ]

  for (const control of controls) {
    await expect(control).toBeVisible()
    const box = await control.boundingBox()
    expect(box).toBeTruthy()
    expect(Math.round(box.width)).toBe(150)
    expect(Math.round(box.height)).toBe(40)
  }
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

    const headers = (await table.locator('.ant-table-thead > tr > th').allTextContents())
      .map(value => value.trim())
    const statusIndex = headers.findIndex(value => value.includes('Trạng thái'))
    const actionIndex = headers.findIndex(value => value.includes('Hành động'))
    if (statusIndex >= 0) {
      expect(statusIndex, `${path}: cột trạng thái phải nằm ngay trước hành động`).toBe(actionIndex - 1)
    }
  }
})

test('biểu mẫu thiết bị không còn trường Tên seri', async ({ page }) => {
  await page.goto('/dashboard/devices')
  await page.getByRole('button', { name: /Thêm thiết bị/ }).click()
  const dialog = page.getByRole('dialog', { name: 'Thêm thiết bị' })
  await expect(dialog.getByText('Tên seri', { exact: true })).toHaveCount(0)
  await expect(dialog.getByText('Số seri', { exact: true })).toBeVisible()
})

test('quản lý vị trí không còn vị trí cha và không hiển thị nút gốc phòng lab', async ({ page }) => {
  await page.goto('/dashboard/locations')

  await expect(page.getByRole('heading', { name: 'Danh sách vị trí tài sản' })).toBeVisible()
  await expect(page.getByText('LAB-ROOT', { exact: true })).toHaveCount(0)
  await page.getByRole('button', { name: 'Sửa vị trí' }).click()

  const dialog = page.getByRole('dialog', { name: 'Sửa vị trí' })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText('Vị trí cha', { exact: true })).toHaveCount(0)
})

test('bảng yêu cầu cấp phát hiển thị cột số lượng gọn và cột hành động đủ rộng', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 900 })
  await page.goto('/dashboard/consumable-requests')

  const table = page.locator('.desktop-table')
  const quantityBox = await table.getByRole('columnheader', { name: /Số lượng/ }).boundingBox()
  const statusBox = await table.getByRole('columnheader', { name: /Trạng thái/ }).boundingBox()
  const actionBox = await table.getByRole('columnheader', { name: 'Hành động' }).boundingBox()

  expect(quantityBox).toBeTruthy()
  expect(statusBox).toBeTruthy()
  expect(actionBox).toBeTruthy()
  expect(Math.round(quantityBox.width)).toBeLessThanOrEqual(160)
  expect(Math.round(statusBox.width)).toBeLessThanOrEqual(250)
  expect(Math.round(actionBox.width)).toBeGreaterThanOrEqual(290)
})

test('bảng vật tư tiêu hao ẩn các cột tồn kho phụ', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 900 })
  await page.goto('/dashboard/devices?tab=consumables')

  const table = page.locator('.consumables-desktop-table')
  for (const columnName of ['Danh mục', 'Tổng tồn', 'Tồn tối thiểu', 'Đang giữ', 'Số lô']) {
    await expect(table.getByRole('columnheader', { name: new RegExp(columnName) })).toHaveCount(0)
  }
  for (const columnName of ['Mã vật tư', 'Tên vật tư', 'Đơn vị', 'Khả dụng', 'Người chịu trách nhiệm', 'Trạng thái', 'Hành động']) {
    await expect(table.getByRole('columnheader', { name: new RegExp(columnName) })).toBeVisible()
  }
})

test('lịch sử mượn trả không ghim trạng thái và ẩn các cột chỉ cần trong chi tiết', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/dashboard/borrow-history')

  const table = page.locator('.desktop-table')
  const statusHeader = table.getByRole('columnheader', { name: /Trạng thái/ })

  await expect(statusHeader).toBeVisible()
  await expect(table.locator('.table-sticky-status-column')).toHaveCount(0)
  await expect(table.getByRole('columnheader', { name: /Ngày đăng ký/ })).toHaveCount(0)
  await expect(table.getByRole('columnheader', { name: /Hạn trả/ })).toHaveCount(0)
  await expect(table.getByRole('columnheader', { name: /Tình trạng trả/ })).toHaveCount(0)

  expect(await statusHeader.evaluate(element => getComputedStyle(element).position)).not.toBe('sticky')
})

test('phiếu chờ duyệt chỉ hiện thông tin phụ trong cửa sổ chi tiết', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 })
  await page.goto('/dashboard/borrow-requests')

  const table = page.locator('.desktop-table')
  for (const columnName of ['SĐT liên hệ', 'Số seri', 'Ngày đăng ký', 'Hạn trả']) {
    await expect(table.getByRole('columnheader', { name: new RegExp(columnName) })).toHaveCount(0)
  }

  const rows = table.locator('.ant-table-tbody > tr.ant-table-row')
  await expect(rows).toHaveCount(2)
  await expect(rows.nth(0).locator('button.view-action')).toHaveCount(1)
  await expect(rows.nth(1).locator('button.view-action')).toHaveCount(1)

  await page.getByRole('button', { name: 'Xem chi tiết yêu cầu' }).click()
  const dialog = page.getByRole('dialog', { name: 'Chi tiết yêu cầu mượn' })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText('0987654321', { exact: true })).toBeVisible()
  await expect(dialog.getByText(/Số seri: ARD-001/)).toBeVisible()
  await expect(dialog.getByText('Ngày đăng ký', { exact: true })).toBeVisible()
  await expect(dialog.getByText('Hạn trả', { exact: true })).toBeVisible()
})
