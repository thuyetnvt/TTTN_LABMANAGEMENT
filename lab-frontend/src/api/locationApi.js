import axiosClient from './axiosClient'

export const locationApi = {
  getAll: () => axiosClient.get('/location'),
  create: (data) => axiosClient.post('/location', data),
  update: (id, data) => axiosClient.put(`/location/${id}`, data),
  remove: (id) => axiosClient.delete(`/location/${id}`)
}
