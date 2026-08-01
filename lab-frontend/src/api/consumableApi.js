import axiosClient from './axiosClient';

export const consumableApi = {
  getAll: () => axiosClient.get('/consumable'),
  getTransactions: (id) => axiosClient.get(`/consumable/${id}/transactions`),
  create: (data) => axiosClient.post('/consumable', data),
  update: (id, data) => axiosClient.put(`/consumable/${id}`, data),
  delete: (id) => axiosClient.delete(`/consumable/${id}`)
};
