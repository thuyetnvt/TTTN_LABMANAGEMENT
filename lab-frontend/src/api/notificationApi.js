import axiosClient from './axiosClient.js'

export const notificationApi = {
  getAll: (params = {}) => axiosClient.get('/notification', { params }),
  getUnreadCount: () => axiosClient.get('/notification/unread-count'),
  markRead: (id) => axiosClient.put(`/notification/${id}/read`),
  markAllRead: () => axiosClient.put('/notification/read-all')
}
