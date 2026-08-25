export const getApiErrorMessage = (error, fallback = 'Đã có lỗi xảy ra. Vui lòng thử lại.') => {
  const responseMessage = error?.response?.data?.message
  if (typeof responseMessage === 'string' && responseMessage.trim()) return responseMessage

  if (typeof error?.message === 'string' && error.message.trim()) return error.message

  return fallback
}

export const getApiSuccessMessage = (result, fallback) => result?.message || fallback
