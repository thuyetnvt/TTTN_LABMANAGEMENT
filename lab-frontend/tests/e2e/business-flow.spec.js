import { test, expect } from '@playwright/test'

const businessFlowEnabled = process.env.E2E_BUSINESS_FLOW === '1'
const frontendBaseUrl = process.env.E2E_BASE_URL || 'http://localhost:8081'
const apiBaseUrl = `${frontendBaseUrl.replace(/\/$/, '')}/api`

test('luồng mượn nhiều tài sản, bàn giao, trả, bảo trì và kiểm kê QR', async ({ page, request }) => {
  test.setTimeout(180000)
  test.skip(!businessFlowEnabled, 'Đặt E2E_BUSINESS_FLOW=1 để chạy flow nghiệp vụ có ghi dữ liệu test.')

  const password = process.env.E2E_BUSINESS_PASSWORD || process.env.E2E_ADMIN_PASSWORD
  const adminUsername = process.env.E2E_ADMIN_USERNAME || 'admin'
  const studentUsername = process.env.E2E_STUDENT_USERNAME || 'sv1'
  const teacherUsername = process.env.E2E_TEACHER_USERNAME || 'giangvien1'
  const managerUsername = process.env.E2E_MANAGER_USERNAME || 'truonglab'
  if (!password) {
    throw new Error('Thiếu E2E_BUSINESS_PASSWORD hoặc E2E_ADMIN_PASSWORD.')
  }

  const loginApi = async (username) => {
    for (let attempt = 0; attempt < 2; attempt += 1) {
      const response = await request.post(`${apiBaseUrl}/auth/login`, {
        data: { username, password }
      })
      if (response.status() === 429 && attempt === 0) {
        await new Promise(resolve => setTimeout(resolve, 61000))
        continue
      }
      expect(response.ok(), await response.text()).toBeTruthy()
      return response.json()
    }
    throw new Error(`Không thể đăng nhập E2E cho ${username}.`)
  }

  const admin = await loginApi(adminUsername)
  const student = await loginApi(studentUsername)
  const teacher = await loginApi(teacherUsername)
  const manager = await loginApi(managerUsername)
  const headers = token => ({ Authorization: `Bearer ${token}` })

  const usersResponse = await request.get(`${apiBaseUrl}/users`, { headers: headers(admin.token) })
  expect(usersResponse.ok(), await usersResponse.text()).toBeTruthy()
  const users = await usersResponse.json()
  const teacherUser = users.find(user => user.username === teacherUsername)
  expect(teacherUser, `Không tìm thấy người dùng ${teacherUsername}`).toBeTruthy()

  const locationsResponse = await request.get(`${apiBaseUrl}/location`, { headers: headers(admin.token) })
  expect(locationsResponse.ok(), await locationsResponse.text()).toBeTruthy()
  const locations = await locationsResponse.json()
  const location = locations.find(item => item.isActive)
  expect(location, 'Cần ít nhất một vị trí đang hoạt động để chạy E2E.').toBeTruthy()

  const categoryName = `E2E IoT ${Date.now()}`
  const categoryResponse = await request.post(`${apiBaseUrl}/assetcategory`, {
    headers: headers(admin.token),
    data: { name: categoryName, description: 'Dữ liệu phục vụ E2E, không dùng cho production.' }
  })
  expect(categoryResponse.ok(), await categoryResponse.text()).toBeTruthy()
  const category = await categoryResponse.json()

  const createEquipment = async (suffix) => {
    const serial = `E2E-SN-${Date.now()}-${suffix}`
    const response = await request.post(`${apiBaseUrl}/equipment`, {
      headers: headers(admin.token),
      multipart: {
        assetCode: `E2E-${Date.now()}-${suffix}`,
        name: `Thiết bị E2E ${suffix}`,
        model: 'E2E-MODEL',
        serial,
        serialName: serial,
        location: location.name,
        locationNodeId: String(location.id),
        assetCategoryId: String(category.id),
        responsiblePerson: 'Đội kiểm thử',
        decisionFile: {
          name: 'quyet-dinh-e2e.pdf',
          mimeType: 'application/pdf',
          buffer: Buffer.from('%PDF-1.4\n1 0 obj\n<<>>\nendobj\n%%EOF')
        }
      }
    })
    expect(response.ok(), await response.text()).toBeTruthy()
    return response.json()
  }

  const equipment = [await createEquipment('A'), await createEquipment('B')]

  await loginUi(page, '/dashboard/devices', studentUsername, password)
  await expect(page.getByRole('heading', { name: 'Thiết bị & Tài sản' })).toBeVisible()
  await expect(page.getByRole('cell', { name: equipment[0].name }).first()).toBeVisible()

  const borrowResponse = await request.post(`${apiBaseUrl}/borrow`, {
    headers: headers(student.token),
    data: {
      expectedReturnDate: new Date(Date.now() + 7 * 86400000).toISOString(),
      purpose: 'Kiểm thử luồng mượn nhiều tài sản',
      teacherId: teacherUser.id,
      items: equipment.map(item => ({ equipmentId: item.id, note: 'Kiểm thử E2E' }))
    }
  })
  expect(borrowResponse.ok(), await borrowResponse.text()).toBeTruthy()
  const borrow = await borrowResponse.json()

  await page.goto('/dashboard/borrow-history')
  await expect(page.getByRole('heading', { name: 'Lịch sử mượn/trả' })).toBeVisible()
  const teacherApproval = await request.put(`${apiBaseUrl}/borrow/${borrow.id}/teacher-approve`, {
    headers: headers(teacher.token),
    data: { note: 'Đã kiểm tra mục đích thực hành.' }
  })
  expect(teacherApproval.ok(), await teacherApproval.text()).toBeTruthy()

  const managerApproval = await request.put(`${apiBaseUrl}/borrow/${borrow.id}/approve`, {
    headers: headers(manager.token)
  })
  expect(managerApproval.ok(), await managerApproval.text()).toBeTruthy()

  const handoverResponse = await request.post(`${apiBaseUrl}/handover`, {
    headers: headers(manager.token),
    data: {
      borrowRecordId: borrow.id,
      notes: 'Bàn giao đủ thiết bị và phụ kiện theo E2E.',
      items: equipment.map(item => ({
        equipmentId: item.id,
        condition: 'AVAILABLE',
        accessories: 'Nguồn, cáp USB',
        note: 'Đủ phụ kiện'
      }))
    }
  })
  expect(handoverResponse.ok(), await handoverResponse.text()).toBeTruthy()

  const returnResponse = await request.put(`${apiBaseUrl}/borrow/${borrow.id}/return`, {
    headers: headers(manager.token),
    data: {
      items: equipment.map(item => ({
        equipmentId: item.id,
        condition: 'AVAILABLE',
        note: 'Đã nhận đủ, hoạt động bình thường.',
        compensationAmount: 0
      }))
    }
  })
  expect(returnResponse.ok(), await returnResponse.text()).toBeTruthy()

  const maintenanceResponse = await request.post(`${apiBaseUrl}/maintenance`, {
    headers: headers(manager.token),
    data: {
      equipmentId: equipment[0].id,
      description: 'Kiểm tra sau E2E',
      performedBy: 'Kỹ thuật viên E2E',
      supplier: 'Nội bộ',
      checklist: 'Nguồn; kết nối',
      cost: 0
    }
  })
  expect(maintenanceResponse.ok(), await maintenanceResponse.text()).toBeTruthy()
  const maintenance = await maintenanceResponse.json()
  const completeMaintenance = await request.put(`${apiBaseUrl}/maintenance/${maintenance.id}/complete`, {
    headers: headers(manager.token),
    data: {
      result: 'Thiết bị hoạt động bình thường.',
      nextEquipmentStatus: 'AVAILABLE',
      checklistResult: 'Đạt'
    }
  })
  expect(completeMaintenance.ok(), await completeMaintenance.text()).toBeTruthy()

  const inventoryResponse = await request.post(`${apiBaseUrl}/inventory`, {
    headers: headers(manager.token),
    data: { name: `Kiểm kê E2E ${Date.now()}`, assetCategoryId: category.id }
  })
  expect(inventoryResponse.ok(), await inventoryResponse.text()).toBeTruthy()
  const inventory = await inventoryResponse.json()
  for (const item of equipment) {
    const scanResponse = await request.post(`${apiBaseUrl}/inventory/${inventory.id}/scan`, {
      headers: headers(manager.token),
      data: { qrToken: item.qrToken, status: 'INVENTORY_FOUND', note: 'Đã quét bằng QR trong E2E.' }
    })
    expect(scanResponse.ok(), await scanResponse.text()).toBeTruthy()
  }
  const completeInventory = await request.post(`${apiBaseUrl}/inventory/${inventory.id}/complete`, {
    headers: headers(manager.token)
  })
  expect(completeInventory.ok(), await completeInventory.text()).toBeTruthy()
})

async function loginUi(page, route, username, password) {
  for (let attempt = 0; attempt < 2; attempt += 1) {
    await page.goto('/login')
    await page.getByLabel(/tài khoản/i).fill(username)
    await page.getByLabel(/mật khẩu/i).fill(password)
    const responsePromise = page.waitForResponse(
      response => response.url().endsWith('/api/auth/login'),
      { timeout: 15000 }
    )
    await page.getByRole('button', { name: 'Đăng nhập' }).click()
    const response = await responsePromise
    if (response.status() === 429 && attempt === 0) {
      await page.waitForTimeout(61000)
      continue
    }
    expect(response.ok(), await response.text()).toBeTruthy()
    break
  }
  await expect(page).toHaveURL(/\/dashboard/)
  await page.goto(route)
}
