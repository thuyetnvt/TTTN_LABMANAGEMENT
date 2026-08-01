import axiosClient from './axiosClient';

export const userApi = {
  getAll: () => axiosClient.get('/users'),
  getTeachers: () => axiosClient.get('/users/teachers'),
  create: (data) => axiosClient.post('/users', data),
  update: (id, data) => axiosClient.put(`/users/${id}`, data),
  changePassword: (data) => axiosClient.put('/users/me/password', data),
  delete: (id) => axiosClient.delete(`/users/${id}`)
};
