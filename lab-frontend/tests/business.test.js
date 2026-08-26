import test from 'node:test'
import assert from 'node:assert/strict'
import { createPinia, setActivePinia } from 'pinia'
import { ROLE_LABELS, STATUS, normalizeStatus, roleLabel, statusLabel, statusMatches } from '../src/constants/business.js'
import { useNotificationStore } from '../src/stores/notificationStore.js'
import { getApiErrorMessage, getApiSuccessMessage } from '../src/utils/apiError.js'
import { getDashboardAlertTarget } from '../src/utils/dashboardAlerts.js'
import { getExportErrorMessage } from '../src/utils/reportsExport.js'
import {
  getBorrowStatusLabel,
  getConsumableRequestStatusLabel,
  getEquipmentStatusLabel,
  getInventoryStatusLabel,
  getMaintenanceStatusLabel,
  getPenaltyStatusLabel,
  getReturnConditionLabel
} from '../src/utils/statusLabels.js'

test('ánh xạ vai trò và trạng thái sang tiếng Việt', () => {
  assert.equal(roleLabel('Admin'), 'Quản trị viên')
  assert.equal(getEquipmentStatusLabel(STATUS.AVAILABLE), 'Rảnh')
  assert.equal(getEquipmentStatusLabel(STATUS.BORROW_PENDING), 'Chờ mượn')
  assert.equal(getBorrowStatusLabel(STATUS.BORROW_PENDING), 'Chờ duyệt')
  assert.equal(getMaintenanceStatusLabel('COMPLETING'), 'Đang nghiệm thu')
  assert.equal(getInventoryStatusLabel('MISSING'), 'Thất lạc')
  assert.equal(getConsumableRequestStatusLabel('ISSUED'), 'Đã cấp phát')
  assert.equal(getPenaltyStatusLabel('PAID'), 'Đã thanh toán')
  assert.equal(getReturnConditionLabel('SCRATCHED'), 'Trầy xước')
  assert.equal(getEquipmentStatusLabel('UNKNOWN_ENUM'), 'Không xác định')
  assert.equal(statusLabel(STATUS.BORROWED), 'Đang mượn')
  assert.equal(ROLE_LABELS.STUDENT, 'Sinh viên')
})

test('chuẩn hóa trạng thái cũ nhưng giữ mã ổn định', () => {
  assert.equal(normalizeStatus('Rảnh'), STATUS.AVAILABLE)
  assert.equal(normalizeStatus('Đang mượn'), STATUS.BORROWED)
  assert.equal(statusMatches('Đang mượn', STATUS.BORROWED), true)
  assert.equal(statusMatches(STATUS.BROKEN, STATUS.AVAILABLE), false)
})

test('notification store dedupe realtime và chỉ tăng unread một lần', () => {
  setActivePinia(createPinia())
  const store = useNotificationStore()
  const payload = { id: 9001, type: 'BORROW_PENDING', title: 'Yêu cầu mới', message: 'Kiểm thử', url: '' }

  assert.equal(store.handleRealtimeNotification(payload), true)
  assert.equal(store.handleRealtimeNotification(payload), false)
  assert.equal(store.unreadCount, 1)
  assert.equal(store.items.length, 1)
})

test('lấy message backend khi gửi nhắc trả thành công', () => {
  assert.equal(
    getApiSuccessMessage({ message: 'SMTP đã gửi email.' }, 'Đã gửi email nhắc trả thành công.'),
    'SMTP đã gửi email.'
  )
  assert.equal(
    getApiSuccessMessage({}, 'Đã gửi email nhắc trả thành công.'),
    'Đã gửi email nhắc trả thành công.'
  )
})

test('không hiển thị [object Object] khi response lỗi là object', () => {
  assert.equal(
    getApiErrorMessage({ response: { data: { message: 'Người mượn chưa có email.' } } }, 'Không thể gửi nhắc trả.'),
    'Người mượn chưa có email.'
  )
  assert.equal(
    getApiErrorMessage({ response: { data: { code: 'SMTP_NOT_CONFIGURED' } }, message: 'SMTP chưa cấu hình.' }, 'Không thể gửi nhắc trả.'),
    'SMTP chưa cấu hình.'
  )
  assert.equal(
    getApiErrorMessage({ response: { data: { message: { detail: 'not-a-string' } } }, message: 'Mất kết nối.' }, 'Không thể gửi nhắc trả.'),
    'Mất kết nối.'
  )
  assert.equal(
    getApiErrorMessage({ response: { data: { errors: { Password: ['Mật khẩu phải có ít nhất 8 ký tự.'] } } } }, 'Dữ liệu không hợp lệ.'),
    'Mật khẩu phải có ít nhất 8 ký tự.'
  )
})

test('đọc được message thật khi API export trả lỗi dạng blob JSON', async () => {
  const error = {
    message: 'Máy chủ đang gặp sự cố.',
    response: {
      data: new Blob([JSON.stringify({ message: 'Không thể tạo file Excel do dữ liệu không hợp lệ.' })], {
        type: 'application/problem+json'
      })
    }
  }

  assert.equal(
    await getExportErrorMessage(error),
    'Không thể tạo file Excel do dữ liệu không hợp lệ.'
  )
})

test('điều hướng cảnh báo Dashboard đến đúng màn hình', () => {
  assert.deepEqual(getDashboardAlertTarget('overdue'), { name: 'BorrowRequests' })
  assert.deepEqual(getDashboardAlertTarget('low-stock'), { name: 'Devices', query: { tab: 'consumables' } })
  assert.deepEqual(getDashboardAlertTarget('pending-borrow-requests'), { name: 'BorrowRequests' })
  assert.deepEqual(getDashboardAlertTarget('pending-consumable-requests'), { name: 'ConsumableRequests' })
  assert.deepEqual(getDashboardAlertTarget('warranty-soon'), { name: 'Devices', query: { status: 'warranty' } })
  assert.equal(getDashboardAlertTarget('unknown'), null)
})
