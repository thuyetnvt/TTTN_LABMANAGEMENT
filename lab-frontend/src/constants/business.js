export const STATUS = Object.freeze({
  AVAILABLE: 'AVAILABLE',
  BORROW_PENDING: 'BORROW_PENDING',
  TEACHER_PENDING: 'TEACHER_PENDING',
  APPROVED: 'APPROVED',
  BORROWED: 'BORROWED',
  RETURN_PROCESSING: 'RETURN_PROCESSING',
  RETURNED: 'RETURNED',
  RETURNED_DAMAGED: 'RETURNED_DAMAGED',
  REJECTED: 'REJECTED',
  UNDER_WARRANTY: 'UNDER_WARRANTY',
  BROKEN: 'BROKEN',
  MAINTENANCE_IN_PROGRESS: 'MAINTENANCE_IN_PROGRESS',
  MAINTENANCE_COMPLETED: 'MAINTENANCE_COMPLETED',
  CONSUMABLE_PENDING: 'CONSUMABLE_PENDING',
  CONSUMABLE_PROCESSING: 'CONSUMABLE_PROCESSING',
  CONSUMABLE_ISSUED: 'CONSUMABLE_ISSUED',
  UNPAID: 'UNPAID',
  PAID: 'PAID',
  INVENTORY_OPEN: 'INVENTORY_OPEN',
  INVENTORY_COMPLETED: 'INVENTORY_COMPLETED',
  INVENTORY_PENDING: 'INVENTORY_PENDING',
  INVENTORY_FOUND: 'INVENTORY_FOUND',
  INVENTORY_WRONG_LOCATION: 'INVENTORY_WRONG_LOCATION',
  INVENTORY_DAMAGED: 'INVENTORY_DAMAGED',
  INVENTORY_MISSING: 'INVENTORY_MISSING'
})

export const ROLE_LABELS = Object.freeze({
  Admin: 'Quản trị viên',
  ADMIN: 'Quản trị viên',
  'Trưởng lab': 'Trưởng phòng Lab',
  LAB_HEAD: 'Trưởng phòng Lab',
  'Phó lab': 'Phó phòng Lab',
  DEPUTY_LAB_HEAD: 'Phó phòng Lab',
  'Giảng viên': 'Giảng viên',
  TEACHER: 'Giảng viên',
  'Sinh viên': 'Sinh viên',
  STUDENT: 'Sinh viên',
  Guest: 'Khách'
})

export const ROLE = Object.freeze({
  ADMIN: 'Admin',
  LAB_HEAD: 'Trưởng lab',
  DEPUTY_LAB_HEAD: 'Phó lab',
  TEACHER: 'Giảng viên',
  STUDENT: 'Sinh viên'
})

export const MANAGER_ROLES = Object.freeze([
  ROLE.ADMIN,
  ROLE.LAB_HEAD,
  ROLE.DEPUTY_LAB_HEAD
])

export const BORROWER_ROLES = Object.freeze([ROLE.STUDENT, ROLE.TEACHER])
export const isManagerRole = (value) => MANAGER_ROLES.includes(value)
export const isBorrowerRole = (value) => BORROWER_ROLES.includes(value)
export const isAdminRole = (value) => value === ROLE.ADMIN
export const isStudentRole = (value) => value === ROLE.STUDENT
export const isTeacherRole = (value) => value === ROLE.TEACHER

const LEGACY_STATUS = Object.freeze({
  Rảnh: STATUS.AVAILABLE,
  'Sẵn sàng': STATUS.AVAILABLE,
  'Chờ duyệt': STATUS.BORROW_PENDING,
  'Chờ GV duyệt': STATUS.TEACHER_PENDING,
  'Đang xử lý duyệt': 'APPROVAL_PROCESSING',
  'Đang xử lý trả': STATUS.RETURN_PROCESSING,
  'Đang mượn': STATUS.BORROWED,
  'Đã trả': STATUS.RETURNED,
  'Đã trả (Hỏng)': STATUS.RETURNED_DAMAGED,
  'Đã trả (Bảo hành)': STATUS.RETURNED_DAMAGED,
  'Từ chối': STATUS.REJECTED,
  'Bảo hành': STATUS.UNDER_WARRANTY,
  'Hỏng': STATUS.BROKEN,
  'Đang xử lý': STATUS.MAINTENANCE_IN_PROGRESS,
  'Hoàn tất': STATUS.MAINTENANCE_COMPLETED,
  'Hoàn thành': STATUS.MAINTENANCE_COMPLETED,
  'Đã cấp phát': STATUS.CONSUMABLE_ISSUED,
  'Chưa thanh toán': STATUS.UNPAID,
  'Đã thanh toán': STATUS.PAID,
  'Đang kiểm kê': STATUS.INVENTORY_OPEN,
  'Đã kết thúc kiểm kê': STATUS.INVENTORY_COMPLETED,
  'Chưa kiểm kê': STATUS.INVENTORY_PENDING,
  'Đã tìm thấy': STATUS.INVENTORY_FOUND,
  'Sai vị trí': STATUS.INVENTORY_WRONG_LOCATION,
  'Hỏng khi kiểm kê': STATUS.INVENTORY_DAMAGED,
  'Thất lạc': STATUS.INVENTORY_MISSING
})

export const normalizeStatus = (value) => LEGACY_STATUS[value] || value || ''

export const statusLabel = (value) => ({
  [STATUS.AVAILABLE]: 'Rảnh',
  [STATUS.BORROW_PENDING]: 'Chờ duyệt',
  [STATUS.TEACHER_PENDING]: 'Chờ giảng viên duyệt',
  APPROVAL_PROCESSING: 'Đang xử lý duyệt',
  [STATUS.APPROVED]: 'Đã duyệt',
  [STATUS.BORROWED]: 'Đang mượn',
  [STATUS.RETURN_PROCESSING]: 'Đang xử lý trả',
  [STATUS.RETURNED]: 'Đã trả',
  [STATUS.RETURNED_DAMAGED]: 'Đã trả, có hư hỏng',
  [STATUS.REJECTED]: 'Từ chối',
  [STATUS.UNDER_WARRANTY]: 'Bảo hành',
  [STATUS.BROKEN]: 'Hỏng',
  [STATUS.MAINTENANCE_IN_PROGRESS]: 'Đang bảo trì',
  [STATUS.MAINTENANCE_COMPLETED]: 'Đã hoàn thành bảo trì',
  [STATUS.CONSUMABLE_PENDING]: 'Chờ duyệt cấp phát',
  [STATUS.CONSUMABLE_PROCESSING]: 'Đang xử lý cấp phát',
  [STATUS.CONSUMABLE_ISSUED]: 'Đã cấp phát',
  [STATUS.INVENTORY_OPEN]: 'Đang kiểm kê',
  [STATUS.INVENTORY_COMPLETED]: 'Đã kết thúc kiểm kê',
  [STATUS.INVENTORY_PENDING]: 'Chưa kiểm kê',
  [STATUS.INVENTORY_FOUND]: 'Đã tìm thấy',
  [STATUS.INVENTORY_WRONG_LOCATION]: 'Sai vị trí',
  [STATUS.INVENTORY_DAMAGED]: 'Hỏng khi kiểm kê',
  [STATUS.INVENTORY_MISSING]: 'Thất lạc',
  [STATUS.UNPAID]: 'Chưa thanh toán',
  [STATUS.PAID]: 'Đã thanh toán'
}[normalizeStatus(value)] || value || 'Chưa xác định')

export const statusColor = (value) => ({
  [STATUS.AVAILABLE]: 'green',
  [STATUS.RETURNED]: 'green',
  [STATUS.MAINTENANCE_COMPLETED]: 'green',
  [STATUS.BORROW_PENDING]: 'orange',
  [STATUS.TEACHER_PENDING]: 'orange',
  APPROVAL_PROCESSING: 'blue',
  [STATUS.RETURN_PROCESSING]: 'blue',
  [STATUS.BORROWED]: 'blue',
  [STATUS.UNDER_WARRANTY]: 'purple',
  [STATUS.BROKEN]: 'red',
  [STATUS.RETURNED_DAMAGED]: 'red',
  [STATUS.REJECTED]: 'red',
  [STATUS.UNPAID]: 'red',
  [STATUS.PAID]: 'green',
  [STATUS.MAINTENANCE_IN_PROGRESS]: 'blue',
  [STATUS.CONSUMABLE_PENDING]: 'orange',
  [STATUS.CONSUMABLE_PROCESSING]: 'blue',
  [STATUS.CONSUMABLE_ISSUED]: 'green'
  ,[STATUS.INVENTORY_OPEN]: 'blue'
  ,[STATUS.INVENTORY_COMPLETED]: 'green'
  ,[STATUS.INVENTORY_PENDING]: 'orange'
  ,[STATUS.INVENTORY_FOUND]: 'green'
  ,[STATUS.INVENTORY_WRONG_LOCATION]: 'orange'
  ,[STATUS.INVENTORY_DAMAGED]: 'red'
  ,[STATUS.INVENTORY_MISSING]: 'red'
}[normalizeStatus(value)] || 'default')

export const statusMatches = (value, expected) => normalizeStatus(value) === expected
export const roleLabel = (value) => ROLE_LABELS[value] || value || 'Chưa xác định'
