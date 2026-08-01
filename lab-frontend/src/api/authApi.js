import axiosClient from './axiosClient'

const authApi = {
  login(credentials) {
    // Gọi API thật tới C# Backend (POST http://localhost:5248/api/auth/login)
    return axiosClient.post('/auth/login', credentials)
  },
  forgotPassword(data) {
    return axiosClient.post('/auth/forgot-password', data)
  },
  resetPassword(data) {
    return axiosClient.post('/auth/reset-password', data)
  },
}

export default authApi
