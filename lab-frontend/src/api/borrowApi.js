import axiosClient from './axiosClient';

export const borrowApi = {
  createRequest: (data) => axiosClient.post('/borrow', data),
  getPendingRequests: () => axiosClient.get('/borrow/pending'),
  getPendingRequestsPaged: (params = {}) => axiosClient.get('/borrow/pending/paged', { params }),
  getHistory: () => axiosClient.get('/borrow/history'),
  getHistoryPaged: (params = {}) => axiosClient.get('/borrow/history/paged', { params }),
  approve: (id) => axiosClient.put(`/borrow/${id}/approve`),
  reject: (id) => axiosClient.put(`/borrow/${id}/reject`),
  cancel: (id, reason) => axiosClient.put(`/borrow/${id}/cancel`, { reason }),
  returnEquipment: (id, data = null) => axiosClient.put(`/borrow/${id}/return`, data),
  reportDamage: (id, data) => axiosClient.put(`/borrow/${id}/report-damage`, data),
  uploadReturnEvidence: (id, file, evidenceType = 'PHOTO_AFTER', equipmentId = null) => {
    const form = new FormData()
    form.append('file', file)
    form.append('evidenceType', evidenceType)
    if (equipmentId) form.append('equipmentId', equipmentId)
    return axiosClient.post(`/borrow/${id}/return-evidence`, form)
  },
  getTeacherPending: () => axiosClient.get('/borrow/teacher-pending'),
  getTeacherPendingPaged: (params = {}) => axiosClient.get('/borrow/teacher-pending/paged', { params }),
  teacherApprove: (id, note) => axiosClient.put(`/borrow/${id}/teacher-approve`, { note }),
  teacherReject: (id, note) => axiosClient.put(`/borrow/${id}/teacher-reject`, { note }),
  remind: (id) => axiosClient.post(`/borrow/${id}/remind`)
};
