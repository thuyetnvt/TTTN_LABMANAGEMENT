import {
  AuditOutlined,
  BellOutlined,
  CheckCircleOutlined,
  InboxOutlined,
  ToolOutlined,
  WarningOutlined
} from '@ant-design/icons-vue'
import { formatVietnamDate, parseApiDate } from './dateTime.js'

const TYPE_LABELS = {
  BORROW_PENDING: 'Mượn trả',
  BORROW_TEACHER_PENDING: 'Mượn trả',
  BORROW_APPROVED: 'Mượn trả',
  BORROW_REJECTED: 'Mượn trả',
  BORROW_RETURNED: 'Mượn trả',
  HANDOVER_CREATED: 'Bàn giao',
  CONSUMABLE_PENDING: 'Vật tư',
  CONSUMABLE_ISSUED: 'Vật tư',
  CONSUMABLE_REJECTED: 'Vật tư',
  MAINTENANCE_CREATED: 'Bảo trì',
  MAINTENANCE_COMPLETED: 'Bảo trì',
  MAINTENANCE_SCHEDULE_GENERATED: 'Bảo trì',
  INVENTORY_CREATED: 'Kiểm kê',
  INVENTORY_COMPLETED: 'Kiểm kê'
}

export const notificationTypeLabel = type => TYPE_LABELS[type] || 'Hệ thống'

export const notificationIcon = type => {
  if (type?.startsWith('MAINTENANCE')) return ToolOutlined
  if (type?.startsWith('INVENTORY')) return AuditOutlined
  if (type?.startsWith('CONSUMABLE')) return InboxOutlined
  if (type?.startsWith('BORROW') || type === 'HANDOVER_CREATED') return CheckCircleOutlined
  if (type === 'WARNING') return WarningOutlined
  return BellOutlined
}

export const formatRelativeTime = value => {
  if (!value) return '—'
  const date = parseApiDate(value)
  if (!date) return '—'
  const timestamp = date.getTime()
  const seconds = Math.max(0, Math.floor((Date.now() - timestamp) / 1000))
  if (seconds < 60) return 'Vừa xong'
  if (seconds < 3600) return `${Math.floor(seconds / 60)} phút trước`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)} giờ trước`
  if (seconds < 172800) return 'Hôm qua'
  if (seconds < 604800) return `${Math.floor(seconds / 86400)} ngày trước`
  return formatVietnamDate(date)
}
