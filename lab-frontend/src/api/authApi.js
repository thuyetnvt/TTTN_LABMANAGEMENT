import axiosClient from './axiosClient'

const authApi = {
  login(credentials) {
    // Gọi API thật qua base URL được cấu hình cho môi trường hiện tại.
    return axiosClient.post('/auth/login', credentials)
  },
  googleLogin(data) {
    return axiosClient.post('/auth/google-login', data)
  },
  forgotPassword(data) {
    return axiosClient.post('/auth/forgot-password', data)
  },
  resetPassword(data) {
    return axiosClient.post('/auth/reset-password', data)
  },
}

export default authApi
