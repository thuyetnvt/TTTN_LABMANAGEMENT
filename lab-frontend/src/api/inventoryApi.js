import axiosClient from './axiosClient'

export const inventoryApi = {
  getAll: () => axiosClient.get('/inventory'),
  getById: (id) => axiosClient.get(`/inventory/${id}`),
  create: (data) => axiosClient.post('/inventory', data),
  scan: (id, data) => axiosClient.post(`/inventory/${id}/scan`, data),
  complete: (id) => axiosClient.post(`/inventory/${id}/complete`)
}
