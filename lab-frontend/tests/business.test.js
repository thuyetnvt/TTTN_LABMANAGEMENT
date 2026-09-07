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

test('bộ lọc cột hiển thị đúng biểu tượng và nằm sát mép phải tiêu đề', () => {
  const source = readFileSync(new URL('../src/components/TableColumnFilter.vue', import.meta.url), 'utf8')

  assert.match(source, /<SearchOutlined v-if="type === 'search'" \/>/)
  assert.match(source, /<FilterOutlined v-else \/>/)
  assert.match(source, /class="table-column-controls table-column-filter-control"/)
  assert.match(source, /placement="bottomRight"/)
  assert.match(source, /\.table-column-controls\s*\{[\s\S]*?margin-left:\s*auto;/)
})

test('tiêu đề cột có mũi tên tăng giảm và truyền sắp xếp về API phân trang', () => {
  const filterSource = readFileSync(new URL('../src/components/TableColumnFilter.vue', import.meta.url), 'utf8')
  const deviceSource = readFileSync(new URL('../src/components/DeviceTable.vue', import.meta.url), 'utf8')

  assert.match(filterSource, /CaretUpOutlined/)
  assert.match(filterSource, /CaretDownOutlined/)
  assert.match(filterSource, /@click\.stop="applySort\('ascend'\)"/)
  assert.match(filterSource, /@click\.stop="applySort\('descend'\)"/)
  assert.match(deviceSource, /sortBy:\s*sortState\.field/)
  assert.match(deviceSource, /sortDirection:\s*sortState\.order === 'descend'/)
  assert.match(deviceSource, /sortKey:\s*'name',\s*sortable:\s*true/)
})

test('mọi bảng nghiệp vụ của các vai trò đều có lọc hoặc sắp xếp theo cột', () => {
  const tableFiles = [
    '../src/views/BorrowHistoryView.vue',
    '../src/views/BorrowRequestsView.vue',
    '../src/views/TeacherApprovalView.vue',
    '../src/views/ConsumableRequestsView.vue',
    '../src/views/InventoryView.vue',
    '../src/views/MaintenanceView.vue',
    '../src/views/MaintenanceSchedulesView.vue',
    '../src/views/AuditLogsView.vue',
    '../src/views/ApprovalDelegationsView.vue',
    '../src/views/LocationsView.vue',
    '../src/views/ReportsView.vue',
    '../src/components/DeviceTable.vue',
    '../src/components/ConsumablesTable.vue',
    '../src/components/AssetCategoriesTable.vue',
    '../src/components/UserTable.vue'
  ]

  for (const relativePath of tableFiles) {
    const source = readFileSync(new URL(relativePath, import.meta.url), 'utf8')
    assert.match(source, /sortable:\s*true/, relativePath)
    assert.match(source, /@sort=|emit\('sort'/, relativePath)
  }
})

test('thanh thao tác không làm tràn khung nội dung khi màn hình hẹp', () => {
  const deviceSource = readFileSync(new URL('../src/components/DeviceTable.vue', import.meta.url), 'utf8')
  const shellSource = readFileSync(new URL('../src/views/DashboardView.vue', import.meta.url), 'utf8')

  assert.match(deviceSource, /\.table-actions\s*\{[\s\S]*?flex-wrap:\s*wrap;/)
  assert.match(deviceSource, /\.left-actions\s*\{[\s\S]*?min-width:\s*0;/)
  assert.match(deviceSource, /@media \(max-width: 767px\)[\s\S]*?\.left-actions > \*,[\s\S]*?width: 100% !important;/)
  assert.match(shellSource, /\.dashboard-content\s*\{[\s\S]*?overflow-x:\s*hidden;/)
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
    [getInventoryStatusLabel, STATUS.INVENTORY_DAMAGED, 'Hư hỏng', 'red'],
    [getInventoryStatusLabel, STATUS.INVENTORY_WRONG_LOCATION, 'Sai vị trí', 'orange']
  ]

  for (const [labeler, status, expectedLabel, expectedColor] of cases) {
    assert.equal(labeler(status), expectedLabel, status)
    assert.equal(getStatusColor(status), expectedColor, status)
  }
})

test('báo cáo hiển thị đang mượn và tài sản hỏng riêng', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.match(source, /label:\s*['"]Đang mượn['"][^]*?value:\s*formatNumber\(report\.value\.totals\.borrowed\)/)
  assert.match(source, /label:\s*['"]Đang hỏng['"][^]*?value:\s*formatNumber\(report\.value\.totals\.broken\)/)
})

test('báo cáo không hiển thị dải bộ lọc đang áp dụng hoặc nút lọc màu cam', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.doesNotMatch(source, /Bộ lọc đang áp dụng|FilterOutlined|@click="applyFilters"/)
  assert.match(source, /const resetFilters = \(\) =>/)
  assert.match(source, /watch\(filterForm, value =>/)
})

test('báo cáo giữ cả phiếu đang xử lý trả trong danh sách đang mượn', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.match(source, /record\.processingReturn\s*\?\s*'orange'/)
  assert.match(source, /record\.processingReturn\s*\?\s*'Đang xử lý trả'/)
})

test('báo cáo đặt chi tiết vận hành lên trước và thay bảo trì bằng người chịu trách nhiệm', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.ok(source.indexOf('class="detail-card"') < source.indexOf('class="main-grid"'))
  assert.match(source, /key="responsible"\s+tab="Người chịu trách nhiệm"/)
  assert.match(source, /responsibleColumns/)
  assert.doesNotMatch(source, /key="maintenance"\s+tab="Bảo trì"/)
})

test('báo cáo mở danh sách thiết bị đúng trạng thái ngay trong modal', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.match(source, /@click="openStatusDetails\(item\)"/)
  assert.match(source, /const openStatusDetails = async item =>/)
  assert.match(source, /equipmentApi\.getPaged\(\{ page: 1, pageSize: 100, status: item\.value \}\)/)
  assert.match(source, /v-model:open="statusDetailsVisible"/)
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
