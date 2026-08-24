import axiosClient from './axiosClient'

export const notificationApi = {
  getAll: () => axiosClient.get('/notification'),
  getUnreadCount: () => axiosClient.get('/notification/unread-count'),
  markRead: (id) => axiosClient.put(`/notification/${id}/read`),
  markAllRead: () => axiosClient.put('/notification/read-all')
}
