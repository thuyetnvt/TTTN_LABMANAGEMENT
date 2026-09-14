import axiosClient from './axiosClient';

export const consumableRequestApi = {
  getAll: () => axiosClient.get('/consumablerequest'),
  getPaged: (params = {}) => axiosClient.get('/consumablerequest/paged', { params }),
  exportReport: (params = {}) => axiosClient.get('/consumablerequest/export', { params, responseType: 'blob' }),
  create: (data) => axiosClient.post('/consumablerequest', data),
  approve: (id) => axiosClient.put(`/consumablerequest/${id}/approve`),
  getAvailableLots: (id) => axiosClient.get(`/consumablerequest/${id}/available-lots`),
  handover: (id, data) => axiosClient.put(`/consumablerequest/${id}/handover`, data),
  confirmReceipt: (id) => axiosClient.put(`/consumablerequest/${id}/confirm-receipt`),
  reject: (id, data) => axiosClient.put(`/consumablerequest/${id}/reject`, data)
};
