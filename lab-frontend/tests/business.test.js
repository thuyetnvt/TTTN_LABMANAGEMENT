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
  '../src/views/ApprovalDelegationsView.vue',
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

test('tiêu đề cột của toàn bộ bảng không tự xuống hàng', () => {
  const globalStyle = readFileSync(new URL('../src/style.css', import.meta.url), 'utf8')
  const filterSource = readFileSync(new URL('../src/components/TableColumnFilter.vue', import.meta.url), 'utf8')
  const consumablesSource = readFileSync(new URL('../src/components/ConsumablesTable.vue', import.meta.url), 'utf8')
  const maintenanceSource = readFileSync(new URL('../src/views/MaintenanceView.vue', import.meta.url), 'utf8')

  assert.match(globalStyle, /\.ant-table-wrapper \.ant-table-thead > tr > th\s*\{[\s\S]*?white-space:\s*nowrap;/)
  assert.match(filterSource, /\.table-column-title\s*\{[\s\S]*?white-space:\s*nowrap;/)
  assert.match(consumablesSource, /\.consumables-desktop-table :deep\(\.ant-table-thead > tr > th\)[\s\S]*?white-space:\s*nowrap;/)
  assert.match(maintenanceSource, /title: 'Ngày thực hiện',[\s\S]*?width: 175/)
  assert.match(maintenanceSource, /title: 'Người thực hiện',[\s\S]*?width: 220/)
})

test('vị trí lưu vật tư được chọn từ cây vị trí có sẵn', () => {
  const source = readFileSync(new URL('../src/components/ConsumablesTable.vue', import.meta.url), 'utf8')
  const locationSelectSource = readFileSync(new URL('../src/components/LocationTreeSelect.vue', import.meta.url), 'utf8')

  assert.match(source, /import LocationTreeSelect from '\.\/LocationTreeSelect\.vue'/)
  assert.match(source, /import \{ locationApi \} from '\.\.\/api\/locationApi'/)
  assert.match(source, /locations\.value = await locationApi\.getAll\(\)/)
  assert.match(source, /v-model:value="formStorageLocationNodeId"[\s\S]*?@change="syncFormStorageLocation"/)
  assert.match(source, /v-model:value="lotStorageLocationNodeId"[\s\S]*?@change="syncLotStorageLocation"/)
  assert.doesNotMatch(source, /<a-input v-model:value="(?:formData|lotForm)\.storageLocation"/)
  assert.match(locationSelectSource, /title:\s*node\.name/)
  assert.doesNotMatch(locationSelectSource, /node\.code[^\n]*node\.name/)
})

test('vị trí được quản lý dạng phẳng và không còn trường vị trí cha', () => {
  const viewSource = readFileSync(new URL('../src/views/LocationsView.vue', import.meta.url), 'utf8')
  const locationSelectSource = readFileSync(new URL('../src/components/LocationTreeSelect.vue', import.meta.url), 'utf8')
  const apiSource = readFileSync(new URL('../src/api/locationApi.js', import.meta.url), 'utf8')

  assert.match(viewSource, /Danh sách vị trí tài sản/)
  assert.doesNotMatch(viewSource, /Vị trí cha|parentOptions|parentName|form\.parentId/)
  assert.doesNotMatch(locationSelectSource, /parentId|children:/)
  assert.match(apiSource, /LAB-ROOT/)
  assert.match(apiSource, /withoutParent/)
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

test('tiêu đề cột không bị rút gọn và bảng tự giữ đủ độ rộng', () => {
  const filterSource = readFileSync(new URL('../src/components/TableColumnFilter.vue', import.meta.url), 'utf8')
  const dashboardSource = readFileSync(new URL('../src/views/DashboardView.vue', import.meta.url), 'utf8')
  const consumableRequestSource = readFileSync(new URL('../src/views/ConsumableRequestsView.vue', import.meta.url), 'utf8')
  const borrowHistorySource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const deviceSource = readFileSync(new URL('../src/components/DeviceTable.vue', import.meta.url), 'utf8')

  assert.match(filterSource, /class="table-column-title" :title="title"/)
  assert.match(filterSource, /\.table-column-title\s*\{[\s\S]*?text-overflow:\s*clip;/)
  assert.match(dashboardSource, /<a-menu-item-group v-if="isManagerRole\(role\) \|\| isTeacherRole\(role\)" title="Vận hành">/)
  assert.match(consumableRequestSource, /:scroll="\{ x: 'max-content' \}"/)
  assert.match(borrowHistorySource, /:scroll="\{ x: 'max-content' \}"/)
  assert.match(deviceSource, /columns\.value\.reduce\(\(total, column\) => total \+ \(Number\(column\.width\) \|\| 0\), 0\)/)
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
  assert.equal(statusLabel('Hoàn tất'), 'Hỏng')
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
    [getMaintenanceStatusLabel, STATUS.MAINTENANCE_IN_PROGRESS, 'Hỏng', 'red'],
    [getMaintenanceStatusLabel, STATUS.MAINTENANCE_COMPLETING, 'Hỏng', 'red'],
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

test('các ô lọc nhật ký hoạt động có cùng kích thước', () => {
  const source = readFileSync(new URL('../src/views/AuditLogsView.vue', import.meta.url), 'utf8')

  assert.match(source, /--audit-filter-width:\s*150px/)
  assert.match(source, /--audit-filter-height:\s*40px/)
  assert.match(source, /\.filter-control\s*\{[\s\S]*?width:\s*var\(--audit-filter-width\)/)
  assert.match(source, /\.filter-dates\s*\{\s*width:\s*var\(--audit-filter-width\)/)
  assert.match(source, /\.filter-search\s*\{[\s\S]*?width:[^;]+!important/)
  assert.match(source, /\.filter-search :deep\(\.ant-input-affix-wrapper\)\s*\{[\s\S]*?flex:\s*0 0 var\(--audit-filter-width\)/)
  assert.match(source, /\.filter-control,[\s\S]*?height:\s*var\(--audit-filter-height\)/)
})

test('các ô lọc quản lý người dùng có kích thước 150 x 40', () => {
  const source = readFileSync(new URL('../src/views/AdminUsersView.vue', import.meta.url), 'utf8')

  assert.match(source, /--user-filter-width:\s*150px/)
  assert.match(source, /--user-filter-height:\s*40px/)
  assert.match(source, /class="user-filter-control"/)
  assert.match(source, /\.user-filter-search\s*\{[\s\S]*?width:[^;]+!important/)
  assert.match(source, /\.user-filter-search :deep\(\.ant-input-affix-wrapper\)\s*\{[\s\S]*?flex:\s*0 0 var\(--user-filter-width\)/)
})

test('cảnh báo báo cáo mở trang đích theo đúng trạng thái cảnh báo', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')
  const attentionSection = source.slice(source.indexOf('const attentionCards'), source.indexOf('const hasAttention'))

  assert.match(attentionSection, /route: \{ name: 'BorrowHistory', query: \{ status: 'OVERDUE' \} \}/)
  assert.doesNotMatch(attentionSection, /name: 'Maintenance'|maintenance-cost/)
  assert.match(attentionSection, /route: \{ name: 'Devices', query: \{ status: STATUS\.BROKEN \} \}/)
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
  assert.match(source, /title: 'Chi tiết', key: 'details', width: 90/)
  assert.match(source, /aria-label="Xem chi tiết thiết bị phụ trách"/)
  assert.doesNotMatch(source, /<template #icon><EyeOutlined \/><\/template>\s*Xem/)
  assert.match(source, /@click="showResponsibleDetails\(record\)"/)
  assert.match(source, /v-model:open="responsibleDetailsVisible"/)
  assert.match(source, /responsibleDetailsItems/)
  assert.doesNotMatch(source, /key="maintenance"\s+tab="Bảo trì"/)
})

test('báo cáo giữ khoảng cách giữa các khối nội dung bằng nhau', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.match(source, /\.reports-content \{ display: flex; flex-direction: column; gap: 24px; \}/)
  assert.match(source, /\.overview-section, \.main-grid \{ margin-bottom: 0; \}/)
})

test('báo cáo mở danh sách thiết bị đúng trạng thái ngay trong modal', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.match(source, /@click="openStatusDetails\(item\)"/)
  assert.match(source, /const openStatusDetails = async item =>/)
  assert.match(source, /equipmentApi\.getPaged\(\{ page: 1, pageSize: 100, status: item\.value \}\)/)
  assert.match(source, /report\.value\.reservedEquipment/)
  assert.match(source, /reservedByName/)
  assert.match(source, /holdExpiresAt/)
  assert.match(source, /v-model:open="statusDetailsVisible"/)
})

test('báo cáo hiển thị đúng ngày hạn trả và không lệch sang cột trạng thái', () => {
  const source = readFileSync(new URL('../src/views/ReportsView.vue', import.meta.url), 'utf8')

  assert.ok(source.includes("title: 'Hạn trả', dataIndex: 'expectedReturnDate', key: 'expectedReturnDate'"))
  assert.ok(source.includes('const formatDate = value => formatVietnamDate(value)'))
  assert.ok(source.includes("title: 'Trạng thái', dataIndex: 'status', key: 'status'"))
})

test('lịch sử mượn trả chỉ hiển thị tình trạng trả trong chi tiết', () => {
  const source = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const columns = source.match(/const columns = \[([\s\S]*?)\n\]/)?.[1] || ''
  const mobileSummary = source.slice(source.indexOf('<dl class="mobile-card-details">'), source.indexOf('</dl>', source.indexOf('<dl class="mobile-card-details">')))

  assert.doesNotMatch(columns, /title: 'Tình trạng trả'/)
  assert.doesNotMatch(mobileSummary, /Tình trạng trả/)
  assert.match(source, /<a-descriptions-item label="Tình trạng trả">[\s\S]*?selectedRecord\.returnCondition/)
  assert.match(source, /title: 'Trạng thái'[\s\S]*?width: 180[\s\S]*?className: 'status-column'/)
  assert.match(source, /\.status-column\) \{ width: 180px !important;/)
})

test('phiếu chờ duyệt chỉ hiển thị số điện thoại, seri và ngày trong cửa sổ chi tiết', () => {
  const source = readFileSync(new URL('../src/views/BorrowRequestsView.vue', import.meta.url), 'utf8')
  const columns = source.match(/const columns = \[([\s\S]*?)\n\]/)?.[1] || ''

  assert.doesNotMatch(columns, /title: 'SĐT liên hệ'/)
  assert.doesNotMatch(columns, /title: 'Số seri'/)
  assert.doesNotMatch(columns, /title: 'Ngày đăng ký'/)
  assert.doesNotMatch(columns, /title: 'Hạn trả'/)
  assert.match(source, /title="Chi tiết yêu cầu mượn"/)
  assert.match(source, /label="SĐT liên hệ"/)
  assert.match(source, /label="Ngày đăng ký"/)
  assert.match(source, /label="Hạn trả"/)
  assert.match(source, /Số seri:/)
})

test('phiếu mượn bắt buộc nhập số điện thoại liên hệ riêng', () => {
  const source = readFileSync(new URL('../src/components/DeviceTable.vue', import.meta.url), 'utf8')

  assert.match(source, /label="Số điện thoại liên hệ" required/)
  assert.match(source, /:value="borrowForm\.contactPhone"/)
  assert.match(source, /:maxlength="10"/)
  assert.match(source, /@update:value="updateBorrowContactPhone"/)
  assert.match(source, /replace\(\/\\D\/g, ''\)\.slice\(0, 10\)/)
  assert.match(source, /\^\[0-9\]\{10\}\$/)
  assert.match(source, /contactPhone,\s*\n\s*purpose:/)
})

test('bảng thiết bị hiển thị vị trí trước model và chuyển các trường phụ khác vào chi tiết', () => {
  const source = readFileSync(new URL('../src/components/DeviceTable.vue', import.meta.url), 'utf8')
  const columns = source.match(/const columns = computed\(\(\) => \{([\s\S]*?)const tableScrollX/)?.[1] || ''

  for (const title of ['Số seri', 'Danh mục', 'Ngày nhập', 'Quyết định']) {
    assert.doesNotMatch(columns, new RegExp(`title: '${title}'`))
  }
  assert.match(columns, /title: 'Vị trí'[\s\S]*title: 'Model'/)
  assert.match(columns, /title: 'Trạng thái'[\s\S]*align: 'center'[\s\S]*width: 140/)
  assert.match(source, /\.device-table :deep\(th\.status-column\)[\s\S]*text-align: center !important/)
  assert.doesNotMatch(source, />Khấu hao \(%\)</)
  assert.doesNotMatch(source, /Thời gian khấu hao \(tháng\)/)
  assert.doesNotMatch(source, /Tài chính & Khấu hao/)
  assert.doesNotMatch(source, /Đã khấu hao/)
  assert.match(source, /detailField\('categoryName', 'Danh mục'/)
  assert.match(source, /detailField\('serial', 'Số seri'/)
  assert.match(source, /detailField\('entryDate', 'Ngày nhập'/)
  assert.match(source, /detailField\('location', 'Vị trí lưu trữ'/)
  assert.match(source, /detailField\('decisionFile', 'Quyết định'/)
})

test('luồng bàn giao cho phép báo sai lệch và khóa xác nhận khi đang chờ xử lý', () => {
  const historySource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const handoverApiSource = readFileSync(new URL('../src/api/handoverApi.js', import.meta.url), 'utf8')
  const issueViewSource = readFileSync(new URL('../src/views/HandoverIssuesView.vue', import.meta.url), 'utf8')
  const routerSource = readFileSync(new URL('../src/router/index.js', import.meta.url), 'utf8')

  assert.match(historySource, /Báo sai lệch/)
  assert.match(historySource, /selectedHandover\?\.canConfirm/)
  assert.match(historySource, /hasPendingIssueReports/)
  assert.match(historySource, /Ảnh bằng chứng/)
  assert.match(historySource, /selectIssueEvidence/)
  assert.match(handoverApiSource, /createIssueReport/)
  assert.match(handoverApiSource, /form\.append\('files'/)
  assert.match(handoverApiSource, /resolveIssueReport/)
  assert.match(issueViewSource, /Báo cáo sai lệch bàn giao/)
  assert.match(issueViewSource, /downloadIssueEvidence/)
  assert.match(routerSource, /name: 'HandoverIssues'/)
})

test('người mượn luôn mở được biên bản đang chờ xác nhận nhận tài sản', () => {
  const source = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')

  assert.match(source, /v-else-if="canOpenHandover\(record\)"/)
  assert.match(source, /v-else-if="canOpenHandover\(item\)"/)
  assert.match(source, /const canOpenHandover = record => Boolean\([\s\S]*?record\?\.canConfirmHandover[\s\S]*?STATUS\.APPROVED[\s\S]*?record\?\.handover[\s\S]*?!record\.handover\.confirmedAt/)
  assert.match(source, /selectedHandover\?\.canConfirm[\s\S]*?confirmReceipt/)
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
  assert.deepEqual(getDashboardAlertTarget('pending-borrow-requests'), { name: 'BorrowRequests', query: { status: STATUS.BORROW_PENDING } })
  assert.deepEqual(getDashboardAlertTarget('pending-consumable-requests'), { name: 'ConsumableRequests', query: { status: STATUS.CONSUMABLE_PENDING } })
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

test('dashboard giảng viên đưa thao tác nhanh lên đầu và không hiện mũi tên', () => {
  const source = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const teacherStart = source.indexOf('<template v-else-if="isTeacher">')
  const teacherEnd = source.indexOf('<template v-else-if="isStudent">')
  const teacherSection = source.slice(teacherStart, teacherEnd)
  const quickSection = teacherSection.slice(
    teacherSection.indexOf('<section class="teacher-quick-section"'),
    teacherSection.indexOf('</section>', teacherSection.indexOf('<section class="teacher-quick-section"'))
  )

  assert.ok(teacherSection.indexOf('teacher-quick-section') < teacherSection.indexOf('teacher-kpi-grid'))
  assert.doesNotMatch(quickSection, /ArrowRightOutlined/)
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

test('dashboard quản trị mở đúng các phiếu mượn đang chờ xử lý', () => {
  const overviewSource = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const requestsSource = readFileSync(new URL('../src/views/BorrowRequestsView.vue', import.meta.url), 'utf8')

  assert.match(overviewSource, /key: 'pending-borrow-requests'[\s\S]*?pendingBorrowRequests[\s\S]*?status: STATUS\.BORROW_PENDING/)
  assert.match(requestsSource, /const route = useRoute\(\)/)
  assert.match(requestsSource, /statusFilter = ref\(getRouteStatus\(route\.query\.status\)\)/)
})

test('dashboard quản trị mở đúng các yêu cầu cấp phát đang chờ duyệt', () => {
  const overviewSource = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const requestsSource = readFileSync(new URL('../src/views/ConsumableRequestsView.vue', import.meta.url), 'utf8')

  assert.match(overviewSource, /key: 'pending-consumable-requests'[\s\S]*?pendingConsumableRequests[\s\S]*?status: STATUS\.CONSUMABLE_PENDING/)
  assert.match(requestsSource, /const route = useRoute\(\)/)
  assert.match(requestsSource, /statusFilter = ref\(getRouteStatus\(route\.query\.status\)\)/)
  assert.match(requestsSource, /watch\(\(\) => route\.query\.status/)
})

test('yêu cầu cấp phát lọc theo khoảng ngày gửi', () => {
  const source = readFileSync(new URL('../src/views/ConsumableRequestsView.vue', import.meta.url), 'utf8')

  assert.match(source, /<a-range-picker[\s\S]*?v-model:value="requestDateRange"/)
  assert.match(source, /from:\s*requestDateRange\.value\?\.\[0\]\?\.format\('YYYY-MM-DD'\)/)
  assert.match(source, /to:\s*requestDateRange\.value\?\.\[1\]\?\.format\('YYYY-MM-DD'\)/)
})

test('bảng yêu cầu cấp phát thu gọn cột số lượng và hành động', () => {
  const source = readFileSync(new URL('../src/views/ConsumableRequestsView.vue', import.meta.url), 'utf8')

  assert.match(source, /title: 'Số lượng'[\s\S]*?width: 120[\s\S]*?className: 'quantity-column'/)
  assert.match(source, /title: 'Trạng thái'[\s\S]*?width: 190[\s\S]*?className: 'status-column'/)
  assert.match(source, /title: 'Hành động'[\s\S]*?table-sticky-action-column[\s\S]*?width: 190/)
  assert.match(source, /\.quantity-column\) \{ width: 120px !important;/)
  assert.match(source, /\.status-column\) \{ width: 190px !important;/)
  assert.match(source, /\.table-sticky-action-column\) \{ width: 190px !important;/)
})

test('dashboard không còn hiển thị hoặc điều hướng chức năng bảo trì', () => {
  const overviewSource = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const shellSource = readFileSync(new URL('../src/views/DashboardView.vue', import.meta.url), 'utf8')
  const routerSource = readFileSync(new URL('../src/router/index.js', import.meta.url), 'utf8')

  assert.doesNotMatch(overviewSource, /label:\s*['"]Bảo trì['"]/)
  assert.doesNotMatch(shellSource, /MaintenanceSchedules?:/)
  assert.doesNotMatch(routerSource, /name:\s*['"]Maintenance(?:Schedules)?['"]/)
})

test('các thẻ tổng quan quản trị mở đúng danh sách và bộ lọc tương ứng', () => {
  const source = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')

  assert.match(source, /class="manager-kpi-card"[\s\S]*?@click="navigateTo\(item\.route\)"/)
  assert.match(source, /key: 'total-equipment'[\s\S]*?route: \{ name: 'Devices' \}/)
  assert.match(source, /key: 'borrowed-equipment'[\s\S]*?status: STATUS\.BORROWED/)
  assert.match(source, /key: 'pending-work'[\s\S]*?pendingBorrowRequests[\s\S]*?name: 'BorrowRequests'[\s\S]*?status: STATUS\.BORROW_PENDING/)
  assert.match(source, /key: 'broken-equipment'[\s\S]*?status: STATUS\.BROKEN/)
})

test('thẻ dashboard sinh viên mở lịch sử theo đúng nhóm trạng thái', () => {
  const overviewSource = readFileSync(new URL('../src/views/OverviewView.vue', import.meta.url), 'utf8')
  const historySource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')

  assert.match(overviewSource, /key: 'pending'[\s\S]*?BORROW_HISTORY_FILTERS\.PENDING/)
  assert.match(overviewSource, /key: 'approved'[\s\S]*?BORROW_HISTORY_FILTERS\.APPROVED/)
  assert.match(overviewSource, /key: 'active'[\s\S]*?BORROW_HISTORY_FILTERS\.ACTIVE/)
  assert.match(overviewSource, /key: 'returned'[\s\S]*?BORROW_HISTORY_FILTERS\.COMPLETED/)
  assert.match(historySource, /BORROW_HISTORY_FILTERS\.PENDING/)
  assert.match(historySource, /BORROW_HISTORY_FILTERS\.ACTIVE/)
  assert.match(historySource, /BORROW_HISTORY_FILTERS\.COMPLETED/)
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

test('lịch sử mượn trả hỗ trợ nhập và xuất Excel có bước xem trước', () => {
  const viewSource = readFileSync(new URL('../src/views/BorrowHistoryView.vue', import.meta.url), 'utf8')
  const apiSource = readFileSync(new URL('../src/api/borrowApi.js', import.meta.url), 'utf8')

  assert.match(viewSource, /Nhập Excel/)
  assert.match(viewSource, /Xuất Excel/)
  assert.match(viewSource, /previewHistoryImport/)
  assert.match(viewSource, /importHistory/)
  assert.match(apiSource, /\/borrow\/history\/export/)
  assert.match(apiSource, /\/borrow\/history\/import\/preview/)
  assert.match(apiSource, /\/borrow\/history\/import/)
})

test('Vite cho phép đổi đích proxy API khi kiểm thử VPS hoặc backend local', () => {
  const source = readFileSync(new URL('../vite.config.js', import.meta.url), 'utf8')

  assert.match(source, /VITE_DEV_API_PROXY_TARGET/)
  assert.match(source, /proxyTarget/)
})
