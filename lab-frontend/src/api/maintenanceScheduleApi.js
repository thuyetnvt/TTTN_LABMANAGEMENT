import axiosClient from './axiosClient'

export const maintenanceScheduleApi = {
  getAll: () => axiosClient.get('/maintenance-schedules'),
  create: (data) => axiosClient.post('/maintenance-schedules', data),
  update: (id, data) => axiosClient.put(`/maintenance-schedules/${id}`, data),
  generate: (id) => axiosClient.post(`/maintenance-schedules/${id}/generate`),
  delete: (id) => axiosClient.delete(`/maintenance-schedules/${id}`)
}
