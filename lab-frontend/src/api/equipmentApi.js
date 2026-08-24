import axiosClient from './axiosClient';

export const equipmentApi = {
  getAll: () => axiosClient.get('/equipment'),
  
  create: (data) => axiosClient.post('/equipment', data, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  
  update: (id, data) => axiosClient.put(`/equipment/${id}`, data, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  
  delete: (id) => axiosClient.delete(`/equipment/${id}`),

  inventory: (id) => axiosClient.post(`/equipment/${id}/inventory`),

  downloadDecisionFile: (id) => axiosClient.get(`/equipment/${id}/decision-file`, { responseType: 'blob' }),

  export: () => axiosClient.get('/equipment/export', { responseType: 'blob' })
};
