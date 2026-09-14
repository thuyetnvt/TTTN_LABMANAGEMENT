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
  '/api/handover/issue-reports': [{
    id: 41,
    handoverRecordId: 31,
    borrowRecordId: 21,
    handoverCode: 'BG-E2E-041',
    borrowerName: 'Sinh viên E2E',
    borrowerUsername: 'sv-e2e',
    equipmentId: 4,
    equipmentName: 'Máy hiện sóng E2E',
    assetCode: 'TS-E2E-004',
    serial: 'E2E-SERIAL-004',
    issueType: 'NOT_WORKING',
    description: 'Thiết bị không hoạt động khi bàn giao.',
    status: 'HANDOVER_ISSUE_PENDING',
    reportedAt: now,
    evidence: [{
      id: 51,
      originalFileName: 'bang-chung-e2e.svg',
      contentType: 'image/svg+xml',
      fileSize: 256,
      uploadedAt: now
    }]
  }],
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
    if (path === '/api/handover/issue-reports/41/evidence/51') {
      await route.fulfill({
        status: 200,
        contentType: 'image/svg+xml',
        body: '<svg xmlns="http://www.w3.org/2000/svg" width="112" height="84"><rect width="112" height="84" fill="#e27757"/><circle cx="56" cy="42" r="20" fill="#fff"/></svg>'
      })
      return
    }

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

test('bảng thiết bị ẩn cột model nhưng chi tiết vẫn hiển thị model', async ({ page }) => {
  await page.goto('/dashboard/devices')

  const table = page.locator('.device-table')
  await expect(table.getByRole('columnheader', { name: 'Model', exact: true })).toHaveCount(0)

  await table.getByRole('button', { name: 'Xem chi tiết thiết bị' }).first().click()
  const dialog = page.getByRole('dialog', { name: 'Chi tiết thiết bị' })
  await expect(dialog).toBeVisible()
  await expect(dialog.getByText('Model', { exact: true })).toBeVisible()
  await expect(dialog.getByText('E2E-1000', { exact: true })).toBeVisible()
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

test('cột trạng thái căn giữa cả tiêu đề, bộ điều khiển và nội dung', async ({ page }) => {
  await page.goto('/dashboard/locations')

  const statusHeader = page.locator('th.status-column')
  const statusHeaderContent = statusHeader.locator('.table-column-header')
  const statusHeaderControls = statusHeader.locator('.table-column-controls')
  const statusCell = page.locator('td.status-column').first()

  await expect(statusHeader).toBeVisible()
  await expect(statusHeaderContent).toBeVisible()
  await expect(statusHeaderControls).toBeVisible()
  await expect(statusCell).toBeVisible()
  expect(await statusHeader.evaluate(element => getComputedStyle(element).textAlign)).toBe('center')
  expect(await statusHeaderContent.evaluate(element => getComputedStyle(element).justifyContent)).toBe('center')
  expect(await statusHeaderControls.evaluate(element => getComputedStyle(element).marginLeft)).toBe('4px')
  expect(await statusCell.evaluate(element => getComputedStyle(element).textAlign)).toBe('center')
})

test('cột số tài sản của bảng vị trí thu gọn và căn giữa', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 900 })
  await page.goto('/dashboard/locations')

  const assetCountHeader = page.locator('th.location-asset-count-column')
  const assetCountHeaderContent = assetCountHeader.locator('.table-column-header')
  const assetCountCell = page.locator('td.location-asset-count-column').first()
  const assetCountBox = await assetCountHeader.boundingBox()

  await expect(assetCountHeader).toBeVisible()
  await expect(assetCountCell).toBeVisible()
  expect(assetCountBox).toBeTruthy()
  expect(Math.round(assetCountBox.width)).toBe(130)
  expect(await assetCountHeader.evaluate(element => getComputedStyle(element).textAlign)).toBe('center')
  expect(await assetCountHeaderContent.evaluate(element => getComputedStyle(element).justifyContent)).toBe('center')
  expect(await assetCountCell.evaluate(element => getComputedStyle(element).textAlign)).toBe('center')
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

test('quản lý xuất báo cáo yêu cầu vật tư theo bộ lọc hiện tại', async ({ page }) => {
  let exportUrl = ''
  await page.route('**/api/consumablerequest/export**', async route => {
    exportUrl = route.request().url()
    await route.fulfill({
      status: 200,
      contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      body: 'noi-dung-file-excel-e2e'
    })
  })
  await page.goto('/dashboard/consumable-requests')

  await page.locator('.filter-search input').fill('Điện trở')
  await page.locator('.status-filter').click()
  await page.locator('.ant-select-dropdown:visible .ant-select-item-option-content', { hasText: 'Chờ duyệt cấp phát' }).click()

  const downloadPromise = page.waitForEvent('download')
  await page.getByRole('button', { name: /Xuất báo cáo/ }).click()
  const download = await downloadPromise

  expect(download.suggestedFilename()).toMatch(/^BaoCaoYeuCauVatTu_\d+\.xlsx$/)
  const query = new URL(exportUrl).searchParams
  expect(query.get('search')).toBe('Điện trở')
  expect(query.get('status')).toBe('CONSUMABLE_PENDING')
})

test('bảng vật tư tiêu hao ẩn cột phụ và sắp đúng thứ tự cột chính', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 900 })
  await page.goto('/dashboard/devices?tab=consumables')

  const table = page.locator('.consumables-desktop-table')
  for (const columnName of ['Danh mục', 'Tổng tồn', 'Tồn tối thiểu', 'Đang giữ', 'Số lô']) {
    await expect(table.getByRole('columnheader', { name: new RegExp(columnName) })).toHaveCount(0)
  }
  for (const columnName of ['Mã vật tư', 'Tên vật tư', 'Đơn vị', 'Khả dụng', 'Người chịu trách nhiệm', 'Trạng thái', 'Hành động']) {
    await expect(table.getByRole('columnheader', { name: new RegExp(columnName) })).toBeVisible()
  }

  const headerTexts = (await table.locator('thead th').allTextContents()).map(text => text.trim())
  const columnIndex = name => headerTexts.findIndex(text => text.includes(name))
  expect(columnIndex('Người chịu trách nhiệm')).toBeLessThan(columnIndex('Khả dụng'))
  expect(columnIndex('Khả dụng')).toBeLessThan(columnIndex('Đơn vị'))
  expect(columnIndex('Đơn vị')).toBeLessThan(columnIndex('Trạng thái'))
})

test('bộ lọc danh mục vật tư bằng kích thước bộ lọc tình trạng tồn', async ({ page }) => {
  await page.setViewportSize({ width: 1920, height: 900 })
  await page.goto('/dashboard/devices?tab=consumables')

  const stockFilterBox = await page.locator('.stock-filter').boundingBox()
  const categoryFilterBox = await page.locator('.category-filter').boundingBox()

  expect(stockFilterBox).toBeTruthy()
  expect(categoryFilterBox).toBeTruthy()
  expect(Math.round(categoryFilterBox.width)).toBe(Math.round(stockFilterBox.width))
  expect(Math.round(categoryFilterBox.height)).toBe(Math.round(stockFilterBox.height))
  expect(Math.round(categoryFilterBox.width)).toBe(260)
  expect(Math.round(categoryFilterBox.height)).toBe(40)
})

test('báo cáo sai lệch hiển thị ảnh bằng chứng thu nhỏ thay cho số lượng ảnh', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/dashboard/handover-issues')

  const table = page.locator('.desktop-table')
  const thumbnail = table.locator('img[alt="Bằng chứng của Máy hiện sóng E2E"]')
  await expect(thumbnail).toBeVisible()
  const thumbnailBox = await thumbnail.boundingBox()
  expect(thumbnailBox).toBeTruthy()
  expect(Math.round(thumbnailBox.width)).toBe(56)
  expect(Math.round(thumbnailBox.height)).toBe(42)
  await expect(table.getByText('1 ảnh', { exact: true })).toHaveCount(0)
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
