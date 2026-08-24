import axiosClient from './axiosClient';

export const userApi = {
  getAll: () => axiosClient.get('/users'),
  getTeachers: () => axiosClient.get('/users/teachers'),
  getMe: () => axiosClient.get('/users/me'),
  updateMe: (data) => axiosClient.put('/users/me/profile', data),
  create: (data) => axiosClient.post('/users', data),
  update: (id, data) => axiosClient.put(`/users/${id}`, data),
  changePassword: (data) => axiosClient.put('/users/me/password', data),
  delete: (id) => axiosClient.delete(`/users/${id}`)
};
