export const getApiErrorMessage = (error, fallback = 'Đã có lỗi xảy ra. Vui lòng thử lại.') => {
  const responseData = error?.response?.data
  const responseMessage = typeof responseData === 'string' ? responseData : responseData?.message
  if (typeof responseMessage === 'string' && responseMessage.trim()) return responseMessage

  const validationErrors = responseData?.errors
  if (validationErrors && typeof validationErrors === 'object') {
    const firstError = Object.values(validationErrors).flat().find(value => typeof value === 'string' && value.trim())
    if (firstError) return firstError
  }

  if (typeof error?.message === 'string' && error.message.trim()) return error.message

  return fallback
}

export const getApiSuccessMessage = (result, fallback) => result?.message || fallback
