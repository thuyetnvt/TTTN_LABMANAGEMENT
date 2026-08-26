const normalizeStatus = value => String(value ?? '').trim().toUpperCase()

const UNKNOWN_STATUS_LABEL = 'Không xác định'

const EQUIPMENT_STATUS_LABELS = Object.freeze({
  AVAILABLE: 'Rảnh',
  BORROW_PENDING: 'Chờ mượn',
  BORROWED: 'Đang mượn',
  RETURNED: 'Đã trả',
  RETURNED_DAMAGED: 'Đã trả (hỏng)',
  BROKEN: 'Hỏng',
  UNDER_WARRANTY: 'Bảo hành',
  WARRANTY: 'Bảo hành',
  MAINTENANCE_IN_PROGRESS: 'Đang bảo trì',
  MAINTENANCE_COMPLETED: 'Đã bảo trì'
})

const BORROW_STATUS_LABELS = Object.freeze({
  PENDING: 'Chờ duyệt',
  BORROW_PENDING: 'Chờ duyệt',
  TEACHER_PENDING: 'Chờ giảng viên duyệt',
  APPROVED: 'Đã duyệt',
  PROCESSING_APPROVAL: 'Đang xử lý duyệt',
  APPROVAL_PROCESSING: 'Đang xử lý duyệt',
  BORROWED: 'Đang mượn',
  PROCESSING_RETURN: 'Đang xử lý trả',
  RETURN_PROCESSING: 'Đang xử lý trả',
  RETURNED: 'Đã trả',
  RETURNED_DAMAGED: 'Đã trả (hỏng)',
  REJECTED: 'Từ chối'
})

const MAINTENANCE_STATUS_LABELS = Object.freeze({
  IN_PROGRESS: 'Đang thực hiện',
  MAINTENANCE_IN_PROGRESS: 'Đang thực hiện',
  COMPLETING: 'Đang nghiệm thu',
  MAINTENANCE_COMPLETING: 'Đang nghiệm thu',
  COMPLETED: 'Đã hoàn tất',
  MAINTENANCE_COMPLETED: 'Đã hoàn tất'
})

const INVENTORY_STATUS_LABELS = Object.freeze({
  OPEN: 'Đang kiểm kê',
  INVENTORY_OPEN: 'Đang kiểm kê',
  COMPLETED: 'Đã kết thúc',
  INVENTORY_COMPLETED: 'Đã kết thúc',
  FOUND: 'Đã tìm thấy',
  INVENTORY_FOUND: 'Đã tìm thấy',
  WRONG_LOCATION: 'Sai vị trí',
  INVENTORY_WRONG_LOCATION: 'Sai vị trí',
  DAMAGED: 'Hư hỏng',
  INVENTORY_DAMAGED: 'Hư hỏng',
  MISSING: 'Thất lạc',
  INVENTORY_MISSING: 'Thất lạc',
  PENDING: 'Chưa kiểm kê',
  INVENTORY_PENDING: 'Chưa kiểm kê'
})

const CONSUMABLE_REQUEST_STATUS_LABELS = Object.freeze({
  PENDING: 'Chờ xử lý',
  CONSUMABLE_PENDING: 'Chờ xử lý',
  PROCESSING: 'Đang xử lý',
  CONSUMABLE_PROCESSING: 'Đang xử lý',
  ISSUED: 'Đã cấp phát',
  CONSUMABLE_ISSUED: 'Đã cấp phát',
  REJECTED: 'Từ chối'
})

const PENALTY_STATUS_LABELS = Object.freeze({
  UNPAID: 'Chưa thanh toán',
  PAID: 'Đã thanh toán',
  PENDING: 'Chờ xử lý',
  PROCESSING: 'Đang xử lý',
  ISSUED: 'Đã cấp phát',
  REJECTED: 'Từ chối'
})

const RETURN_CONDITION_LABELS = Object.freeze({
  AVAILABLE: 'Rảnh',
  GOOD: 'Tốt',
  SCRATCHED: 'Trầy xước',
  MISSING_ACCESSORIES: 'Thiếu phụ kiện',
  MISSING_ACCESSORY: 'Thiếu phụ kiện',
  BROKEN: 'Hỏng',
  RETURNED_DAMAGED: 'Đã trả (hỏng)'
})

const getMappedLabel = (map, status) => map[normalizeStatus(status)] || UNKNOWN_STATUS_LABEL

export const getEquipmentStatusLabel = status => getMappedLabel(EQUIPMENT_STATUS_LABELS, status)
export const getBorrowStatusLabel = status => getMappedLabel(BORROW_STATUS_LABELS, status)
export const getMaintenanceStatusLabel = status => getMappedLabel(MAINTENANCE_STATUS_LABELS, status)
export const getInventoryStatusLabel = status => getMappedLabel(INVENTORY_STATUS_LABELS, status)
export const getConsumableRequestStatusLabel = status => getMappedLabel(CONSUMABLE_REQUEST_STATUS_LABELS, status)
export const getPenaltyStatusLabel = status => getMappedLabel(PENALTY_STATUS_LABELS, status)
export const getReturnConditionLabel = status => getMappedLabel(RETURN_CONDITION_LABELS, status)

const labelers = Object.freeze({
  equipment: getEquipmentStatusLabel,
  borrow: getBorrowStatusLabel,
  maintenance: getMaintenanceStatusLabel,
  inventory: getInventoryStatusLabel,
  consumableRequest: getConsumableRequestStatusLabel,
  penalty: getPenaltyStatusLabel,
  returnCondition: getReturnConditionLabel
})

export const getStatusLabel = (status, type = 'equipment') =>
  (labelers[type] || getEquipmentStatusLabel)(status)

export const getStatusColor = (status, type = 'equipment') => {
  const value = normalizeStatus(status)

  if (type === 'maintenance') {
    if (['COMPLETED', 'MAINTENANCE_COMPLETED'].includes(value)) return 'green'
    if (['COMPLETING', 'MAINTENANCE_COMPLETING'].includes(value)) return 'purple'
    if (['IN_PROGRESS', 'MAINTENANCE_IN_PROGRESS'].includes(value)) return 'purple'
  }

  if (type === 'inventory') {
    if (['OPEN', 'INVENTORY_OPEN'].includes(value)) return 'blue'
    if (['COMPLETED', 'INVENTORY_COMPLETED', 'FOUND', 'INVENTORY_FOUND'].includes(value)) return 'green'
    if (['PENDING', 'INVENTORY_PENDING', 'WRONG_LOCATION', 'INVENTORY_WRONG_LOCATION'].includes(value)) return 'orange'
    if (['DAMAGED', 'INVENTORY_DAMAGED', 'MISSING', 'INVENTORY_MISSING'].includes(value)) return 'red'
  }

  if (['UNDER_WARRANTY', 'WARRANTY'].includes(value)) return 'gold'
  if (['SCRATCHED', 'MISSING_ACCESSORIES', 'MISSING_ACCESSORY'].includes(value)) return 'orange'
  if (['BORROW_PENDING', 'PENDING', 'TEACHER_PENDING', 'CONSUMABLE_PENDING'].includes(value)) return 'orange'
  if (['PROCESSING_APPROVAL', 'APPROVAL_PROCESSING', 'PROCESSING_RETURN', 'RETURN_PROCESSING', 'BORROWED', 'CONSUMABLE_PROCESSING'].includes(value)) return 'blue'
  if (['BROKEN', 'RETURNED_DAMAGED', 'REJECTED', 'UNPAID', 'DAMAGED', 'MISSING'].includes(value)) return 'red'
  if (['MAINTENANCE_IN_PROGRESS', 'MAINTENANCE_COMPLETING'].includes(value)) return 'purple'
  if (['AVAILABLE', 'RETURNED', 'MAINTENANCE_COMPLETED', 'ISSUED', 'CONSUMABLE_ISSUED', 'PAID', 'GOOD', 'FOUND'].includes(value)) return 'green'

  return 'default'
}
