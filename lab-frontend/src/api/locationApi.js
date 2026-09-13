import axiosClient from './axiosClient'

const withoutLabRoot = (locations) => (Array.isArray(locations) ? locations : [])
  .filter(location => String(location.code || '').toUpperCase() !== 'LAB-ROOT')
  .map(({ parentId: _parentId, ...location }) => location)

const withoutParent = ({ parentId: _parentId, ...data }) => data

export const locationApi = {
  getAll: async () => withoutLabRoot(await axiosClient.get('/location')),
  create: (data) => axiosClient.post('/location', withoutParent(data)),
  update: (id, data) => axiosClient.put(`/location/${id}`, withoutParent(data)),
  remove: (id) => axiosClient.delete(`/location/${id}`)
}
