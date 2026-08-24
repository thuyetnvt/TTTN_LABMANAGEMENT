import axiosClient from './axiosClient';

export const borrowApi = {
  createRequest: (data) => axiosClient.post('/borrow', data),
  getPendingRequests: () => axiosClient.get('/borrow/pending'),
  getHistory: () => axiosClient.get('/borrow/history'),
  approve: (id) => axiosClient.put(`/borrow/${id}/approve`),
  reject: (id) => axiosClient.put(`/borrow/${id}/reject`),
  returnEquipment: (id, data = null) => axiosClient.put(`/borrow/${id}/return`, data),
  reportDamage: (id, data) => axiosClient.put(`/borrow/${id}/report-damage`, data),
  getTeacherPending: () => axiosClient.get('/borrow/teacher-pending'),
  teacherApprove: (id, note) => axiosClient.put(`/borrow/${id}/teacher-approve`, { note }),
  teacherReject: (id, note) => axiosClient.put(`/borrow/${id}/teacher-reject`, { note }),
  remind: (id) => axiosClient.post(`/borrow/${id}/remind`)
};
