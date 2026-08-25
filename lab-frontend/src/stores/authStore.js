import { defineStore } from 'pinia'
import authApi from '../api/authApi'

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
  }),
  actions: {
    setUser(profile) {
      this.user = profile ? { ...(this.user || {}), ...profile } : null
      if (profile?.role) this.role = profile.role
    },
    async login(username, password, remember = false) {
      try {
        const data = await authApi.login({ username, password })
        this.token = data.token
        this.role = data.role
        this.user = { username: data.username || username, role: data.role }
        
        clearAuthStorage()
        const storage = remember ? localStorage : sessionStorage
        storage.setItem('token', this.token)
        storage.setItem('role', this.role)
        return true
      } catch (error) {
        throw new Error(error?.response?.data?.message || error?.response?.data?.detail || error.message || 'Đăng nhập thất bại')
      }
    },
    logout() {
      this.token = null
      this.role = 'Guest'
      this.user = null
      clearAuthStorage()
    }
  }
})
