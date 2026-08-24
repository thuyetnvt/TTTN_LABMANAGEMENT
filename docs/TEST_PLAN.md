# Kế hoạch kiểm thử

- Build: `dotnet build --no-restore`, `npm run build`.
- Migration: `dotnet ef migrations script --idempotent` và kiểm tra trên database bản sao.
- RBAC/IDOR: thử từng vai trò gọi endpoint của vai trò khác.
- Mượn trả: multi-item, bắt buộc bảo lãnh, duyệt đồng thời, trả từng món, hỏng/bảo hành/bồi thường.
- Kiểm kê: token hợp lệ, token ngoài phạm vi, sai vị trí, kết thúc đánh dấu thiếu.
- Vật tư: hai yêu cầu đồng thời, không âm kho, giao dịch trước/sau.
- Upload: extension hợp lệ, magic bytes sai, file quá cỡ, path traversal.
- Vận hành: Docker health, restart container, backup/restore.

