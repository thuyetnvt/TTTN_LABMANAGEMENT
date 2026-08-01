import axiosClient from './axiosClient';

export const consumableRequestApi = {
  getAll: () => axiosClient.get('/consumablerequest'),
  create: (data) => axiosClient.post('/consumablerequest', data),
  approve: (id) => axiosClient.put(`/consumablerequest/${id}/approve`),
  reject: (id) => axiosClient.put(`/consumablerequest/${id}/reject`)
};
