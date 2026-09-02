import axiosClient from './axiosClient';

export const dashboardApi = {
  getStats: (refresh = false) => axiosClient.get('/dashboard/stats', {
    params: refresh ? { refresh: true } : undefined
  })
};
