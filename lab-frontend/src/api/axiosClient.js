import axios from "axios";

const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5248/api",
  headers: {
    "Content-Type": "application/json",
  },
});

// Interceptors cho request (Tự động gắn Token)
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token") || sessionStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  },
);

// Interceptors cho response (Xử lý lỗi hoặc chuẩn hóa data trả về)
axiosClient.interceptors.response.use(
  (response) => {
    if (response && response.data) {
      return response.data;
    }
    return response;
  },
  (error) => {
    if (error?.response?.status === 401) {
      for (const storage of [localStorage, sessionStorage]) {
        storage.removeItem("token");
        storage.removeItem("role");
      }

      if (window.location.pathname !== "/login") {
        window.location.assign("/login");
      }
    }

    console.error("API Error:", error);
    return Promise.reject(error);
  },
);

export default axiosClient;
