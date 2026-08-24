# Kết quả kiểm thử đã chạy

Ngày 25/08/2026 trên branch `codex/iot-lab-asset-upgrade`:

- `dotnet build --no-restore`: đạt, 0 warning/0 error.
- `npm run build`: đạt; còn warning dependency SignalR về `PURE` annotation và cảnh báo chunk lớn.
- Baseline Docker Compose/health: đã ghi tại `docs/BASELINE_AUDIT.md`; compose hợp lệ và `/health` từng trả HTTP 200.
- Docker sau module bàn giao: `docker compose up -d --build backend frontend` đạt; cả ba service healthy, migration bàn giao áp dụng thành công và backend `/health` trả `Healthy`.
- Chưa có test project tự động và chưa chạy E2E browser trong môi trường này; các mục đó không được tuyên bố đạt.
