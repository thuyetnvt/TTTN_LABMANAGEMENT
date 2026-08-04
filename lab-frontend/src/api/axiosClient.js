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
    if (error.response) {
      const status = error.response.status;
      if (status === 401) {
        for (const storage of [localStorage, sessionStorage]) {
          storage.removeItem("token");
          storage.removeItem("role");
        }
        if (window.location.pathname !== "/login") {
          window.location.assign("/login");
        }
        error.message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
      } else if (status === 400) {
        error.message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
      } else if (status === 403) {
        error.message = "Bạn không có quyền thực hiện thao tác này.";
      } else if (status === 404) {
        error.message = "Không tìm thấy dữ liệu hoặc máy chủ.";
      } else if (status === 500) {
        error.message = "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.";
      } else {
        error.message = error.response.data?.message || "Đã có lỗi xảy ra. Vui lòng thử lại.";
      }
    } else if (error.request) {
      error.message = "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.";
    } else {
      error.message = "Lỗi hệ thống: " + error.message;
    }

    console.error("API Error:", error);
    return Promise.reject(error);
  },
);

export default axiosClient;
