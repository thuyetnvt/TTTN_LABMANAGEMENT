# API chính

Tất cả endpoint dưới đây có tiền tố `/api` và yêu cầu JWT trừ endpoint xác thực/health.

| Nhóm | Endpoint tiêu biểu | Quyền |
|---|---|---|
| Tài sản | `GET/POST/PUT /equipment`, `POST /equipment/{id}/inventory`, `GET /equipment/{id}/location-history`, `GET /equipment/export` | xem / quản lý |
| Vị trí | `GET /location`, `POST/PUT/DELETE /location/{id}` | xem / quản lý |
| Mượn trả | `POST /borrow`, `GET /borrow/history`, `PUT /borrow/{id}/approve`, `/return` | theo vai trò |
| Lịch sử mượn/trả Excel | `GET /borrow/history/export`, `POST /borrow/history/import/preview`, `POST /borrow/history/import` | quản lý |
| Bàn giao | `GET /handover`, `POST /handover` | quản lý |
| Minh chứng bàn giao | `POST/GET/DELETE /handover/{borrowRecordId}/evidence...` | quản lý |
| Bảo lãnh | `GET /borrow/teacher-pending`, `PUT /borrow/{id}/teacher-approve` | giảng viên |
| Kiểm kê | `GET/POST /inventory`, `POST /inventory/{id}/scan`, `/complete` | quản lý |
| Bảo trì | `GET/POST /maintenance`, `PUT /maintenance/{id}/complete` | quản lý |
| Lịch bảo trì | `GET/POST /maintenance-schedules`, `PUT/DELETE /maintenance-schedules/{id}`, `POST /maintenance-schedules/{id}/generate` | quản lý |
| Báo cáo | `GET /reports/summary`, `GET /reports/export`, `GET /reports/export.pdf` | quản lý |
| Import tài sản | `POST /equipment/import/preview`, `POST /equipment/import` | quản lý |
| Vật tư | `/consumable`, `/consumable-request` | theo vai trò |
| Thông báo | `GET /notification`, `/unread-count`, `PUT /notification/{id}/read` | chủ tài khoản |
| Kiểm tra | `GET /health` | công khai trong mạng triển khai |

## Xác thực và phiên đăng nhập

- `POST /auth/login` và `POST /auth/google` trả access token cùng refresh token.
- `POST /auth/refresh` luân chuyển refresh token; token cũ bị thu hồi ngay sau khi sử dụng.
- Phiên refresh mặc định có hạn 7 ngày và bị vô hiệu khi tài khoản khóa, mật khẩu đổi hoặc `TokenVersion` thay đổi.

## Import lịch sử mượn/trả

Quy trình gồm hai bước: gọi `/borrow/history/import/preview` để kiểm tra file, sau đó chỉ gọi `/borrow/history/import` khi toàn bộ dòng hợp lệ. Import chỉ nhận bản ghi lịch sử đã kết thúc, tối đa 500 dòng và không thay đổi trạng thái hiện tại của thiết bị.
