import { defineStore } from 'pinia'
import authApi from '../api/authApi'
import { approvalDelegationApi } from '../api/approvalDelegationApi'

const readStoredValue = (key) => localStorage.getItem(key) || sessionStorage.getItem(key)

const clearAuthStorage = () => {
  for (const storage of [localStorage, sessionStorage]) {
    storage.removeItem('token')
    storage.removeItem('role')
  }
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: readStoredValue('token') || null,
    role: readStoredValue('role') || 'Guest',
    user: null,
    approvalPermissions: {
      canApproveBorrow: false,
      canApproveConsumable: false,
      canHandoverBorrow: false,
      canHandoverConsumable: false,
      delegations: []
    },
  }),
  actions: {
    setUser(profile) {
      this.user = profile ? { ...(this.user || {}), ...profile } : null
      if (profile?.role) this.role = profile.role
    },
    async loadApprovalPermissions() {
      if (!this.token) return this.approvalPermissions
      try {
        const data = await approvalDelegationApi.getMine()
        this.approvalPermissions = data || {
          canApproveBorrow: false,
          canApproveConsumable: false,
          canHandoverBorrow: false,
          canHandoverConsumable: false,
          delegations: []
        }
      } catch (error) {
        // Managers still have access from their role. A failed optional
        // permissions lookup must not block the rest of the dashboard.
        this.approvalPermissions = {
          canApproveBorrow: false,
          canApproveConsumable: false,
          canHandoverBorrow: false,
          canHandoverConsumable: false,
          delegations: []
        }
        if (this.role !== 'Admin' && this.role !== 'Trưởng lab' && this.role !== 'Phó lab') throw error
      }
      return this.approvalPermissions
    },
    async login(username, password, remember = false) {
      try {
        const data = await authApi.login({ username, password })
        this.token = data.token
        this.role = data.role
        this.user = { username: data.username || username, role: data.role }
        this.approvalPermissions = { canApproveBorrow: false, canApproveConsumable: false, canHandoverBorrow: false, canHandoverConsumable: false, delegations: [] }
        
        clearAuthStorage()
        const storage = remember ? localStorage : sessionStorage
        storage.setItem('token', this.token)
        storage.setItem('role', this.role)
        return true
      } catch (error) {
        throw new Error(error?.response?.data?.message || error?.response?.data?.detail || error.message || 'Đăng nhập thất bại')
      }
    },
    async googleLogin(token) {
      try {
        const data = await authApi.googleLogin({ token })
        this.token = data.token
        this.role = data.role
        this.user = { username: data.username, role: data.role }
        this.approvalPermissions = { canApproveBorrow: false, canApproveConsumable: false, canHandoverBorrow: false, canHandoverConsumable: false, delegations: [] }
        
        clearAuthStorage()
        localStorage.setItem('token', this.token)
        localStorage.setItem('role', this.role)
        return true
      } catch (error) {
        throw new Error(error?.response?.data?.message || error?.response?.data?.detail || error.message || 'Đăng nhập Google thất bại')
      }
    },
    logout() {
      this.token = null
      this.role = 'Guest'
      this.user = null
      this.approvalPermissions = { canApproveBorrow: false, canApproveConsumable: false, canHandoverBorrow: false, canHandoverConsumable: false, delegations: [] }
      clearAuthStorage()
    }
  }
})
