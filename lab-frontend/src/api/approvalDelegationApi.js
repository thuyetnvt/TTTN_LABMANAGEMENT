import axiosClient from './axiosClient'

export const approvalDelegationApi = {
  getMine: () => axiosClient.get('/approval-delegations/me'),
  getAll: () => axiosClient.get('/approval-delegations'),
  create: data => axiosClient.post('/approval-delegations', data),
  revoke: id => axiosClient.put(`/approval-delegations/${id}/revoke`)
}
