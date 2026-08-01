import axiosClient from './axiosClient';

export const maintenanceApi = {
  getAll: () => axiosClient.get('/maintenance'),
  create: (data) => axiosClient.post('/maintenance', data),
  complete: (id, data) => axiosClient.put(`/maintenance/${id}/complete`, data),
  delete: (id) => axiosClient.delete(`/maintenance/${id}`)
};
