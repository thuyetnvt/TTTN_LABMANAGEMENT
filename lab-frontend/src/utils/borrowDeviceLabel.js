export function borrowDeviceLabel(record) {
  const items = (record?.details || []).filter(item => item.equipmentName?.trim())
  if (!items.length) return record?.device || 'Chưa có thông tin thiết bị'
  const labels = items.slice(0, 3).map(item => {
    const name = item.equipmentName.trim()
    const quantity = Number(item.quantity || 1)
    return quantity > 1 ? `${name} ×${quantity}` : name
  })
  if (items.length > 3) labels.push(`và ${items.length - 3} thiết bị khác`)
  return labels.join('; ')
}
