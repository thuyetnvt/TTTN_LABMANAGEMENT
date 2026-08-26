const readBlobError = async (error) => {
  const data = error?.response?.data
  if (!data) return ''

  if (typeof data === 'string') return data
  if (typeof Blob !== 'undefined' && data instanceof Blob) {
    return data.text()
  }
  if (typeof data.text === 'function') return data.text()
  return JSON.stringify(data)
}

export const getExportErrorMessage = async (error) => {
  const raw = await readBlobError(error)
  if (!raw) return error?.message || 'Không thể xuất báo cáo.'

  try {
    const payload = typeof raw === 'string' ? JSON.parse(raw) : raw
    return payload?.message || payload?.title || error?.message || 'Không thể xuất báo cáo.'
  } catch {
    return raw || error?.message || 'Không thể xuất báo cáo.'
  }
}
