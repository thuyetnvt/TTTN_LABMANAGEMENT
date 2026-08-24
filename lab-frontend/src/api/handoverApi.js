import axiosClient from './axiosClient'

export const handoverApi = {
  getByBorrowRecord: (id) => axiosClient.get(`/handover/${id}`),
  create: (data) => axiosClient.post('/handover', data)
}
