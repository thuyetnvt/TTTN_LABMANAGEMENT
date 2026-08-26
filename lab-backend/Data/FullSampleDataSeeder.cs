using LabManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Data;

/// <summary>
/// Bộ dữ liệu mẫu đầy đủ cho môi trường phát triển/local.
/// Dữ liệu được đánh dấu bằng tiền tố SEED-FULL và có thể chạy lại an toàn.
/// Không bật seed này trên production.
/// </summary>
public static class FullSampleDataSeeder
{
    private const string Marker = "SeedFullSampleDataV2";
    private const string Prefix = "[SEED-FULL]";

    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
    {
        if (await context.AuditLogs.AnyAsync(log => log.Action == Marker))
        {
            return;
        }

        var password = configuration["Seed:DefaultPassword"]
            ?? throw new InvalidOperationException("Seed:DefaultPassword chưa được cấu hình.");
        var now = DateTime.UtcNow;

        var users = await EnsureUsersAsync(context, password, now);
        var categories = await EnsureCategoriesAsync(context, now);
        var locations = await EnsureLocationsAsync(context, now);
        var equipment = await EnsureEquipmentAsync(context, categories, locations, now);
        var consumables = await EnsureConsumablesAsync(context, categories, now);
        await context.SaveChangesAsync();

        var borrowRecords = await EnsureBorrowRecordsAsync(context, users, equipment, now);
        await EnsureConsumableRequestsAsync(context, users, consumables, now);
        var maintenanceRecords = await EnsureMaintenanceRecordsAsync(context, users, equipment, consumables, now);
        await EnsureMaintenanceSchedulesAsync(context, users, equipment, now);
        await EnsureInventorySessionsAsync(context, users, categories, locations, equipment, now);
        await EnsureHandoversAsync(context, users, equipment, borrowRecords, now);
        await EnsurePenaltiesAsync(context, users, equipment, borrowRecords, now);
        await EnsureConsumableTransactionsAsync(context, users, consumables, now);
        await EnsureBorrowStatusHistoryAsync(context, users, borrowRecords, now);
        await EnsureLocationHistoryAsync(context, users, locations, equipment, now);
        await EnsureNotificationsAsync(context, users, now);
        await EnsureAuditLogsAsync(context, users, now);

        context.AuditLogs.Add(new AuditLog
        {
            Username = "system",
            Action = Marker,
            EntityType = "Database",
            EntityId = Marker,
            Details = "Đã thêm bộ dữ liệu mẫu đầy đủ cho môi trường local.",
            IpAddress = "127.0.0.1",
            CreatedAt = now
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<string, int>> EnsureUsersAsync(
        AppDbContext context,
        string password,
        DateTime now)
    {
        var seeds = new[]
        {
            new UserSeed("admin2", "admin2@lab.local", "Nguyễn Minh Quản", "CB-ADMIN-002", "Quản trị hệ thống", null, Roles.Admin),
            new UserSeed("truonglab2", "truonglab2@lab.local", "Lê Hoàng Trưởng", "CB-LAB-002", "Phòng Lab IoT", null, Roles.LabHead),
            new UserSeed("pholab2", "pholab2@lab.local", "Phạm Ngọc Phó", "CB-LAB-003", "Phòng Lab IoT", null, Roles.DeputyLabHead),
            new UserSeed("giangvien2", "giangvien2@lab.local", "Đỗ Thanh Giảng", "GV-CNTT-002", "Khoa CNTT", null, Roles.Teacher),
            new UserSeed("giangvien3", "giangvien3@lab.local", "Vũ Thảo Giảng", "GV-CNTT-003", "Khoa CNTT", null, Roles.Teacher),
            new UserSeed("sv2", "sv2@lab.local", "Trần Minh An", "SV-CNTT-002", "Khoa CNTT", "D22CQCN02", Roles.Student),
            new UserSeed("sv3", "sv3@lab.local", "Nguyễn Khánh Linh", "SV-CNTT-003", "Khoa CNTT", "D22CQCN03", Roles.Student),
            new UserSeed("sv4", "sv4@lab.local", "Lê Gia Huy", "SV-CNTT-004", "Khoa CNTT", "D22CQCN04", Roles.Student),
            new UserSeed("sv5", "sv5@lab.local", "Phạm Hà My", "SV-CNTT-005", "Khoa CNTT", "D22CQCN05", Roles.Student),
            new UserSeed("sv6", "sv6@lab.local", "Đặng Quốc Bảo", "SV-CNTT-006", "Khoa CNTT", "D22CQCN06", Roles.Student),
            new UserSeed("sv7", "sv7@lab.local", "Hoàng Thu Trang", "SV-CNTT-007", "Khoa CNTT", "D22CQCN07", Roles.Student),
            new UserSeed("sv8", "sv8@lab.local", "Bùi Đức Anh", "SV-CNTT-008", "Khoa CNTT", "D22CQCN08", Roles.Student),
            new UserSeed("sv9", "sv9@lab.local", "Phan Ngọc Mai", "SV-CNTT-009", "Khoa CNTT", "D22CQCN09", Roles.Student),
            new UserSeed("sv10", "sv10@lab.local", "Mai Nhật Nam", "SV-CNTT-010", "Khoa CNTT", "D22CQCN10", Roles.Student)
        };

        var existing = await context.Users.ToDictionaryAsync(user => user.Username, user => user);
        foreach (var seed in seeds)
        {
            if (existing.TryGetValue(seed.Username, out var current))
            {
                if (string.IsNullOrWhiteSpace(current.FullName)) current.FullName = seed.FullName;
                if (string.IsNullOrWhiteSpace(current.UniversityCode)) current.UniversityCode = seed.UniversityCode;
                if (string.IsNullOrWhiteSpace(current.Department)) current.Department = seed.Department;
                if (string.IsNullOrWhiteSpace(current.ClassName)) current.ClassName = seed.ClassName;
                current.IsActive = true;
                continue;
            }

            var user = new User
            {
                Username = seed.Username,
                Email = seed.Email,
                FullName = seed.FullName,
                UniversityCode = seed.UniversityCode,
                Phone = $"09{seed.Username.GetHashCode():x8}"[..10],
                Department = seed.Department,
                ClassName = seed.ClassName,
                Role = seed.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = now.AddDays(-20)
            };
            context.Users.Add(user);
            existing[seed.Username] = user;
        }

        await context.SaveChangesAsync();
        return existing.ToDictionary(item => item.Key, item => item.Value.Id);
    }

    private static async Task<Dictionary<string, int>> EnsureCategoriesAsync(AppDbContext context, DateTime now)
    {
        var seeds = new[]
        {
            new CategorySeed("Robotics", "Robot, cơ cấu chấp hành và thiết bị tự động hóa"),
            new CategorySeed("Mạng & máy chủ", "Thiết bị mạng, máy chủ và lưu trữ dữ liệu"),
            new CategorySeed("Điện tử", "Mạch điện, nguồn và thiết bị hàn"),
            new CategorySeed("An toàn phòng lab", "Thiết bị hỗ trợ an toàn và bảo quản phòng lab")
        };

        var categories = await context.AssetCategories.ToDictionaryAsync(category => category.Name, category => category);
        foreach (var seed in seeds)
        {
            if (categories.ContainsKey(seed.Name)) continue;
            var category = new AssetCategory { Name = seed.Name, Description = seed.Description, CreatedAt = now.AddDays(-25) };
            context.AssetCategories.Add(category);
            categories[seed.Name] = category;
        }

        await context.SaveChangesAsync();
        return categories.ToDictionary(item => item.Key, item => item.Value.Id);
    }

    private static async Task<Dictionary<string, int>> EnsureLocationsAsync(AppDbContext context, DateTime now)
    {
        var seeds = new[]
        {
            new LocationSeed("LAB-ROOT", "Phòng Lab IoT", "BUILDING", null, "Khu vực quản lý chung của phòng lab"),
            new LocationSeed("LAB-IOT-A", "Phòng IoT A", "ROOM", "LAB-ROOT", "Khu thực hành IoT và vi điều khiển"),
            new LocationSeed("LAB-ELEC-B", "Phòng Điện tử B", "ROOM", "LAB-ROOT", "Khu mạch điện và hàn linh kiện"),
            new LocationSeed("LAB-AI-C", "Phòng AI C", "ROOM", "LAB-ROOT", "Khu máy tính và thị giác máy"),
            new LocationSeed("LAB-NET-D", "Phòng Mạng D", "ROOM", "LAB-ROOT", "Khu mạng và máy chủ"),
            new LocationSeed("LAB-STORE", "Kho vật tư", "STORE", "LAB-ROOT", "Kho vật tư tiêu hao"),
            new LocationSeed("LAB-MEAS", "Khu đo lường", "ROOM", "LAB-ROOT", "Khu máy đo và hiệu chuẩn"),
            new LocationSeed("LAB-SAFE", "Tủ an toàn", "CABINET", "LAB-ROOT", "Tủ lưu thiết bị an toàn")
        };

        var locations = await context.LocationNodes.ToDictionaryAsync(location => location.Code, location => location);
        foreach (var seed in seeds.Where(item => item.ParentCode is null))
        {
            if (locations.ContainsKey(seed.Code)) continue;
            var location = new LocationNode
            {
                Code = seed.Code,
                Name = seed.Name,
                Type = seed.Type,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = now.AddDays(-25)
            };
            context.LocationNodes.Add(location);
            locations[seed.Code] = location;
        }

        await context.SaveChangesAsync();
        foreach (var seed in seeds.Where(item => item.ParentCode is not null))
        {
            if (locations.ContainsKey(seed.Code)) continue;
            var location = new LocationNode
            {
                Code = seed.Code,
                Name = seed.Name,
                Type = seed.Type,
                ParentId = locations[seed.ParentCode!].Id,
                Description = seed.Description,
                IsActive = true,
                CreatedAt = now.AddDays(-24)
            };
            context.LocationNodes.Add(location);
            locations[seed.Code] = location;
        }

        await context.SaveChangesAsync();
        return locations.ToDictionary(item => item.Key, item => item.Value.Id);
    }

    private static async Task<Dictionary<string, Equipment>> EnsureEquipmentAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> categories,
        IReadOnlyDictionary<string, int> locations,
        DateTime now)
    {
        var seeds = new[]
        {
            new EquipmentSeed("TS-DEMO-001", "Bộ kit ESP32 DevKit V1", "ESP32 DevKit V1", "DEMO-SN-0001", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.Available, 180000, 18),
            new EquipmentSeed("TS-DEMO-002", "Arduino Mega 2560", "Arduino Mega 2560", "DEMO-SN-0002", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.Available, 420000, 16),
            new EquipmentSeed("TS-DEMO-003", "Raspberry Pi 5 8GB", "Raspberry Pi 5", "DEMO-SN-0003", "Phòng AI C", "LAB-AI-C", "AI", EquipmentStatuses.Borrowed, 2400000, 11),
            new EquipmentSeed("TS-DEMO-004", "NodeMCU ESP8266", "NodeMCU V3", "DEMO-SN-0004", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.Borrowed, 190000, 14),
            new EquipmentSeed("TS-DEMO-005", "Module LoRa SX1278", "Ra-02 SX1278", "DEMO-SN-0005", "Tủ an toàn", "LAB-SAFE", "IoT", EquipmentStatuses.Available, 220000, 9),
            new EquipmentSeed("TS-DEMO-006", "Module Zigbee CC2530", "CC2530 Zigbee", "DEMO-SN-0006", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.BorrowPending, 260000, 7),
            new EquipmentSeed("TS-DEMO-007", "Camera OpenMV H7 Plus", "OpenMV H7 Plus", "DEMO-SN-0007", "Phòng AI C", "LAB-AI-C", "AI", EquipmentStatuses.Available, 3500000, 10),
            new EquipmentSeed("TS-DEMO-008", "Jetson Nano 4GB", "Jetson Nano", "DEMO-SN-0008", "Phòng AI C", "LAB-AI-C", "AI", EquipmentStatuses.Borrowed, 3200000, 8),
            new EquipmentSeed("TS-DEMO-009", "Intel NUC i5", "NUC 12 Pro", "DEMO-SN-0009", "Phòng AI C", "LAB-AI-C", "AI", EquipmentStatuses.MaintenanceInProgress, 12500000, 6),
            new EquipmentSeed("TS-DEMO-010", "Máy hiện sóng Hantek 6022BE", "6022BE", "DEMO-SN-0010", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.UnderWarranty, 2100000, 12),
            new EquipmentSeed("TS-DEMO-011", "Đồng hồ vạn năng Keysight", "34465A", "DEMO-SN-0011", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.Broken, 28000000, 5),
            new EquipmentSeed("TS-DEMO-012", "Máy phân tích logic Saleae", "Logic 8", "DEMO-SN-0012", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.Available, 6200000, 8),
            new EquipmentSeed("TS-DEMO-013", "Máy phát tín hiệu Siglent", "SDG1032X", "DEMO-SN-0013", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.Borrowed, 11500000, 7),
            new EquipmentSeed("TS-DEMO-014", "Nguồn DC Rigol DP832", "DP832", "DEMO-SN-0014", "Bàn điện tử B1", "LAB-ELEC-B", "Điện tử", EquipmentStatuses.Available, 15000000, 13),
            new EquipmentSeed("TS-DEMO-015", "Camera nhiệt FLIR C5", "FLIR C5", "DEMO-SN-0015", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.UnderWarranty, 22000000, 4),
            new EquipmentSeed("TS-DEMO-016", "Trạm hàn Hakko FX-888D", "FX-888D", "DEMO-SN-0016", "Phòng Điện tử B", "LAB-ELEC-B", "Điện tử", EquipmentStatuses.MaintenanceInProgress, 4200000, 15),
            new EquipmentSeed("TS-DEMO-017", "Máy in 3D Bambu A1", "Bambu Lab A1", "DEMO-SN-0017", "Phòng Điện tử B", "LAB-ELEC-B", "Robotics", EquipmentStatuses.Available, 9500000, 3),
            new EquipmentSeed("TS-DEMO-018", "Cánh tay robot Dobot Magician", "Dobot Magician", "DEMO-SN-0018", "Phòng IoT A", "LAB-IOT-A", "Robotics", EquipmentStatuses.Broken, 28000000, 2),
            new EquipmentSeed("TS-DEMO-019", "Cảm biến LiDAR RPLIDAR A1", "RPLIDAR A1", "DEMO-SN-0019", "Phòng AI C", "LAB-AI-C", "Robotics", EquipmentStatuses.Available, 4200000, 6),
            new EquipmentSeed("TS-DEMO-020", "Đầu đọc RFID MFRC522", "MFRC522", "DEMO-SN-0020", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.Available, 150000, 10),
            new EquipmentSeed("TS-DEMO-021", "Tủ nhiệt mini", "MH-30", "DEMO-SN-0021", "Khu đo lường", "LAB-MEAS", "Thiết bị đo", EquipmentStatuses.UnderWarranty, 6800000, 1),
            new EquipmentSeed("TS-DEMO-022", "Router WiFi MikroTik", "hAP ax3", "DEMO-SN-0022", "Phòng Mạng D", "LAB-NET-D", "Mạng & máy chủ", EquipmentStatuses.Available, 3300000, 5),
            new EquipmentSeed("TS-DEMO-023", "NAS Synology DS224+", "DS224+", "DEMO-SN-0023", "Phòng Mạng D", "LAB-NET-D", "Mạng & máy chủ", EquipmentStatuses.Broken, 11000000, 2),
            new EquipmentSeed("TS-DEMO-024", "UPS APC 1200VA", "BX1200MI", "DEMO-SN-0024", "Phòng Mạng D", "LAB-NET-D", "Mạng & máy chủ", EquipmentStatuses.Available, 4200000, 4),
            new EquipmentSeed("TS-DEMO-025", "Máy đo chất lượng không khí", "AirVisual Pro", "DEMO-SN-0025", "Phòng IoT A", "LAB-IOT-A", "IoT", EquipmentStatuses.Available, 7800000, 2),
            new EquipmentSeed("TS-DEMO-026", "Bộ điều khiển động cơ", "TB6612FNG", "DEMO-SN-0026", "Phòng Điện tử B", "LAB-ELEC-B", "Điện tử", EquipmentStatuses.Available, 280000, 12),
            new EquipmentSeed("TS-DEMO-027", "Bộ camera giám sát lab", "Hikvision DS-2CD", "DEMO-SN-0027", "Tủ an toàn", "LAB-SAFE", "An toàn phòng lab", EquipmentStatuses.Available, 1800000, 1),
            new EquipmentSeed("TS-DEMO-028", "Máy hút ẩm phòng lab", "Sharp DW-D20A", "DEMO-SN-0028", "Kho vật tư", "LAB-STORE", "An toàn phòng lab", EquipmentStatuses.Available, 5200000, 1)
        };

        var existing = await context.Equipments.ToDictionaryAsync(item => item.Serial, item => item);
        foreach (var seed in seeds)
        {
            if (existing.ContainsKey(seed.Serial)) continue;
            var equipment = new Equipment
            {
                AssetCode = seed.AssetCode,
                QrToken = $"qr-{seed.Serial.ToLowerInvariant()}",
                Name = seed.Name,
                Model = seed.Model,
                Serial = seed.Serial,
                SerialName = seed.Serial,
                DeviceType = seed.Category,
                Manufacturer = "Thiết bị mẫu Lab IoT",
                Supplier = "Nhà cung cấp thiết bị mẫu",
                FundingSource = "Dữ liệu mẫu local",
                PurchaseValue = seed.PurchaseValue,
                Location = seed.LocationName,
                LocationNodeId = locations[seed.LocationCode],
                ResponsiblePerson = "Nguyễn Minh Quản",
                EntryDate = now.AddMonths(-seed.AgeMonths),
                WarrantyExpiry = now.AddMonths(seed.WarrantyMonths),
                InvoiceNumber = $"HD-SEED-{seed.AssetCode[^3..]}",
                Status = seed.Status,
                BorrowCount = seed.BorrowCount,
                AssetCategoryId = categories[seed.Category],
                Notes = $"{Prefix} Dữ liệu thiết bị dùng để kiểm thử giao diện và báo cáo.",
                CreatedAt = now.AddDays(-20)
            };
            context.Equipments.Add(equipment);
            existing[seed.Serial] = equipment;
        }

        await context.SaveChangesAsync();
        return existing.Values
            .Where(item => !string.IsNullOrWhiteSpace(item.AssetCode))
            .ToDictionary(item => item.AssetCode, item => item, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, int>> EnsureConsumablesAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> categories,
        DateTime now)
    {
        var seeds = new[]
        {
            new ConsumableSeed("VT-SEED-001", "Cảm biến nhiệt DS18B20", "cái", 42, 20, "IoT", "Kho vật tư A1"),
            new ConsumableSeed("VT-SEED-002", "Module relay 4 kênh", "cái", 16, 10, "IoT", "Kho vật tư A1"),
            new ConsumableSeed("VT-SEED-003", "Module RFID RC522", "cái", 28, 12, "IoT", "Kho vật tư A1"),
            new ConsumableSeed("VT-SEED-004", "Bộ dây Dupont cái-cái", "bộ", 7, 15, "Điện tử", "Kho vật tư A2"),
            new ConsumableSeed("VT-SEED-005", "Bộ dây Dupont đực-đực", "bộ", 65, 20, "Điện tử", "Kho vật tư A2"),
            new ConsumableSeed("VT-SEED-006", "Terminal Block 2P", "cái", 9, 25, "Điện tử", "Kho vật tư A2"),
            new ConsumableSeed("VT-SEED-007", "Pin Li-ion 18650", "viên", 34, 20, "An toàn phòng lab", "Tủ an toàn"),
            new ConsumableSeed("VT-SEED-008", "Hộp nhựa ABS 120x80", "hộp", 18, 8, "Robotics", "Kho vật tư B1"),
            new ConsumableSeed("VT-SEED-009", "Keo tản nhiệt", "tuýp", 3, 8, "An toàn phòng lab", "Tủ an toàn"),
            new ConsumableSeed("VT-SEED-010", "Ống co nhiệt nhiều cỡ", "cuộn", 22, 5, "Điện tử", "Kho vật tư A2"),
            new ConsumableSeed("VT-SEED-011", "Cáp USB-C data", "sợi", 31, 10, "Mạng & máy chủ", "Kho vật tư B1"),
            new ConsumableSeed("VT-SEED-012", "Đầu nối JST XH2.54", "bộ", 6, 12, "Điện tử", "Kho vật tư A2")
        };

        var existing = await context.Consumables.ToDictionaryAsync(item => item.Code, item => item);
        foreach (var seed in seeds)
        {
            if (existing.ContainsKey(seed.Code)) continue;
            var item = new Consumable
            {
                Code = seed.Code,
                Name = seed.Name,
                Unit = seed.Unit,
                Quantity = seed.Quantity,
                MinQuantity = seed.MinQuantity,
                ResponsiblePerson = "Nguyễn Minh Quản",
                AssetCategoryId = categories[seed.Category],
                EntryDate = now.AddMonths(-4),
                InvoiceNumber = $"VT-HD-{seed.Code[^3..]}",
                Supplier = "Nhà cung cấp vật tư mẫu",
                UnitCost = 15000 + seed.Quantity * 1000,
                StorageLocation = seed.StorageLocation,
                LotNumber = $"LOT-SEED-{seed.Code[^3..]}",
                ExpiryDate = now.AddYears(2),
                CreatedAt = now.AddDays(-18)
            };
            context.Consumables.Add(item);
            existing[seed.Code] = item;
        }

        await context.SaveChangesAsync();
        return existing.ToDictionary(item => item.Key, item => item.Value.Id);
    }

    private static async Task<List<BorrowRecord>> EnsureBorrowRecordsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, Equipment> equipment,
        DateTime now)
    {
        var requests = new[]
        {
            new BorrowSeed("sv2", "TS-DEMO-001", "giangvien2", BorrowStatuses.TeacherPending, -1, 8, "[SEED-FULL-BORROW-001] Bài thực hành MQTT và ESP32."),
            new BorrowSeed("sv3", "TS-DEMO-002", "giangvien2", BorrowStatuses.Pending, -2, 5, "[SEED-FULL-BORROW-002] Lập trình Arduino nâng cao."),
            new BorrowSeed("sv4", "TS-DEMO-003", "giangvien3", BorrowStatuses.ProcessingApproval, -1, 10, "[SEED-FULL-BORROW-003] Huấn luyện mô hình nhận diện ảnh."),
            new BorrowSeed("sv5", "TS-DEMO-004", "giangvien3", BorrowStatuses.Borrowed, -12, -4, "[SEED-FULL-BORROW-004] Thu thập dữ liệu cảm biến ngoài trời."),
            new BorrowSeed("sv6", "TS-DEMO-005", "giangvien2", BorrowStatuses.Borrowed, -2, 6, "[SEED-FULL-BORROW-005] Thử nghiệm truyền dữ liệu LoRa."),
            new BorrowSeed("sv7", "TS-DEMO-006", "giangvien2", BorrowStatuses.ReturnProcessing, -6, 1, "[SEED-FULL-BORROW-006] Kiểm thử mạng Zigbee."),
            new BorrowSeed("sv8", "TS-DEMO-007", "giangvien3", BorrowStatuses.Returned, -30, -22, "[SEED-FULL-BORROW-007] Chụp dữ liệu camera cho đồ án.", true),
            new BorrowSeed("sv9", "TS-DEMO-008", "giangvien3", BorrowStatuses.ReturnedDamaged, -45, -37, "[SEED-FULL-BORROW-008] Thực hành xử lý ảnh trên Jetson.", true),
            new BorrowSeed("sv10", "TS-DEMO-009", "giangvien2", BorrowStatuses.Rejected, -8, -1, "[SEED-FULL-BORROW-009] Dự án không đủ điều kiện mượn thiết bị."),
            new BorrowSeed("giangvien2", "TS-DEMO-010", null, BorrowStatuses.Borrowed, -9, -2, "[SEED-FULL-BORROW-010] Hiệu chuẩn thiết bị đo.")
        };

        var result = new List<BorrowRecord>();
        foreach (var seed in requests)
        {
            if (!users.TryGetValue(seed.Username, out var userId)
                || !equipment.TryGetValue(seed.AssetCode, out var item))
            {
                continue;
            }

            var existing = await context.BorrowRecords
                .Include(record => record.Details)
                .FirstOrDefaultAsync(record => record.Purpose == seed.Purpose);
            if (existing is not null)
            {
                result.Add(existing);
                continue;
            }

            DateTime? returnedAt = seed.IsReturned ? now.AddDays(seed.ExpectedReturnOffset) : null;
            var record = new BorrowRecord
            {
                UserId = userId,
                EquipmentId = item.Id,
                TeacherId = seed.TeacherUsername is not null && users.TryGetValue(seed.TeacherUsername, out var teacherId) ? teacherId : null,
                BorrowDate = now.AddDays(seed.BorrowOffset),
                ExpectedReturnDate = now.AddDays(seed.ExpectedReturnOffset),
                ActualReturnDate = returnedAt,
                Purpose = seed.Purpose,
                Status = seed.Status,
                ReturnCondition = seed.Status == BorrowStatuses.ReturnedDamaged ? EquipmentStatuses.Broken : EquipmentStatuses.Available,
                ReturnInspectionNote = seed.Status == BorrowStatuses.ReturnedDamaged ? "Vỏ thiết bị có vết nứt, cần kiểm tra." : seed.IsReturned ? "Đã kiểm tra, đủ phụ kiện." : string.Empty,
                WarrantyAction = seed.Status == BorrowStatuses.ReturnedDamaged ? "Kiểm tra bồi thường" : string.Empty,
                IsUnderWarrantyAtReturn = seed.Status == BorrowStatuses.ReturnedDamaged ? false : seed.IsReturned,
                CompensationAmount = seed.Status == BorrowStatuses.ReturnedDamaged ? 1800000 : 0,
                InspectedByUserId = users.GetValueOrDefault("pholab"),
                Details =
                [
                    new BorrowRequestDetail
                    {
                        EquipmentId = item.Id,
                        Quantity = 1,
                        Note = seed.Purpose,
                        Status = seed.Status,
                        ReturnCondition = seed.Status == BorrowStatuses.ReturnedDamaged ? EquipmentStatuses.Broken : EquipmentStatuses.Available,
                        ReturnNote = seed.IsReturned ? "Đã nhận lại thiết bị." : string.Empty,
                        ReturnedAt = returnedAt,
                        CompensationAmount = seed.Status == BorrowStatuses.ReturnedDamaged ? 1800000 : 0
                    }
                ]
            };
            context.BorrowRecords.Add(record);
            await context.SaveChangesAsync();
            result.Add(record);
        }

        return result;
    }

    private static async Task EnsureConsumableRequestsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, int> consumables,
        DateTime now)
    {
        var requests = new[]
        {
            new ConsumableRequestSeed("sv2", "VT-SEED-001", 8, ConsumableRequestStatuses.Pending, "[SEED-FULL-REQ-001] Thực hành cảm biến nhiệt."),
            new ConsumableRequestSeed("sv3", "VT-SEED-002", 4, ConsumableRequestStatuses.Processing, "[SEED-FULL-REQ-002] Chuẩn bị mạch relay."),
            new ConsumableRequestSeed("sv4", "VT-SEED-003", 6, ConsumableRequestStatuses.Issued, "[SEED-FULL-REQ-003] Làm bài thực hành RFID."),
            new ConsumableRequestSeed("sv5", "VT-SEED-004", 10, ConsumableRequestStatuses.Rejected, "[SEED-FULL-REQ-004] Vật tư vượt định mức."),
            new ConsumableRequestSeed("sv6", "VT-SEED-005", 12, ConsumableRequestStatuses.Pending, "[SEED-FULL-REQ-005] Lắp ráp bộ dây thực hành."),
            new ConsumableRequestSeed("sv7", "VT-SEED-006", 20, ConsumableRequestStatuses.Issued, "[SEED-FULL-REQ-006] Hoàn thiện mạch nguồn."),
            new ConsumableRequestSeed("sv8", "VT-SEED-007", 5, ConsumableRequestStatuses.Processing, "[SEED-FULL-REQ-007] Cấp pin cho robot."),
            new ConsumableRequestSeed("sv9", "VT-SEED-009", 2, ConsumableRequestStatuses.Pending, "[SEED-FULL-REQ-008] Bảo trì máy tính lab.")
        };

        foreach (var seed in requests)
        {
            if (!users.TryGetValue(seed.Username, out var userId) || !consumables.TryGetValue(seed.Code, out var consumableId)) continue;
            if (await context.ConsumableRequests.AnyAsync(item => item.Reason == seed.Reason)) continue;

            context.ConsumableRequests.Add(new ConsumableRequest
            {
                UserId = userId,
                ConsumableId = consumableId,
                Quantity = seed.Quantity,
                Reason = seed.Reason,
                Status = seed.Status,
                RequestDate = now.AddDays(-seed.Quantity),
                ApprovalDate = seed.Status is ConsumableRequestStatuses.Issued or ConsumableRequestStatuses.Rejected ? now.AddDays(-seed.Quantity + 1) : null
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<List<MaintenanceRecord>> EnsureMaintenanceRecordsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, Equipment> equipment,
        IReadOnlyDictionary<string, int> consumables,
        DateTime now)
    {
        var seeds = new[]
        {
            new MaintenanceSeed("TS-DEMO-009", MaintenanceStatuses.InProgress, 1, 450000, "Kiểm tra ổ cứng và cập nhật môi trường AI.", "Kỹ thuật lab", "Nhà cung cấp thiết bị mẫu"),
            new MaintenanceSeed("TS-DEMO-016", MaintenanceStatuses.Completing, 3, 180000, "Thay mũi hàn và kiểm tra nhiệt độ.", "Kỹ thuật điện tử", "Hakko Việt Nam"),
            new MaintenanceSeed("TS-DEMO-010", MaintenanceStatuses.Completed, 5, 250000, "Hiệu chuẩn máy hiện sóng.", "Kỹ thuật đo lường", "Rigol Service"),
            new MaintenanceSeed("TS-DEMO-011", MaintenanceStatuses.Completed, 8, 900000, "Kiểm tra và thay cầu chì bảo vệ.", "Kỹ thuật đo lường", "Keysight Service"),
            new MaintenanceSeed("TS-DEMO-017", MaintenanceStatuses.Completed, 12, 320000, "Vệ sinh đầu phun và cân bàn máy in.", "Kỹ thuật cơ khí", "Bambu Lab Service"),
            new MaintenanceSeed("TS-DEMO-023", MaintenanceStatuses.Completed, 15, 600000, "Kiểm tra nguồn và ổ đĩa NAS.", "Kỹ thuật mạng", "Synology Service")
        };

        var records = new List<MaintenanceRecord>();
        foreach (var seed in seeds)
        {
            if (!equipment.TryGetValue(seed.AssetCode, out var item)) continue;
            var existing = await context.MaintenanceRecords
                .FirstOrDefaultAsync(record => record.EquipmentId == item.Id && record.Description.StartsWith(Prefix));
            if (existing is not null)
            {
                records.Add(existing);
                continue;
            }

            var record = new MaintenanceRecord
            {
                EquipmentId = item.Id,
                MaintenanceDate = now.AddDays(-seed.DaysAgo),
                Description = $"{Prefix} {seed.Description}",
                Cost = seed.Cost,
                PerformedBy = seed.PerformedBy,
                Supplier = seed.Supplier,
                Status = seed.Status,
                CompletedAt = seed.Status == MaintenanceStatuses.Completed ? now.AddDays(-seed.DaysAgo + 1) : null,
                Result = seed.Status == MaintenanceStatuses.Completed ? "Thiết bị hoạt động ổn định sau bảo trì." : string.Empty,
                ResultStatus = seed.Status == MaintenanceStatuses.Completed ? EquipmentStatuses.Available : EquipmentStatuses.MaintenanceInProgress,
                ActiveEquipmentKey = seed.Status == MaintenanceStatuses.Completed ? null : $"SEED-FULL-MAINT-{item.Id}",
                Checklist = "Kiểm tra nguồn\nKiểm tra kết nối\nVệ sinh thiết bị\nChạy thử chức năng",
                ChecklistResult = seed.Status == MaintenanceStatuses.Completed ? "Đạt" : string.Empty
            };
            context.MaintenanceRecords.Add(record);
            await context.SaveChangesAsync();
            records.Add(record);

            if (consumables.TryGetValue("VT-SEED-010", out var consumableId))
            {
                context.MaintenancePartUsages.Add(new MaintenancePartUsage
                {
                    MaintenanceRecordId = record.Id,
                    ConsumableId = consumableId,
                    Quantity = 1,
                    UnitCost = 25000,
                    Note = $"{Prefix} Vật tư dùng cho phiếu bảo trì."
                });
            }
        }

        await context.SaveChangesAsync();
        return records;
    }

    private static async Task EnsureMaintenanceSchedulesAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, Equipment> equipment,
        DateTime now)
    {
        if (!users.TryGetValue("truonglab", out var managerId)) return;

        var seeds = new[]
        {
            new ScheduleSeed("TS-DEMO-001", "Kiểm tra kit ESP32 hàng tháng", 30, 5),
            new ScheduleSeed("TS-DEMO-003", "Kiểm tra Raspberry Pi hàng quý", 90, 20),
            new ScheduleSeed("TS-DEMO-010", "Hiệu chuẩn máy đo hàng quý", 90, -2),
            new ScheduleSeed("TS-DEMO-014", "Kiểm tra nguồn DC hàng tháng", 30, 12),
            new ScheduleSeed("TS-DEMO-017", "Bảo dưỡng máy in 3D", 60, 35),
            new ScheduleSeed("TS-DEMO-022", "Kiểm tra router và firmware", 90, 70),
            new ScheduleSeed("TS-DEMO-023", "Kiểm tra NAS và sao lưu", 30, 8),
            new ScheduleSeed("TS-DEMO-028", "Kiểm tra máy hút ẩm", 30, 3)
        };

        foreach (var seed in seeds)
        {
            if (!equipment.TryGetValue(seed.AssetCode, out var item)) continue;
            if (await context.MaintenanceSchedules.AnyAsync(schedule => schedule.EquipmentId == item.Id && schedule.Name.StartsWith(Prefix))) continue;

            context.MaintenanceSchedules.Add(new MaintenanceSchedule
            {
                EquipmentId = item.Id,
                Name = $"{Prefix} {seed.Name}",
                IntervalDays = seed.IntervalDays,
                IntervalUnit = "DAY",
                NextDueAt = now.AddDays(seed.NextDueOffset),
                IsActive = true,
                Notes = "Lịch mẫu để kiểm thử tạo phiếu và nhắc hạn bảo trì.",
                Checklist = "Kiểm tra ngoại quan\nKiểm tra nguồn\nGhi nhận kết quả",
                CreatedByUserId = managerId,
                CreatedAt = now.AddDays(-18),
                UpdatedAt = now.AddDays(-2)
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureInventorySessionsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, int> categories,
        IReadOnlyDictionary<string, int> locations,
        IReadOnlyDictionary<string, Equipment> equipment,
        DateTime now)
    {
        if (!users.TryGetValue("admin", out var adminId)) return;

        var sessions = new[]
        {
            new InventorySeed("INV-SEED-OPEN-2026", "Kiểm kê thiết bị tháng 8 - đang thực hiện", InventoryStatuses.Open, -1, null, "LAB-ROOT"),
            new InventorySeed("INV-SEED-DONE-2026-01", "Kiểm kê thiết bị quý 1", InventoryStatuses.Completed, -90, -88, "LAB-IOT-A"),
            new InventorySeed("INV-SEED-DONE-2026-02", "Kiểm kê khu đo lường", InventoryStatuses.Completed, -45, -43, "LAB-MEAS")
        };

        var sampleItems = equipment.Values.Where(item => item.AssetCode.StartsWith("TS-DEMO-", StringComparison.Ordinal)).Take(12).ToList();
        foreach (var seed in sessions)
        {
            var session = await context.InventorySessions.FirstOrDefaultAsync(item => item.Code == seed.Code);
            if (session is null)
            {
                session = new InventorySession
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    Status = seed.Status,
                    StartedAt = now.AddDays(seed.StartedDaysAgo),
                    CompletedAt = seed.CompletedDaysAgo.HasValue ? now.AddDays(seed.CompletedDaysAgo.Value) : null,
                    CreatedByUserId = adminId,
                    LocationNodeId = locations.GetValueOrDefault(seed.LocationCode),
                    AssetCategoryId = categories.GetValueOrDefault("IoT")
                };
                context.InventorySessions.Add(session);
                await context.SaveChangesAsync();
            }

            for (var index = 0; index < sampleItems.Count; index++)
            {
                var item = sampleItems[index];
                if (await context.InventoryItems.AnyAsync(existing => existing.InventorySessionId == session.Id && existing.EquipmentId == item.Id)) continue;

                var status = seed.Status == InventoryStatuses.Open
                    ? index % 4 == 0 ? InventoryItemStatuses.Found : InventoryItemStatuses.Pending
                    : (index % 5) switch
                    {
                        0 => InventoryItemStatuses.WrongLocation,
                        1 => InventoryItemStatuses.Damaged,
                        2 => InventoryItemStatuses.Missing,
                        _ => InventoryItemStatuses.Found
                    };

                context.InventoryItems.Add(new InventoryItem
                {
                    InventorySessionId = session.Id,
                    EquipmentId = item.Id,
                    ExpectedLocationNodeId = item.LocationNodeId,
                    ExpectedLocationName = item.Location,
                    Status = status,
                    ScannedAt = status == InventoryItemStatuses.Pending ? null : now.AddDays(-1),
                    ScannedByUserId = status == InventoryItemStatuses.Pending ? null : adminId,
                    Note = status == InventoryItemStatuses.Found ? "Đã đối chiếu đúng thông tin." : $"{Prefix} Cần kiểm tra lại khi nghiệm thu."
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureHandoversAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, Equipment> equipment,
        IReadOnlyCollection<BorrowRecord> borrowRecords,
        DateTime now)
    {
        if (!users.TryGetValue("truonglab", out var managerId)) return;

        var activeRecords = borrowRecords
            .Where(record => record.Status == BorrowStatuses.Borrowed && record.EquipmentId.HasValue)
            .Take(2)
            .ToList();
        var index = 1;
        foreach (var record in activeRecords)
        {
            if (await context.HandoverRecords.AnyAsync(item => item.BorrowRecordId == record.Id)) continue;
            if (!record.EquipmentId.HasValue || !equipment.Values.Any(item => item.Id == record.EquipmentId.Value)) continue;

            var handover = new HandoverRecord
            {
                Code = $"BG-SEED-{index:000}",
                BorrowRecordId = record.Id,
                HandedOverByUserId = managerId,
                ReceivedByUserId = record.UserId,
                HandoverAt = now.AddDays(-4),
                ConfirmedAt = index == 1 ? now.AddDays(-4) : null,
                Notes = $"{Prefix} Biên bản bàn giao dùng để kiểm thử.",
                Items =
                [
                    new HandoverItem
                    {
                        EquipmentId = record.EquipmentId.Value,
                        Condition = EquipmentStatuses.Available,
                        Accessories = "Nguồn, cáp kết nối, hộp thiết bị",
                        Note = "Ngoại quan tốt, đã kiểm tra hoạt động."
                    }
                ]
            };
            context.HandoverRecords.Add(handover);
            index++;
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsurePenaltiesAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, Equipment> equipment,
        IReadOnlyCollection<BorrowRecord> borrowRecords,
        DateTime now)
    {
        var damaged = borrowRecords.FirstOrDefault(record => record.Status == BorrowStatuses.ReturnedDamaged);
        if (damaged?.EquipmentId is not int equipmentId || !users.TryGetValue("sv9", out var userId)) return;
        if (await context.Penalties.AnyAsync(item => item.BorrowRecordId == damaged.Id)) return;

        context.Penalties.Add(new Penalty
        {
            UserId = userId,
            EquipmentId = equipmentId,
            BorrowRecordId = damaged.Id,
            Reason = $"{Prefix} Hư hỏng vỏ thiết bị sau khi trả.",
            Amount = 1800000,
            Status = PenaltyStatuses.Unpaid,
            CreatedAt = now.AddDays(-20)
        });

        var returned = borrowRecords.FirstOrDefault(record => record.Status == BorrowStatuses.Returned && record.EquipmentId.HasValue);
        if (returned?.EquipmentId is int returnedEquipmentId && users.TryGetValue("sv8", out var paidUserId))
        {
            context.Penalties.Add(new Penalty
            {
                UserId = paidUserId,
                EquipmentId = returnedEquipmentId,
                BorrowRecordId = returned.Id,
                Reason = $"{Prefix} Trả thiết bị quá hạn.",
                Amount = 150000,
                Status = PenaltyStatuses.Paid,
                CreatedAt = now.AddDays(-25),
                PaidAt = now.AddDays(-23)
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureConsumableTransactionsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, int> consumables,
        DateTime now)
    {
        if (await context.ConsumableTransactions.AnyAsync(item => item.Reason.StartsWith(Prefix))) return;
        var userId = users.GetValueOrDefault("sv4");
        var transactionSeeds = new[]
        {
            new TransactionSeed("VT-SEED-001", "IN", 60, "Nhập lô cảm biến mẫu", -20),
            new TransactionSeed("VT-SEED-002", "OUT", 8, "Cấp phát module relay cho nhóm thực hành", -14),
            new TransactionSeed("VT-SEED-003", "IN", 40, "Nhập lô RFID mẫu", -10),
            new TransactionSeed("VT-SEED-005", "OUT", 12, "Cấp phát dây jumper cho nhóm sinh viên", -5)
        };

        foreach (var seed in transactionSeeds)
        {
            if (!consumables.TryGetValue(seed.Code, out var consumableId)) continue;
            var item = await context.Consumables.FirstAsync(consumable => consumable.Id == consumableId);
            var before = seed.Type == "IN" ? item.Quantity - seed.Quantity : item.Quantity + seed.Quantity;
            if (before < 0) before = 0;
            context.ConsumableTransactions.Add(new ConsumableTransaction
            {
                ConsumableId = consumableId,
                Type = seed.Type,
                Quantity = seed.Quantity,
                BeforeQuantity = before,
                AfterQuantity = item.Quantity,
                Reason = $"{Prefix} {seed.Reason}",
                UserId = userId == 0 ? null : userId,
                CreatedAt = now.AddDays(seed.DaysAgo)
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureBorrowStatusHistoryAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyCollection<BorrowRecord> records,
        DateTime now)
    {
        if (!users.TryGetValue("admin", out var adminId)) return;
        foreach (var record in records)
        {
            if (await context.BorrowStatusHistories.AnyAsync(item => item.BorrowRecordId == record.Id)) continue;
            context.BorrowStatusHistories.AddRange(
                new BorrowStatusHistory
                {
                    BorrowRecordId = record.Id,
                    FromStatus = null,
                    ToStatus = BorrowStatuses.Pending,
                    Note = $"{Prefix} Tạo phiếu mẫu.",
                    ChangedByUserId = record.UserId,
                    CreatedAt = now.AddDays(-15)
                },
                new BorrowStatusHistory
                {
                    BorrowRecordId = record.Id,
                    FromStatus = BorrowStatuses.Pending,
                    ToStatus = record.Status,
                    Note = $"{Prefix} Chuyển trạng thái mẫu.",
                    ChangedByUserId = adminId,
                    CreatedAt = now.AddDays(-14)
                });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureLocationHistoryAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        IReadOnlyDictionary<string, int> locations,
        IReadOnlyDictionary<string, Equipment> equipment,
        DateTime now)
    {
        if (!users.TryGetValue("admin", out var adminId)) return;
        var items = equipment.Values.Where(item => item.AssetCode.StartsWith("TS-DEMO-", StringComparison.Ordinal)).Take(8).ToList();
        foreach (var item in items)
        {
            if (await context.EquipmentLocationHistories.AnyAsync(history => history.EquipmentId == item.Id)) continue;
            var from = locations.GetValueOrDefault("LAB-ROOT");
            var to = item.LocationNodeId;
            context.EquipmentLocationHistories.Add(new EquipmentLocationHistory
            {
                EquipmentId = item.Id,
                FromLocationNodeId = from == 0 ? null : from,
                ToLocationNodeId = to,
                FromLocationName = "Kho tiếp nhận",
                ToLocationName = item.Location,
                Reason = $"{Prefix} Phân bổ thiết bị về vị trí sử dụng.",
                ChangedByUserId = adminId,
                ChangedAt = now.AddDays(-19)
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureNotificationsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        DateTime now)
    {
        var notifications = new[]
        {
            new NotificationSeed("admin", "WARNING", "Có phiếu mượn quá hạn", "Có 2 phiếu mượn cần kiểm tra.", "/dashboard/borrow-history"),
            new NotificationSeed("admin", "INFO", "Đã tạo đợt kiểm kê", "Đợt kiểm kê mẫu đang chờ hoàn tất.", "/dashboard/inventory"),
            new NotificationSeed("truonglab", "WARNING", "Vật tư dưới mức tối thiểu", "Có 4 loại vật tư cần bổ sung.", "/dashboard/devices"),
            new NotificationSeed("pholab", "INFO", "Có lịch bảo trì sắp đến hạn", "Vui lòng kiểm tra lịch bảo trì trong tuần này.", "/dashboard/maintenance-schedules"),
            new NotificationSeed("sv2", "SUCCESS", "Phiếu mượn đang chờ duyệt", "Phiếu mượn thiết bị ESP32 đã được tiếp nhận.", "/dashboard/borrow-history"),
            new NotificationSeed("sv3", "INFO", "Yêu cầu vật tư đã được gửi", "Yêu cầu module relay đang được xử lý.", "/dashboard/consumable-requests")
        };

        foreach (var seed in notifications)
        {
            if (!users.TryGetValue(seed.Username, out var userId)) continue;
            if (await context.Notifications.AnyAsync(item => item.UserId == userId && item.Url == seed.Url && item.Title == seed.Title)) continue;
            context.Notifications.Add(new AppNotification
            {
                UserId = userId,
                Type = seed.Type,
                Title = seed.Title,
                Message = $"{Prefix} {seed.Message}",
                Url = seed.Url,
                IsRead = seed.Username == "sv3",
                CreatedAt = now.AddHours(-2)
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureAuditLogsAsync(
        AppDbContext context,
        IReadOnlyDictionary<string, int> users,
        DateTime now)
    {
        if (await context.AuditLogs.AnyAsync(log => log.EntityId == "SEED-FULL-LOG-001")) return;
        var actions = new[]
        {
            ("admin", "Create", "Equipment", "Tạo thiết bị mẫu ESP32"),
            ("admin", "Update", "Equipment", "Cập nhật vị trí thiết bị mẫu"),
            ("truonglab", "Approve", "BorrowRecord", "Duyệt phiếu mượn mẫu"),
            ("pholab", "Maintenance", "MaintenanceRecord", "Tạo phiếu bảo trì mẫu"),
            ("admin", "Create", "InventorySession", "Tạo đợt kiểm kê mẫu"),
            ("admin", "InventoryScan", "InventoryItem", "Ghi nhận kết quả kiểm kê"),
            ("truonglab", "Issue", "ConsumableRequest", "Cấp phát vật tư mẫu"),
            ("giangvien2", "Create", "BorrowRecord", "Tạo phiếu mượn cho nhóm thực hành"),
            ("sv2", "Create", "ConsumableRequest", "Gửi yêu cầu cấp phát vật tư"),
            ("admin", "Update", "User", "Cập nhật thông tin tài khoản mẫu"),
            ("pholab", "Return", "BorrowRecord", "Kiểm tra trả thiết bị mẫu"),
            ("admin", "Create", "MaintenanceSchedule", "Tạo kế hoạch bảo trì mẫu")
        };

        var index = 1;
        foreach (var action in actions)
        {
            context.AuditLogs.Add(new AuditLog
            {
                UserId = users.GetValueOrDefault(action.Item1) == 0 ? null : users[action.Item1],
                Username = action.Item1,
                Action = action.Item2,
                EntityType = action.Item3,
                EntityId = $"SEED-FULL-LOG-{index:000}",
                Details = $"{{\"message\":\"{Prefix} {action.Item4}\"}}",
                IpAddress = "127.0.0.1",
                CreatedAt = now.AddMinutes(-index * 7)
            });
            index++;
        }

        await context.SaveChangesAsync();
    }

    private sealed record UserSeed(string Username, string Email, string FullName, string UniversityCode, string Department, string? ClassName, string Role);
    private sealed record CategorySeed(string Name, string Description);
    private sealed record LocationSeed(string Code, string Name, string Type, string? ParentCode, string Description);
    private sealed record EquipmentSeed(string AssetCode, string Name, string Model, string Serial, string LocationName, string LocationCode, string Category, string Status, decimal PurchaseValue, int BorrowCount, int AgeMonths = 6, int WarrantyMonths = 18);
    private sealed record ConsumableSeed(string Code, string Name, string Unit, int Quantity, int MinQuantity, string Category, string StorageLocation);
    private sealed record BorrowSeed(string Username, string AssetCode, string? TeacherUsername, string Status, int BorrowOffset, int ExpectedReturnOffset, string Purpose, bool IsReturned = false);
    private sealed record ConsumableRequestSeed(string Username, string Code, int Quantity, string Status, string Reason);
    private sealed record MaintenanceSeed(string AssetCode, string Status, int DaysAgo, decimal Cost, string Description, string PerformedBy, string Supplier);
    private sealed record ScheduleSeed(string AssetCode, string Name, int IntervalDays, int NextDueOffset);
    private sealed record InventorySeed(string Code, string Name, string Status, int StartedDaysAgo, int? CompletedDaysAgo, string LocationCode);
    private sealed record TransactionSeed(string Code, string Type, int Quantity, string Reason, int DaysAgo);
    private sealed record NotificationSeed(string Username, string Type, string Title, string Message, string Url);
}
