const ALERT_TARGETS = Object.freeze({
  overdue: { name: 'BorrowRequests' },
  'low-stock': { name: 'Devices', query: { tab: 'consumables' } },
  'pending-requests': { name: 'BorrowRequests' },
  'warranty-soon': { name: 'Devices', query: { status: 'warranty' } }
})

export const getDashboardAlertTarget = (type) => {
  const target = ALERT_TARGETS[type]
  if (!target) return null

  return {
    name: target.name,
    ...(target.query ? { query: { ...target.query } } : {})
  }
}
