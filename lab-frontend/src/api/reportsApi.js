import axiosClient from './axiosClient'
import { getExportErrorMessage } from '../utils/reportsExport.js'

const cleanParams = (params = {}) => Object.fromEntries(Object.entries(params).filter(([, value]) => value !== null && value !== undefined && value !== ''))

const requestExport = async (url, params) => {
  try {
    return await axiosClient.get(url, { params: cleanParams(params), responseType: 'blob' })
  } catch (error) {
    error.message = await getExportErrorMessage(error)
    throw error
  }
}

export const reportsApi = {
  summary: (params) => axiosClient.get('/reports/summary', { params: cleanParams(params) }),
  export: (params) => requestExport('/reports/export', params),
  exportPdf: (params) => requestExport('/reports/export.pdf', params)
}
