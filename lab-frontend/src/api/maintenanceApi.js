import axiosClient from './axiosClient';

export const maintenanceApi = {
  getAll: () => axiosClient.get('/maintenance'),
  getPaged: (params = {}) => axiosClient.get('/maintenance/paged', { params }),
  create: (data) => axiosClient.post('/maintenance', data),
  complete: (id, data) => axiosClient.put(`/maintenance/${id}/complete`, data),
  uploadEvidence: (id, file, evidenceType = 'PHOTO') => {
    const form = new FormData()
    form.append('file', file)
    form.append('evidenceType', evidenceType)
    return axiosClient.post(`/maintenance/${id}/evidence`, form)
  },
  delete: (id) => axiosClient.delete(`/maintenance/${id}`)
};
