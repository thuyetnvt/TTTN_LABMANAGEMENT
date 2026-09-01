import axiosClient from './axiosClient'

export const inventoryApi = {
  getAll: () => axiosClient.get('/inventory'),
  getPaged: (params = {}) => axiosClient.get('/inventory/paged', { params }),
  getById: (id) => axiosClient.get(`/inventory/${id}`),
  getItemsPaged: (id, params = {}) => axiosClient.get(`/inventory/${id}/items/paged`, { params }),
  create: (data) => axiosClient.post('/inventory', data),
  scan: (id, data) => axiosClient.post(`/inventory/${id}/scan`, data),
  startReview: (id) => axiosClient.post(`/inventory/${id}/start-review`),
  reviewItem: (sessionId, itemId, data) => axiosClient.put(`/inventory/${sessionId}/items/${itemId}/review`, data),
  complete: (id) => axiosClient.post(`/inventory/${id}/complete`),
  uploadEvidence: (sessionId, itemId, file, evidenceType = 'PHOTO') => {
    const form = new FormData()
    form.append('file', file)
    form.append('evidenceType', evidenceType)
    return axiosClient.post(`/inventory/${sessionId}/items/${itemId}/evidence`, form)
  },
  exportExcel: (id) => axiosClient.get(`/inventory/${id}/export.xlsx`, { responseType: 'blob' }),
  exportPdf: (id) => axiosClient.get(`/inventory/${id}/export.pdf`, { responseType: 'blob' })
}
