import test from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { createPinia, setActivePinia } from 'pinia'
import { ROLE_LABELS, STATUS, normalizeStatus, roleLabel, statusLabel, statusMatches } from '../src/constants/business.js'
import { useNotificationStore } from '../src/stores/notificationStore.js'
import { getApiErrorMessage, getApiSuccessMessage } from '../src/utils/apiError.js'
import { getDashboardAlertTarget } from '../src/utils/dashboardAlerts.js'
import {
  getBorrowStatusLabel,
  getConsumableRequestStatusLabel,
  getEquipmentStatusLabel,
  getInventoryStatusLabel,
  getMaintenanceStatusLabel,
  getPenaltyStatusLabel,
  getReturnConditionLabel,
  getStatusColor
} from '../src/utils/statusLabels.js'
import { formatVietnamDateInput, formatVietnamDateTime, vietnamDateInputToUtc } from '../src/utils/dateTime.js'
import { createTablePagination, TABLE_PAGE_SIZE, TABLE_PAGE_SIZE_OPTIONS } from '../src/utils/tablePagination.js'

const TABLE_FILES_WITH_STICKY_ACTION = [
  '../src/views/BorrowRequestsView.vue',
  '../src/views/BorrowHistoryView.vue',
  '../src/views/ConsumableRequestsView.vue',
  '../src/views/InventoryView.vue',
  '../src/views/LocationsView.vue',
  '../src/components/DeviceTable.vue',
  '../src/components/ConsumablesTable.vue',
  '../src/components/AssetCategoriesTable.vue',
  '../src/components/UserTable.vue',
  '../src/views/MaintenanceView.vue',
  '../src/views/MaintenanceSchedulesView.vue',
  '../src/views/PenaltyView.vue',
  '../src/views/TeacherApprovalView.vue'
]

test('ghim cùng một cột hành động cho cả header và body của mọi bảng thao tác', () => {
  for (const relativePath of TABLE_FILES_WITH_STICKY_ACTION) {
    const source = readFileSync(new URL(relativePath, import.meta.url), 'utf8')

    assert.doesNotMatch(source, /fixed\s*:\s*['"]right['"]/, relativePath)
    assert.match(source, /className\s*:\s*['"]table-sticky-action-column['"]/, relativePath)
    assert.match(source, /customCell\s*:\s*\(\)\s*=>\s*\(\{\s*class:\s*['"]table-sticky-action-column['"]\s*\}\)/, relativePath)
  }

  const globalStyle = readFileSync(new URL('../src/style.css', import.meta.url), 'utf8')
  assert.match(globalStyle, /\.table-sticky-action-column\s*\{[\s\S]*?position:\s*sticky\s*!important;/)
  assert.match(globalStyle, /\.table-sticky-action-column\s*\{[\s\S]*?right:\s*0\s*!important;/)
})

test('dùng chung phân trang 20 dòng và cho phép đổi số dòng', () => {
  const pagination = createTablePagination()

  assert.equal(TABLE_PAGE_SIZE, 20)
  assert.deepEqual(TABLE_PAGE_SIZE_OPTIONS, ['10', '20', '50', '100'])
  assert.equal(pagination.defaultPageSize, 20)
  assert.equal(pagination.showSizeChanger, true)
  assert.equal(pagination.hideOnSinglePage, false)
  assert.deepEqual(pagination.position, ['bottomRight'])
})

test('ánh xạ vai trò và trạng thái sang tiếng Việt', () => {
  assert.equal(roleLabel('Admin'), 'Quản trị viên')
  assert.equal(statusLabel(STATUS.BORROWED), 'Đang mượn')
  assert.equal(statusLabel('Hoàn tất'), 'Đã hoàn thành bảo trì')
  assert.equal(getReturnConditionLabel(STATUS.AVAILABLE), 'Bình thường')
  assert.equal(getReturnConditionLabel(STATUS.BROKEN), 'Hỏng')
  assert.equal(ROLE_LABELS.STUDENT, 'Sinh viên')
})

test('hiển thị timestamp hoạt động theo múi giờ Việt Nam', () => {
  assert.equal(
    formatVietnamDateTime('2026-08-27T03:47:57'),
    '10:47:57 27/8/2026'
  )
  assert.equal(
    formatVietnamDateTime('2026-08-27T03:47:57Z'),
    '10:47:57 27/8/2026'
  )
  assert.equal(formatVietnamDateInput('2026-08-29T17:00:00Z'), '2026-08-30')
  assert.equal(vietnamDateInputToUtc('2026-08-30'), '2026-08-29T17:00:00.000Z')
})

test('chuẩn hóa trạng thái cũ nhưng giữ mã ổn định', () => {
  assert.equal(normalizeStatus('Rảnh'), STATUS.AVAILABLE)
  assert.equal(normalizeStatus('Đang mượn'), STATUS.BORROWED)
  assert.equal(statusMatches('Đang mượn', STATUS.BORROWED), true)
  assert.equal(statusMatches(STATUS.BROKEN, STATUS.AVAILABLE), false)
})

test('nhật ký ẩn mã nội bộ và luôn có nhãn người thao tác', () => {
  const source = readFileSync(new URL('../src/views/AuditLogsView.vue', import.meta.url), 'utf8')

  assert.doesNotMatch(source, /title:\s*['"]Mã['"]\s*,\s*dataIndex:\s*['"]entityId['"]/)
  assert.match(source, /if \(!normalized\) return ['"]Không xác định['"]/)
  assert.match(source, /normalized\.toLowerCase\(\) === ['"]system['"] \? ['"]Hệ thống['"]/)
})

test('mọi trạng thái nghiệp vụ đều có nhãn và màu rõ ràng', () => {
  const cases = [
    [getEquipmentStatusLabel, STATUS.BORROW_PENDING, 'Đã giữ chỗ', 'orange'],
    [getEquipmentStatusLabel, STATUS.MISSING, 'Thất lạc', 'red'],
    [getBorrowStatusLabel, STATUS.BORROW_PENDING, 'Chờ duyệt', 'orange'],
    [getBorrowStatusLabel, STATUS.APPROVAL_PROCESSING, 'Đang xử lý duyệt', 'blue'],
    [getBorrowStatusLabel, STATUS.RETURN_PROCESSING, 'Đang xử lý trả', 'blue'],
    [getBorrowStatusLabel, STATUS.CANCELLED, 'Đã hủy', 'red'],
    [getBorrowStatusLabel, STATUS.EXPIRED, 'Hết hạn giữ chỗ', 'orange'],
    [getMaintenanceStatusLabel, STATUS.MAINTENANCE_IN_PROGRESS, 'Đang bảo trì', 'blue'],
    [getMaintenanceStatusLabel, STATUS.MAINTENANCE_COMPLETING, 'Đang nghiệm thu', 'purple'],
    [getConsumableRequestStatusLabel, STATUS.CONSUMABLE_PENDING, 'Chờ duyệt cấp phát', 'orange'],
    [getConsumableRequestStatusLabel, STATUS.CONSUMABLE_ISSUED, 'Đã cấp phát', 'green'],
    [getPenaltyStatusLabel, STATUS.UNPAID, 'Chưa thanh toán', 'red'],
    [getPenaltyStatusLabel, STATUS.PAID, 'Đã thanh toán', 'green'],
    [getInventoryStatusLabel, STATUS.INVENTORY_DAMAGED, 'Hư hỏng', 'red'],
    [getInventoryStatusLabel, STATUS.INVENTORY_WRONG_LOCATION, 'Sai vị trí', 'orange']
  ]

  for (const [labeler, status, expectedLabel, expectedColor] of cases) {
    assert.equal(labeler(status), expectedLabel, status)
    assert.equal(getStatusColor(status), expectedColor, status)
  }
})

test('kiểm kê hiển thị đã đối soát cho tài sản đã quét bình thường', () => {
  const source = readFileSync(new URL('../src/views/InventoryView.vue', import.meta.url), 'utf8')

  assert.match(source, /record\.reviewedAt \|\| isScannedNormally\(record\)/)
  assert.match(source, /const isScannedNormally = record => record\.status === STATUS\.INVENTORY_FOUND && Boolean\(record\.scannedAt\)/)
})

test('landing page dùng ảnh nội bộ để không phụ thuộc URL ảnh ngoài', () => {
  const source = readFileSync(new URL('../src/views/LandingView.vue', import.meta.url), 'utf8')

  assert.doesNotMatch(source, /images\.unsplash\.com/)
  assert.match(source, /src="\/lab-bg\.png"/)
  assert.match(source, /image: '\/hero\.png'/)
  assert.match(source, /image: '\/lab-bg\.png'/)
  assert.match(source, /image: '\/feature\.png'/)
})

test('luồng hủy phiếu có nút và lý do ở cả người mượn và quản lý', () => {
  const historySource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const requestsSource = readFileSync(new URL('../src/views/BorrowRequestsView.vue', import.meta.url), 'utf8')
  const apiSource = readFileSync(new URL('../src/api/borrowApi.js', import.meta.url), 'utf8')

  assert.match(historySource, /record\.canCancel/)
  assert.match(historySource, /cancelReason/)
  assert.match(requestsSource, /openCancelModal/)
  assert.match(requestsSource, /Tài sản đang giữ chỗ sẽ được trả về trạng thái sẵn sàng/)
  assert.match(apiSource, /cancel:\s*\(id, reason\)\s*=>\s*axiosClient\.put\(`\/borrow\/\$\{id\}\/cancel`/)
})

test('tách xử lý trả khỏi phiếu chờ duyệt và đưa sang lịch sử', () => {
  const historySource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const requestsSource = readFileSync(new URL('../src/views/BorrowRequestsView.vue', import.meta.url), 'utf8')
  const modalSource = readFileSync(new URL('../src/components/ReturnInspectionModal.vue', import.meta.url), 'utf8')

  assert.doesNotMatch(requestsSource, /showReturnModal|handleRemind|Kiểm tra tài sản khi trả/)
  assert.match(historySource, /ReturnInspectionModal/)
  assert.match(historySource, /Kiểm tra trả/)
  assert.match(historySource, /Nhắc trả/)
  assert.match(modalSource, /borrowApi\.returnEquipment/)
  assert.match(modalSource, /borrowApi\.uploadReturnEvidence/)
  assert.match(modalSource, /overduePenaltyAmount/)
  assert.match(modalSource, /tự động chuyển sang Đã thanh toán/)
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

test('điều hướng cảnh báo Dashboard đến đúng màn hình', () => {
  assert.deepEqual(getDashboardAlertTarget('overdue'), { name: 'BorrowHistory', query: { status: 'OVERDUE' } })
  assert.deepEqual(getDashboardAlertTarget('low-stock'), { name: 'Devices', query: { tab: 'consumables', stock: 'LOW_STOCK' } })
  assert.deepEqual(getDashboardAlertTarget('pending-borrow-requests'), { name: 'BorrowRequests' })
  assert.deepEqual(getDashboardAlertTarget('pending-consumable-requests'), { name: 'ConsumableRequests' })
  assert.deepEqual(getDashboardAlertTarget('warranty-soon'), { name: 'Devices', query: { status: 'warranty-soon' } })
  assert.deepEqual(getDashboardAlertTarget('teacher-pending-approvals'), { name: 'TeacherApproval' })
  assert.equal(getDashboardAlertTarget('unknown'), null)
})

test('dashboard giảng viên dùng dữ liệu và tác vụ riêng theo vai trò', () => {
  const source = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')

  assert.match(source, /v-else-if="isTeacher"/)
  assert.match(source, /Không gian giảng viên/)
  assert.match(source, /Chờ bạn bảo lãnh/)
  assert.match(source, /teacherSummary/)
  assert.match(source, /name:\s*'TeacherApproval'/)
})

test('dashboard sinh viên dùng thống kê cá nhân, không dùng số liệu toàn lab', () => {
  const source = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')

  assert.match(source, /v-else-if="isStudent"/)
  assert.match(source, /Không gian sinh viên/)
  assert.match(source, /studentSummary/)
  assert.match(source, /Tình trạng phiếu mượn của bạn/)
  assert.match(source, /const studentStats[\s\S]*?studentSummary\.value\.activeBorrows/)
  assert.doesNotMatch(source, /label:\s*'Thiết bị rảnh'/)
})

test('dashboard có skeleton ban đầu và cho phép làm mới bỏ qua cache', () => {
  const viewSource = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const apiSource = readFileSync(new URL('../src/api/dashboardApi.js', import.meta.url), 'utf8')

  assert.match(viewSource, /v-if="initialLoading"/)
  assert.match(viewSource, /<a-skeleton/)
  assert.match(viewSource, /refreshStats\(true\)/)
  assert.match(apiSource, /params:\s*refresh\s*\?\s*\{\s*refresh:\s*true\s*\}/)
})

test('API không bị quay vô hạn khi backend không phản hồi', () => {
  const source = readFileSync(new URL('../src/api/axiosClient.js', import.meta.url), 'utf8')

  assert.match(source, /timeout:\s*apiTimeoutMs/)
  assert.match(source, /Máy chủ phản hồi quá lâu/)
})

test('Vite cho phép đổi đích proxy API khi kiểm thử VPS hoặc backend local', () => {
  const source = readFileSync(new URL('../vite.config.js', import.meta.url), 'utf8')

  assert.match(source, /VITE_DEV_API_PROXY_TARGET/)
  assert.match(source, /proxyTarget/)
})
