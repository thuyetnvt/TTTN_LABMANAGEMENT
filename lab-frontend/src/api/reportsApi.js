import axiosClient from './axiosClient'

const cleanParams = (params = {}) => Object.fromEntries(Object.entries(params).filter(([, value]) => value !== null && value !== undefined && value !== ''))

export const reportsApi = {
  summary: (params) => axiosClient.get('/reports/summary', { params: cleanParams(params) }),
  export: (params) => axiosClient.get('/reports/export', { params: cleanParams(params), responseType: 'blob' }),
  exportPdf: (params) => axiosClient.get('/reports/export.pdf', { params: cleanParams(params), responseType: 'blob' })
}
