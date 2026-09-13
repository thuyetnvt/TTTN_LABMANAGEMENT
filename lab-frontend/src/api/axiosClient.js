import axios from "axios";

// Use Vite's same-origin proxy in development. Docker injects the same /api
// value for production, while an explicit VITE_API_BASE_URL still works for
// a separately hosted backend.
export const apiBaseUrl = import.meta.env?.VITE_API_BASE_URL || "/api";
export const apiTimeoutMs = Number(import.meta.env?.VITE_API_TIMEOUT_MS) || 15000;

const axiosClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: apiTimeoutMs,
  headers: {
    "Content-Type": "application/json",
  },
});

let refreshPromise = null;

const getSessionStorage = () => {
  if (localStorage.getItem("refreshToken")) return localStorage;
  if (sessionStorage.getItem("refreshToken")) return sessionStorage;
  return null;
};

const clearSession = () => {
  for (const storage of [localStorage, sessionStorage]) {
    storage.removeItem("token");
    storage.removeItem("refreshToken");
    storage.removeItem("role");
  }
};

const redirectToLogin = () => {
  clearSession();
  if (window.location.pathname !== "/login") window.location.assign("/login");
};

const refreshSession = async () => {
  const storage = getSessionStorage();
  const refreshToken = storage?.getItem("refreshToken");
  if (!storage || !refreshToken) throw new Error("Không có refresh token.");

  const response = await axios.post(
    `${apiBaseUrl}/auth/refresh`,
    { refreshToken },
    { timeout: apiTimeoutMs, headers: { "Content-Type": "application/json" } },
  );
  storage.setItem("token", response.data.token);
  storage.setItem("refreshToken", response.data.refreshToken);
  if (response.data.role) storage.setItem("role", response.data.role);
  return response.data.token;
};

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
  async (error) => {
    if (error.response) {
      const status = error.response.status;
      const originalRequest = error.config || {};
      const isAuthRequest = String(originalRequest.url || "").includes("/auth/");
      if (status === 401 && !originalRequest._retry && !isAuthRequest && getSessionStorage()) {
        originalRequest._retry = true;
        try {
          if (!refreshPromise) refreshPromise = refreshSession().finally(() => { refreshPromise = null; });
          const token = await refreshPromise;
          originalRequest.headers = originalRequest.headers || {};
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return axiosClient(originalRequest);
        } catch {
          redirectToLogin();
          error.message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
          return Promise.reject(error);
        }
      } else if (status === 401) {
        if (!isAuthRequest) redirectToLogin();
        error.message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
      } else if (status === 400) {
        error.message = error.response.data?.message || "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
      } else if (status === 403) {
        error.message = "Bạn không có quyền thực hiện thao tác này.";
      } else if (status === 404) {
        error.message = error.response.data?.message || "Không tìm thấy dữ liệu hoặc máy chủ.";
      } else if (status === 500) {
        error.message = error.response.data?.message || "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.";
      } else {
        error.message = error.response.data?.message || "Đã có lỗi xảy ra. Vui lòng thử lại.";
      }
    } else if (error.code === "ECONNABORTED" || error.code === "ETIMEDOUT") {
      error.message = "Máy chủ phản hồi quá lâu. Vui lòng thử lại sau.";
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
