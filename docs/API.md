# API chính

Tất cả endpoint dưới đây có tiền tố `/api` và yêu cầu JWT trừ endpoint xác thực/health.

| Nhóm | Endpoint tiêu biểu | Quyền |
|---|---|---|
| Tài sản | `GET/POST/PUT /equipment`, `POST /equipment/{id}/inventory`, `GET /equipment/export` | xem / quản lý |
| Vị trí | `GET /location`, `POST/PUT/DELETE /location/{id}` | xem / quản lý |
| Mượn trả | `POST /borrow`, `GET /borrow/history`, `PUT /borrow/{id}/approve`, `/return` | theo vai trò |
| Bảo lãnh | `GET /borrow/teacher-pending`, `PUT /borrow/{id}/teacher-approve` | giảng viên |
| Kiểm kê | `GET/POST /inventory`, `POST /inventory/{id}/scan`, `/complete` | quản lý |
| Bảo trì | `GET/POST /maintenance`, `PUT /maintenance/{id}/complete` | quản lý |
| Vật tư | `/consumable`, `/consumable-request` | theo vai trò |
| Thông báo | `GET /notification`, `/unread-count`, `PUT /notification/{id}/read` | chủ tài khoản |
| Kiểm tra | `GET /health` | công khai trong mạng triển khai |

