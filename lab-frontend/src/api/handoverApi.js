import axiosClient from './axiosClient'

export const handoverApi = {
  getByBorrowRecord: (id) => axiosClient.get(`/handover/${id}`),
  create: (data) => axiosClient.post('/handover', data),
  confirmReceipt: (borrowRecordId) => axiosClient.post(`/handover/${borrowRecordId}/confirm-receipt`),
  createIssueReport: (borrowRecordId, data) => {
    const form = new FormData()
    form.append('equipmentId', data.equipmentId)
    form.append('issueType', data.issueType)
    form.append('description', data.description)
    for (const file of data.files || []) form.append('files', file)
    return axiosClient.post(`/handover/${borrowRecordId}/issue-reports`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },
  getIssueReports: (status) => axiosClient.get('/handover/issue-reports', { params: status ? { status } : {} }),
  resolveIssueReport: (id, data) => axiosClient.put(`/handover/issue-reports/${id}/resolve`, data),
  downloadIssueEvidence: (reportId, evidenceId) => axiosClient.get(
    `/handover/issue-reports/${reportId}/evidence/${evidenceId}`,
    { responseType: 'blob' }
  ),
  uploadEvidence: (borrowRecordId, file, evidenceType, equipmentId = null) => {
    const form = new FormData()
    form.append('file', file)
    form.append('evidenceType', evidenceType)
    if (equipmentId) form.append('equipmentId', equipmentId)
    return axiosClient.post(`/handover/${borrowRecordId}/evidence`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  }
}
