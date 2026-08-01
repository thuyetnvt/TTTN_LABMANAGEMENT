import axiosClient from './axiosClient';

export const assetCategoryApi = {
  getAll: () => axiosClient.get('/assetcategory'),
  create: (data) => axiosClient.post('/assetcategory', data),
  update: (id, data) => axiosClient.put(`/assetcategory/${id}`, data),
  delete: (id) => axiosClient.delete(`/assetcategory/${id}`)
};
