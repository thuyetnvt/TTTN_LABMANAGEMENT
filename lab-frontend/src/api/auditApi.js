import axiosClient from './axiosClient'

export const auditApi = {
  getLogs: (params = {}) => axiosClient.get('/audit', { params })
}
