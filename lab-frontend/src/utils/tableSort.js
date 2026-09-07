const normalizeValue = value => value === null || value === undefined ? '' : value

export const sortTableRows = (rows, field, order, getValue = row => row?.[field]) => {
  if (!order || !field) return rows
  const direction = order === 'descend' ? -1 : 1

  return [...rows].sort((left, right) => {
    const leftValue = normalizeValue(getValue(left, field))
    const rightValue = normalizeValue(getValue(right, field))

    if (leftValue === rightValue) return 0
    if (leftValue === '') return 1
    if (rightValue === '') return -1
    if (typeof leftValue === 'number' && typeof rightValue === 'number') {
      return (leftValue - rightValue) * direction
    }

    const leftDate = Date.parse(leftValue)
    const rightDate = Date.parse(rightValue)
    if (!Number.isNaN(leftDate) && !Number.isNaN(rightDate)) {
      return (leftDate - rightDate) * direction
    }

    return String(leftValue).localeCompare(String(rightValue), 'vi', { numeric: true, sensitivity: 'base' }) * direction
  })
}
