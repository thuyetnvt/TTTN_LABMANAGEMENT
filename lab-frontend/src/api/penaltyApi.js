import axiosClient from './axiosClient';

export const penaltyApi = {
  getAll: () => axiosClient.get('/penalty'),
  getPaged: (params = {}) => axiosClient.get('/penalty/paged', { params }),
  pay: (id) => axiosClient.put(`/penalty/${id}/pay`)
};
