import { normalizeStatus } from '../constants/business.js'

const UNKNOWN_STATUS = 'Không xác định'

const equipmentLabels = Object.freeze({
  AVAILABLE: 'Rảnh',
  BORROW_PENDING: 'Chờ mượn',
  BORROWED: 'Đang mượn',
  RETURNED: 'Đã trả',
  RETURNED_DAMAGED: 'Đã trả (hỏng)',
  BROKEN: 'Hỏng',
  MISSING: 'Thất lạc',
  UNDER_WARRANTY: 'Bảo hành',
  MAINTENANCE_IN_PROGRESS: 'Đang bảo trì',
  MAINTENANCE_COMPLETED: 'Đã bảo trì'
})

const borrowLabels = Object.freeze({
  PENDING: 'Chờ duyệt',
  TEACHER_PENDING: 'Chờ giảng viên duyệt',
  PROCESSING_APPROVAL: 'Đang xử lý duyệt',
  APPROVAL_PROCESSING: 'Đang xử lý duyệt',
  APPROVED: 'Đã duyệt',
  BORROWED: 'Đang mượn',
  PROCESSING_RETURN: 'Đang xử lý trả',
  RETURN_PROCESSING: 'Đang xử lý trả',
  RETURNED: 'Đã trả',
  RETURNED_DAMAGED: 'Đã trả (hỏng)',
  REJECTED: 'Từ chối'
})

const maintenanceLabels = Object.freeze({
  IN_PROGRESS: 'Đang thực hiện',
  COMPLETING: 'Đang nghiệm thu',
  COMPLETED: 'Đã hoàn tất',
  MAINTENANCE_IN_PROGRESS: 'Đang thực hiện',
  MAINTENANCE_COMPLETING: 'Đang nghiệm thu',
  MAINTENANCE_COMPLETED: 'Đã hoàn tất'
})

const inventoryLabels = Object.freeze({
  OPEN: 'Đang kiểm kê',
  COMPLETED: 'Đã kết thúc',
  FOUND: 'Đã tìm thấy',
  WRONG_LOCATION: 'Sai vị trí',
  DAMAGED: 'Hư hỏng',
  MISSING: 'Thất lạc',
  PENDING: 'Chưa kiểm kê',
  INVENTORY_OPEN: 'Đang kiểm kê',
  INVENTORY_REVIEWING: 'Đang đối soát',
  INVENTORY_COMPLETED: 'Đã kết thúc',
  INVENTORY_FOUND: 'Đã tìm thấy',
  INVENTORY_WRONG_LOCATION: 'Sai vị trí',
  INVENTORY_DAMAGED: 'Hư hỏng',
  INVENTORY_MISSING: 'Thất lạc',
  INVENTORY_PENDING: 'Chưa kiểm kê'
})

const consumableRequestLabels = Object.freeze({
  PENDING: 'Chờ xử lý',
  PROCESSING: 'Đang xử lý',
  APPROVED: 'Đã duyệt, chờ bàn giao',
  HANDED_OVER: 'Đã bàn giao, chờ xác nhận',
  RECEIVED: 'Đã nhận vật tư',
  ISSUED: 'Đã cấp phát',
  REJECTED: 'Từ chối',
  CONSUMABLE_PENDING: 'Chờ xử lý',
  CONSUMABLE_PROCESSING: 'Đang xử lý',
  CONSUMABLE_APPROVED: 'Đã duyệt, chờ bàn giao',
  CONSUMABLE_HANDED_OVER: 'Đã bàn giao, chờ xác nhận',
  CONSUMABLE_RECEIVED: 'Đã nhận vật tư',
  CONSUMABLE_ISSUED: 'Đã cấp phát'
})

const penaltyLabels = Object.freeze({
  UNPAID: 'Chưa thanh toán',
  PAID: 'Đã thanh toán'
})

const returnConditionLabels = Object.freeze({
  AVAILABLE: 'Bình thường',
  GOOD: 'Tốt',
  SCRATCHED: 'Trầy xước',
  MISSING_ACCESSORIES: 'Thiếu phụ kiện',
  BROKEN: 'Hỏng'
})

const displayLabel = (labels, status) => labels[normalizeStatus(status)] || labels[status] || UNKNOWN_STATUS

export const getEquipmentStatusLabel = status => displayLabel(equipmentLabels, status)
export const getBorrowStatusLabel = status => displayLabel(borrowLabels, status)
export const getMaintenanceStatusLabel = status => displayLabel(maintenanceLabels, status)
export const getInventoryStatusLabel = status => displayLabel(inventoryLabels, status)
export const getConsumableRequestStatusLabel = status => displayLabel(consumableRequestLabels, status)
export const getPenaltyStatusLabel = status => displayLabel(penaltyLabels, status)
export const getReturnConditionLabel = status => displayLabel(returnConditionLabels, status)

export const getStatusColor = status => {
  const normalized = normalizeStatus(status)
  if (normalized === 'AVAILABLE' || normalized === 'RETURNED' || normalized === 'MAINTENANCE_COMPLETED' || normalized === 'CONSUMABLE_RECEIVED') return 'green'
  if (normalized === 'BORROWED' || normalized === 'MAINTENANCE_IN_PROGRESS' || normalized === 'RETURN_PROCESSING') return 'blue'
  if (normalized === 'COMPLETED' || normalized === 'APPROVED' || normalized === 'CONSUMABLE_APPROVED') return 'cyan'
  if (normalized === 'CONSUMABLE_HANDED_OVER') return 'purple'
  if (normalized === 'UNDER_WARRANTY') return 'gold'
  if (normalized === 'BROKEN' || normalized === 'RETURNED_DAMAGED' || normalized === 'REJECTED') return 'red'
  if (normalized.includes('PENDING') || normalized === 'PENDING') return 'orange'
  if (normalized === 'IN_PROGRESS' || normalized === 'COMPLETING') return 'purple'
  return 'default'
}
