const VIETNAM_TIME_ZONE = 'Asia/Ho_Chi_Minh'
const ISO_DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/
const HAS_TIME_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i

/**
 * API timestamps are stored as UTC. MySQL DateTime values can be serialized
 * without an offset, so explicitly interpret offset-less timestamps as UTC
 * before displaying them in Vietnam time.
 */
export const parseApiDate = value => {
  if (!value) return null
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value
  }

  const text = String(value).trim()
  if (!text) return null

  const normalized = ISO_DATE_ONLY.test(text)
    ? `${text}T00:00:00Z`
    : HAS_TIME_ZONE.test(text) ? text : `${text}Z`
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? null : date
}

export const formatVietnamDateTime = (value, fallback = '—') => {
  const date = parseApiDate(value)
  return date
    ? date.toLocaleString('vi-VN', { timeZone: VIETNAM_TIME_ZONE })
    : fallback
}

export const formatVietnamDate = (value, fallback = '—') => {
  const date = parseApiDate(value)
  return date
    ? date.toLocaleDateString('vi-VN', { timeZone: VIETNAM_TIME_ZONE })
    : fallback
}

export const formatVietnamDateInput = (value, fallback = '') => {
  const date = parseApiDate(value)
  if (!date) return fallback
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: VIETNAM_TIME_ZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(date)
  const part = type => parts.find(item => item.type === type)?.value
  return `${part('year')}-${part('month')}-${part('day')}`
}

export const vietnamDateInputToUtc = (value, fallback = null) => {
  if (!ISO_DATE_ONLY.test(String(value || '').trim())) return fallback
  const date = new Date(`${String(value).trim()}T00:00:00+07:00`)
  return Number.isNaN(date.getTime()) ? fallback : date.toISOString()
}
