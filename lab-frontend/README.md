# LabManagement Frontend

Frontend Vue 3 cho hệ thống quản lý phòng lab.

## Yêu cầu

- Node.js 22+
- Backend LabManagement đang chạy

## Chạy môi trường phát triển

```bash
cp .env.example .env
npm ci
npm run dev
```

Các biến môi trường:

```text
VITE_API_BASE_URL=http://localhost:5248/api
VITE_SIGNALR_URL=http://localhost:5248/notificationHub
```

## Kiểm tra bản production

```bash
npm ci
npm run build
npm run preview
```

## Build Docker

Nginx trong image reverse proxy `/api` và `/notificationHub` tới container có tên
`backend` trên cùng Docker network. Cách khuyến nghị là chạy bằng file
`docker-compose.yml` ở thư mục gốc của bộ full-stack.

```bash
docker build \
  --build-arg VITE_API_BASE_URL=/api \
  --build-arg VITE_SIGNALR_URL=/notificationHub \
  -t labmanagement-frontend .
```

## Phiên đăng nhập và đặt lại mật khẩu

Frontend sử dụng:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

Khi access token hết hạn, frontend dùng refresh token một lần để nhận cặp token mới rồi tự gửi lại request đang dở. Nếu refresh thất bại, phiên được xóa và người dùng quay về trang đăng nhập.

Backend cần gửi liên kết dạng:

```text
https://your-domain/reset-password?token=<reset-token>
```

Trong dashboard, bấm avatar góc phải để đổi mật khẩu. Sau khi đổi, token cũ bị
thu hồi và người dùng phải đăng nhập lại.
