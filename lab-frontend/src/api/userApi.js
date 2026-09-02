import axiosClient, { apiBaseUrl } from './axiosClient';

export const userApi = {
  getAll: () => axiosClient.get('/users'),
  getPaged: (params = {}) => axiosClient.get('/users/paged', { params }),
  getTeachers: () => axiosClient.get('/users/teachers'),
  getMe: () => axiosClient.get('/users/me'),
  updateMe: (data) => axiosClient.put('/users/me/profile', data),
  uploadAvatar: (file) => {
    const formData = new FormData()
    formData.append('file', file)
    return axiosClient.post('/users/me/avatar', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
  deleteAvatar: () => axiosClient.delete('/users/me/avatar'),
  // The own-avatar endpoint is explicitly /users/me/avatar. Using
  // /users/avatar falls through to the collection route and returns 405.
  avatarUrl: (userId = null) => `${apiBaseUrl}/users/${userId ? `${userId}/` : 'me/'}avatar`,
  create: (data) => axiosClient.post('/users', data),
  update: (id, data) => axiosClient.put(`/users/${id}`, data),
  changePassword: (data) => axiosClient.put('/users/me/password', data),
  delete: (id) => axiosClient.delete(`/users/${id}`),
  activate: (id) => axiosClient.put(`/users/${id}/activate`)
};
