using LabManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Data;

public static class DbInitializer
{
    public static async Task SeedDevelopmentDataAsync(
        AppDbContext context,
        IConfiguration configuration)
    {
        var defaultPassword = configuration["Seed:DefaultPassword"];
        if (string.IsNullOrWhiteSpace(defaultPassword) || defaultPassword.Length < 8)
        {
            throw new InvalidOperationException(
                "Seed:DefaultPassword phải có ít nhất 8 ký tự khi bật seed dữ liệu.");
        }

        var users = new[]
        {
            new User { Username = "admin", Email = "admin@lab.local", Role = Roles.Admin },
            new User { Username = "truonglab", Email = "truonglab@lab.local", Role = Roles.LabHead },
            new User { Username = "pholab", Email = "pholab@lab.local", Role = Roles.DeputyLabHead },
            new User { Username = "giangvien1", Email = "giangvien1@lab.local", Role = Roles.Teacher },
            new User { Username = "sv1", Email = "sv1@lab.local", Role = Roles.Student }
        };

        foreach (var user in users)
        {
            if (await context.Users.AnyAsync(existing => existing.Username == user.Username))
            {
                continue;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;
            context.Users.Add(user);
        }

        var categories = new[]
        {
            new AssetCategory { Name = "IoT", Description = "Thiết bị và module phục vụ IoT" },
            new AssetCategory { Name = "AI", Description = "Thiết bị phục vụ AI và xử lý dữ liệu" },
            new AssetCategory { Name = "Thiết bị đo", Description = "Máy đo và thiết bị hiệu chuẩn" },
            new AssetCategory { Name = "Linh kiện", Description = "Linh kiện, module và vật tư kỹ thuật" }
        };

        foreach (var category in categories)
        {
            if (!await context.AssetCategories.AnyAsync(existing => existing.Name == category.Name))
            {
                context.AssetCategories.Add(category);
            }
        }

        await context.SaveChangesAsync();

        await SeedSampleInventoryAsync(context);
        await SeedSampleOperationsAsync(context);
        await SeedSampleAuditLogsAsync(context);
    }

    private static async Task SeedSampleInventoryAsync(AppDbContext context)
    {
        var categoryIds = await context.AssetCategories
            .ToDictionaryAsync(category => category.Name, category => category.Id);

        var now = DateTime.UtcNow;
        var equipments = new[]
        {
            new Equipment
            {
                Name = "Bộ kit Arduino Uno R3",
                Model = "Arduino Uno R3",
                Serial = "LAB-IOT-ARD-001",
                SerialName = "ARD-UNO-01",
                Location = "Tủ IoT A1",
                ResponsiblePerson = "Nguyễn Văn Lab",
                EntryDate = now.AddMonths(-10),
                WarrantyExpiry = now.AddMonths(14),
                InvoiceNumber = "HD-IOT-0001",
                Status = EquipmentStatuses.Available,
                AssetCategoryId = categoryIds.GetValueOrDefault("IoT"),
                BorrowCount = 3,
                CreatedAt = now.AddDays(-40)
            },
            new Equipment
            {
                Name = "Máy hiện sóng Rigol DS1054Z",
                Model = "DS1054Z",
                Serial = "LAB-MEAS-OSC-001",
                SerialName = "OSC-RIGOL-01",
                Location = "Bàn đo lường B2",
                ResponsiblePerson = "Trần Thị Thiết Bị",
                EntryDate = now.AddYears(-1),
                WarrantyExpiry = now.AddMonths(8),
                InvoiceNumber = "HD-DO-0007",
                Status = EquipmentStatuses.Borrowed,
                AssetCategoryId = categoryIds.GetValueOrDefault("Thiết bị đo"),
                BorrowCount = 8,
                CreatedAt = now.AddDays(-38)
            },
            new Equipment
            {
                Name = "Laptop GPU RTX 4060",
                Model = "MSI Cyborg 15",
                Serial = "LAB-AI-LAP-001",
                SerialName = "AI-LAP-01",
                Location = "Phòng AI C3",
                ResponsiblePerson = "Phạm Minh AI",
                EntryDate = now.AddMonths(-7),
                WarrantyExpiry = now.AddMonths(17),
                InvoiceNumber = "HD-AI-0012",
                Status = EquipmentStatuses.Available,
                AssetCategoryId = categoryIds.GetValueOrDefault("AI"),
                BorrowCount = 5,
                CreatedAt = now.AddDays(-36)
            },
            new Equipment
            {
                Name = "Camera Intel RealSense D435",
                Model = "D435",
                Serial = "LAB-AI-CAM-001",
                SerialName = "REAL-D435-01",
                Location = "Kệ camera C1",
                ResponsiblePerson = "Phạm Minh AI",
                EntryDate = now.AddMonths(-15),
                WarrantyExpiry = now.AddMonths(3),
                InvoiceNumber = "HD-AI-0004",
                Status = EquipmentStatuses.Warranty,
                AssetCategoryId = categoryIds.GetValueOrDefault("AI"),
                BorrowCount = 4,
                CreatedAt = now.AddDays(-35)
            },
            new Equipment
            {
                Name = "Nguồn DC lập trình Korad",
                Model = "KA3005P",
                Serial = "LAB-MEAS-PSU-001",
                SerialName = "PSU-KORAD-01",
                Location = "Bàn điện tử B1",
                ResponsiblePerson = "Trần Thị Thiết Bị",
                EntryDate = now.AddYears(-2),
                WarrantyExpiry = now.AddMonths(-2),
                InvoiceNumber = "HD-DO-0002",
                Status = EquipmentStatuses.Broken,
                AssetCategoryId = categoryIds.GetValueOrDefault("Thiết bị đo"),
                BorrowCount = 6,
                CreatedAt = now.AddDays(-34)
            },
            new Equipment
            {
                Name = "Bộ cảm biến môi trường",
                Model = "DHT22/BH1750/BMP280",
                Serial = "LAB-IOT-SEN-001",
                SerialName = "SENSOR-KIT-01",
                Location = "Tủ linh kiện A2",
                ResponsiblePerson = "Nguyễn Văn Lab",
                EntryDate = now.AddMonths(-4),
                WarrantyExpiry = now.AddMonths(20),
                InvoiceNumber = "HD-LK-0018",
                Status = EquipmentStatuses.Available,
                AssetCategoryId = categoryIds.GetValueOrDefault("Linh kiện"),
                BorrowCount = 1,
                CreatedAt = now.AddDays(-33)
            }
        };

        foreach (var equipment in equipments)
        {
            if (!await context.Equipments.AnyAsync(existing => existing.Serial == equipment.Serial))
            {
                context.Equipments.Add(equipment);
            }
        }

        var consumables = new[]
        {
            new Consumable
            {
                Name = "Điện trở 220 Ohm",
                Unit = "cái",
                Quantity = 450,
                MinQuantity = 100,
                ResponsiblePerson = "Nguyễn Văn Lab",
                AssetCategoryId = categoryIds.GetValueOrDefault("Linh kiện"),
                EntryDate = now.AddMonths(-2),
                InvoiceNumber = "VT-0001",
                CreatedAt = now.AddDays(-30)
            },
            new Consumable
            {
                Name = "Dây jumper đực-cái",
                Unit = "sợi",
                Quantity = 320,
                MinQuantity = 80,
                ResponsiblePerson = "Nguyễn Văn Lab",
                AssetCategoryId = categoryIds.GetValueOrDefault("IoT"),
                EntryDate = now.AddMonths(-2),
                InvoiceNumber = "VT-0002",
                CreatedAt = now.AddDays(-29)
            },
            new Consumable
            {
                Name = "Board test breadboard",
                Unit = "cái",
                Quantity = 24,
                MinQuantity = 10,
                ResponsiblePerson = "Trần Thị Thiết Bị",
                AssetCategoryId = categoryIds.GetValueOrDefault("Linh kiện"),
                EntryDate = now.AddMonths(-3),
                InvoiceNumber = "VT-0003",
                CreatedAt = now.AddDays(-28)
            },
            new Consumable
            {
                Name = "Cảm biến siêu âm HC-SR04",
                Unit = "cái",
                Quantity = 8,
                MinQuantity = 10,
                ResponsiblePerson = "Nguyễn Văn Lab",
                AssetCategoryId = categoryIds.GetValueOrDefault("IoT"),
                EntryDate = now.AddMonths(-1),
                InvoiceNumber = "VT-0004",
                CreatedAt = now.AddDays(-27)
            }
        };

        foreach (var consumable in consumables)
        {
            if (!await context.Consumables.AnyAsync(existing => existing.Name == consumable.Name))
            {
                context.Consumables.Add(consumable);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleOperationsAsync(AppDbContext context)
    {
        var users = await context.Users.ToDictionaryAsync(user => user.Username, user => user.Id);
        var equipments = await context.Equipments.ToDictionaryAsync(equipment => equipment.Serial, equipment => equipment);
        var consumables = await context.Consumables.ToDictionaryAsync(consumable => consumable.Name, consumable => consumable.Id);
        var now = DateTime.UtcNow;

        if (users.TryGetValue("sv1", out var studentId)
            && users.TryGetValue("giangvien1", out var teacherId))
        {
            await AddBorrowRecordAsync(
                context,
                studentId,
                equipments["LAB-AI-LAP-001"].Id,
                teacherId,
                now.AddDays(-1),
                now.AddDays(6),
                "Làm đồ án nhận dạng thiết bị bằng camera.",
                BorrowStatuses.TeacherPending);

            await AddBorrowRecordAsync(
                context,
                teacherId,
                equipments["LAB-IOT-ARD-001"].Id,
                null,
                now.AddDays(-2),
                now.AddDays(5),
                "Demo thực hành IoT tuần 3.",
                BorrowStatuses.Pending);

            await AddBorrowRecordAsync(
                context,
                studentId,
                equipments["LAB-MEAS-OSC-001"].Id,
                teacherId,
                now.AddDays(-5),
                now.AddDays(2),
                "Đo tín hiệu PWM cho bài thực hành vi điều khiển.",
                BorrowStatuses.Borrowed);

            await AddBorrowRecordAsync(
                context,
                studentId,
                equipments["LAB-IOT-SEN-001"].Id,
                teacherId,
                now.AddDays(-18),
                now.AddDays(-10),
                "Thu thập dữ liệu môi trường cho mini project.",
                BorrowStatuses.Returned,
                now.AddDays(-9),
                EquipmentStatuses.Available,
                "Đủ phụ kiện, hoạt động bình thường.",
                "Không cần xử lý",
                false,
                users.GetValueOrDefault("pholab"));

            var brokenRecord = await AddBorrowRecordAsync(
                context,
                studentId,
                equipments["LAB-MEAS-PSU-001"].Id,
                teacherId,
                now.AddDays(-25),
                now.AddDays(-17),
                "Kiểm tra nguồn cấp cho mạch công suất.",
                BorrowStatuses.ReturnedDamaged,
                now.AddDays(-16),
                EquipmentStatuses.Broken,
                "Cổng output lỏng, thiết bị hết bảo hành.",
                "Hết bảo hành - kiểm tra bồi thường",
                false,
                users.GetValueOrDefault("truonglab"),
                350000);

            if (!await context.Penalties.AnyAsync(penalty => penalty.BorrowRecordId == brokenRecord.Id))
            {
                context.Penalties.Add(new Penalty
                {
                    UserId = studentId,
                    EquipmentId = equipments["LAB-MEAS-PSU-001"].Id,
                    BorrowRecordId = brokenRecord.Id,
                    Reason = "Cổng output lỏng sau khi trả thiết bị.",
                    Amount = 350000,
                    Status = PenaltyStatuses.Unpaid,
                    CreatedAt = now.AddDays(-16)
                });
            }
        }

        if (equipments.TryGetValue("LAB-AI-CAM-001", out var camera)
            && !await context.MaintenanceRecords.AnyAsync(record => record.EquipmentId == camera.Id))
        {
            context.MaintenanceRecords.Add(new MaintenanceRecord
            {
                EquipmentId = camera.Id,
                MaintenanceDate = now.AddDays(-3),
                Description = "Camera mất tín hiệu depth, gửi kiểm tra bảo hành.",
                Cost = 0,
                PerformedBy = "Bảo hành hãng",
                Status = MaintenanceStatuses.InProgress
            });
        }

        if (equipments.TryGetValue("LAB-IOT-SEN-001", out var sensorKit)
            && !await context.MaintenanceRecords.AnyAsync(record => record.EquipmentId == sensorKit.Id))
        {
            context.MaintenanceRecords.Add(new MaintenanceRecord
            {
                EquipmentId = sensorKit.Id,
                MaintenanceDate = now.AddDays(-8),
                Description = "Hiệu chuẩn cảm biến ánh sáng sau khi trả.",
                Cost = 120000,
                PerformedBy = "Kỹ thuật lab",
                Status = MaintenanceStatuses.Completed,
                CompletedAt = now.AddDays(-7),
                Result = "Cảm biến hoạt động ổn định."
            });
        }

        if (users.TryGetValue("sv1", out var requestUserId))
        {
            await AddConsumableRequestAsync(
                context,
                requestUserId,
                consumables["Điện trở 220 Ohm"],
                30,
                "Làm bài thực hành mạch LED.",
                ConsumableRequestStatuses.Pending,
                now.AddDays(-1));

            await AddConsumableRequestAsync(
                context,
                requestUserId,
                consumables["Dây jumper đực-cái"],
                20,
                "Chuẩn bị demo cảm biến IoT.",
                ConsumableRequestStatuses.Issued,
                now.AddDays(-7),
                now.AddDays(-6));

            await AddConsumableRequestAsync(
                context,
                requestUserId,
                consumables["Cảm biến siêu âm HC-SR04"],
                4,
                "Mượn vượt định mức cho nhóm ngoài lịch.",
                ConsumableRequestStatuses.Rejected,
                now.AddDays(-5),
                now.AddDays(-4));
        }

        await context.SaveChangesAsync();
    }

    private static async Task<BorrowRecord> AddBorrowRecordAsync(
        AppDbContext context,
        int userId,
        int equipmentId,
        int? teacherId,
        DateTime borrowDate,
        DateTime expectedReturnDate,
        string purpose,
        string status,
        DateTime? actualReturnDate = null,
        string returnCondition = "",
        string returnInspectionNote = "",
        string warrantyAction = "",
        bool? isUnderWarrantyAtReturn = null,
        int? inspectedByUserId = null,
        decimal compensationAmount = 0)
    {
        var existing = await context.BorrowRecords
            .Include(record => record.Details)
            .FirstOrDefaultAsync(record => record.EquipmentId == equipmentId && record.Purpose == purpose);
        if (existing is not null)
        {
            return existing;
        }

        var record = new BorrowRecord
        {
            UserId = userId,
            EquipmentId = equipmentId,
            TeacherId = teacherId,
            BorrowDate = borrowDate,
            ExpectedReturnDate = expectedReturnDate,
            ActualReturnDate = actualReturnDate,
            Purpose = purpose,
            Status = status,
            ReturnCondition = returnCondition,
            ReturnInspectionNote = returnInspectionNote,
            WarrantyAction = warrantyAction,
            IsUnderWarrantyAtReturn = isUnderWarrantyAtReturn,
            InspectedByUserId = inspectedByUserId,
            CompensationAmount = compensationAmount,
            Details =
            [
                new BorrowRequestDetail
                {
                    EquipmentId = equipmentId,
                    Quantity = 1,
                    Note = purpose
                }
            ]
        };

        context.BorrowRecords.Add(record);
        await context.SaveChangesAsync();
        return record;
    }

    private static async Task AddConsumableRequestAsync(
        AppDbContext context,
        int userId,
        int consumableId,
        int quantity,
        string reason,
        string status,
        DateTime requestDate,
        DateTime? approvalDate = null)
    {
        if (await context.ConsumableRequests.AnyAsync(request => request.ConsumableId == consumableId && request.Reason == reason))
        {
            return;
        }

        context.ConsumableRequests.Add(new ConsumableRequest
        {
            UserId = userId,
            ConsumableId = consumableId,
            Quantity = quantity,
            Reason = reason,
            Status = status,
            RequestDate = requestDate,
            ApprovalDate = approvalDate
        });
    }

    private static async Task SeedSampleAuditLogsAsync(AppDbContext context)
    {
        if (await context.AuditLogs.AnyAsync(log => log.Action == "SeedSampleData"))
        {
            return;
        }

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Username = "system",
                Action = "SeedSampleData",
                EntityType = "Database",
                EntityId = "sample",
                Details = """{"message":"Đã thêm dữ liệu mẫu tạm cho nghiệm thu local."}""",
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow
            },
            new AuditLog
            {
                Username = "admin",
                Action = "Create",
                EntityType = "Equipment",
                EntityId = "LAB-IOT-ARD-001",
                Details = """{"name":"Bộ kit Arduino Uno R3"}""",
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });

        await context.SaveChangesAsync();
    }
}
