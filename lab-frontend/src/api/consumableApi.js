import axiosClient from './axiosClient';

export const consumableApi = {
  getAll: () => axiosClient.get('/consumable'),
  getPaged: (params = {}) => axiosClient.get('/consumable/paged', { params }),
  lookup: (params = {}) => axiosClient.get('/consumable/lookup', { params }),
  getTransactions: (id) => axiosClient.get(`/consumable/${id}/transactions`),
  getLots: (id) => axiosClient.get(`/consumable/${id}/lots`),
  addLot: (id, data) => axiosClient.post(`/consumable/${id}/lots`, data),
  updateLot: (id, lotId, data) => axiosClient.put(`/consumable/${id}/lots/${lotId}`, data),
  create: (data) => axiosClient.post('/consumable', data),
  update: (id, data) => axiosClient.put(`/consumable/${id}`, data),
  delete: (id) => axiosClient.delete(`/consumable/${id}`)
};
