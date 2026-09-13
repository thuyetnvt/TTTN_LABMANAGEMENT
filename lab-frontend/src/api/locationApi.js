import axiosClient from './axiosClient'

const keepLabLocations = data => Array.isArray(data)
  ? data.filter(location => String(location?.type || '').toUpperCase() !== 'BUILDING')
  : []

export const locationApi = {
  getAll: async () => keepLabLocations(await axiosClient.get('/location')),
  create: (data) => axiosClient.post('/location', data),
  update: (id, data) => axiosClient.put(`/location/${id}`, data),
  remove: (id) => axiosClient.delete(`/location/${id}`)
}
