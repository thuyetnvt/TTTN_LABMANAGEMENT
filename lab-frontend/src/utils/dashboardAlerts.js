const ALERT_TARGETS = Object.freeze({
  overdue: { name: 'BorrowHistory', query: { status: 'OVERDUE' } },
  'low-stock': { name: 'Devices', query: { tab: 'consumables', stock: 'LOW_STOCK' } },
  'pending-requests': { name: 'BorrowRequests' },
  'pending-borrow-requests': { name: 'BorrowRequests' },
  'pending-consumable-requests': { name: 'ConsumableRequests' },
  'warranty-soon': { name: 'Devices', query: { status: 'warranty-soon' } }
})

export const getDashboardAlertTarget = (type) => {
  const target = ALERT_TARGETS[type]
  if (!target) return null

  return {
    name: target.name,
    ...(target.query ? { query: { ...target.query } } : {})
  }
}
