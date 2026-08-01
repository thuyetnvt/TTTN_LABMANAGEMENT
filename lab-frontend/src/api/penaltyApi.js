import axiosClient from './axiosClient';

export const penaltyApi = {
  getAll: () => axiosClient.get('/penalty'),
  pay: (id) => axiosClient.put(`/penalty/${id}/pay`)
};
