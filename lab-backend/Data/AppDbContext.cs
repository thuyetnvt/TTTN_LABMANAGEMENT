using Microsoft.EntityFrameworkCore;
using LabManagementAPI.Models;

namespace LabManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<BorrowRecord> BorrowRecords { get; set; }
        public DbSet<BorrowRequestDetail> BorrowRequestDetails { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<ConsumableRequest> ConsumableRequests { get; set; }
        public DbSet<ConsumableTransaction> ConsumableTransactions { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LocationNode> LocationNodes { get; set; }
        public DbSet<BorrowStatusHistory> BorrowStatusHistories { get; set; }
        public DbSet<InventorySession> InventorySessions { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AssetCategory>(entity =>
            {
                entity.Property(category => category.Name).HasMaxLength(150);
                entity.Property(category => category.Description).HasMaxLength(1000);
                entity.HasIndex(category => category.Name).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Username).HasMaxLength(100);
                entity.Property(u => u.Email).HasMaxLength(256);
                entity.Property(u => u.Role).HasMaxLength(50);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email);
                entity.HasIndex(u => new { u.Role, u.IsActive });
            });

            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.Property(e => e.AssetCode).HasMaxLength(100);
                entity.Property(e => e.QrToken).HasMaxLength(64);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Model).HasMaxLength(255);
                entity.Property(e => e.Serial).HasMaxLength(100);
                entity.Property(e => e.SerialName).HasMaxLength(255);
                entity.Property(e => e.DeviceType).HasMaxLength(150);
                entity.Property(e => e.MacAddress).HasMaxLength(50);
                entity.Property(e => e.Imei).HasMaxLength(50);
                entity.Property(e => e.FirmwareVersion).HasMaxLength(100);
                entity.Property(e => e.Manufacturer).HasMaxLength(150);
                entity.Property(e => e.Supplier).HasMaxLength(255);
                entity.Property(e => e.FundingSource).HasMaxLength(255);
                entity.Property(e => e.PurchaseValue).HasPrecision(18, 2);
                entity.Property(e => e.ImagePath).HasMaxLength(1000);
                entity.Property(e => e.Notes).HasMaxLength(2000);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.ResponsiblePerson).HasMaxLength(255);
                entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
                entity.Property(e => e.DecisionFileName).HasMaxLength(255);
                entity.Property(e => e.DecisionFilePath).HasMaxLength(1000);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.HasIndex(e => e.Serial).IsUnique();
                entity.HasIndex(e => e.AssetCode).IsUnique();
                entity.HasIndex(e => e.QrToken).IsUnique();
                entity.HasIndex(e => e.LocationNodeId);
                entity.HasIndex(e => e.Status);
                entity.HasOne(e => e.LocationNode)
                    .WithMany()
                    .HasForeignKey(e => e.LocationNodeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<LocationNode>(entity =>
            {
                entity.Property(location => location.Code).HasMaxLength(100);
                entity.Property(location => location.Name).HasMaxLength(255);
                entity.Property(location => location.Type).HasMaxLength(50);
                entity.Property(location => location.Description).HasMaxLength(1000);
                entity.HasIndex(location => location.Code).IsUnique();
                entity.HasOne(location => location.Parent)
                    .WithMany(location => location!.Children)
                    .HasForeignKey(location => location.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Consumable>(entity =>
            {
                entity.Property(item => item.Name).HasMaxLength(255);
                entity.Property(item => item.Unit).HasMaxLength(50);
                entity.Property(item => item.ResponsiblePerson).HasMaxLength(255);
                entity.Property(item => item.InvoiceNumber).HasMaxLength(100);
                entity.HasIndex(item => item.AssetCategoryId);
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Consumables_Quantity", "Quantity >= 0");
                    table.HasCheckConstraint("CK_Consumables_MinQuantity", "MinQuantity >= 0");
                });
            });

            modelBuilder.Entity<BorrowRecord>(entity =>
            {
                entity.Property(record => record.Purpose).HasMaxLength(1000);
                entity.Property(record => record.Status).HasMaxLength(50);
                entity.Property(record => record.TeacherDecisionNote).HasMaxLength(2000);
                entity.Property(record => record.ManagerDecisionNote).HasMaxLength(2000);
                entity.Property(record => record.ReturnCondition).HasMaxLength(50);
                entity.Property(record => record.ReturnInspectionNote).HasMaxLength(2000);
                entity.Property(record => record.WarrantyAction).HasMaxLength(255);
                entity.Property(record => record.CompensationAmount).HasPrecision(18, 2);
                entity.HasIndex(record => record.Status);
                entity.HasIndex(record => record.ExpectedReturnDate);
                entity.HasOne(record => record.InspectedByUser)
                    .WithMany()
                    .HasForeignKey(record => record.InspectedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(record => record.Teacher)
                    .WithMany()
                    .HasForeignKey(record => record.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(record => record.User)
                    .WithMany()
                    .HasForeignKey(record => record.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(record => record.Equipment)
                    .WithMany()
                    .HasForeignKey(record => record.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BorrowRequestDetail>(entity =>
            {
                entity.Property(detail => detail.Note).HasMaxLength(1000);
                entity.Property(detail => detail.Status).HasMaxLength(50);
                entity.Property(detail => detail.ReturnCondition).HasMaxLength(50);
                entity.Property(detail => detail.ReturnNote).HasMaxLength(2000);
                entity.Property(detail => detail.CompensationAmount).HasPrecision(18, 2);
                entity.HasIndex(detail => new { detail.BorrowRecordId, detail.EquipmentId }).IsUnique();
                entity.HasOne(detail => detail.Equipment)
                    .WithMany()
                    .HasForeignKey(detail => detail.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BorrowStatusHistory>(entity =>
            {
                entity.Property(history => history.FromStatus).HasMaxLength(50);
                entity.Property(history => history.ToStatus).HasMaxLength(50);
                entity.Property(history => history.Note).HasMaxLength(2000);
                entity.HasIndex(history => new { history.BorrowRecordId, history.CreatedAt });
                entity.HasOne(history => history.BorrowRecord)
                    .WithMany(record => record.StatusHistory)
                    .HasForeignKey(history => history.BorrowRecordId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(history => history.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(history => history.ChangedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InventorySession>(entity =>
            {
                entity.Property(session => session.Code).HasMaxLength(50);
                entity.Property(session => session.Name).HasMaxLength(255);
                entity.Property(session => session.Status).HasMaxLength(50);
                entity.HasIndex(session => session.Code).IsUnique();
                entity.HasIndex(session => session.Status);
                entity.HasOne(session => session.LocationNode)
                    .WithMany()
                    .HasForeignKey(session => session.LocationNodeId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(session => session.AssetCategory)
                    .WithMany()
                    .HasForeignKey(session => session.AssetCategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(session => session.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(session => session.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.Property(item => item.ExpectedLocationName).HasMaxLength(255);
                entity.Property(item => item.Status).HasMaxLength(50);
                entity.Property(item => item.Note).HasMaxLength(2000);
                entity.HasIndex(item => new { item.InventorySessionId, item.EquipmentId }).IsUnique();
                entity.HasIndex(item => new { item.InventorySessionId, item.Status });
                entity.HasOne(item => item.InventorySession)
                    .WithMany(session => session.Items)
                    .HasForeignKey(item => item.InventorySessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Equipment)
                    .WithMany()
                    .HasForeignKey(item => item.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.ScannedByUser)
                    .WithMany()
                    .HasForeignKey(item => item.ScannedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ConsumableRequest>(entity =>
            {
                entity.Property(request => request.Reason).HasMaxLength(1000);
                entity.Property(request => request.Status).HasMaxLength(50);
                entity.HasIndex(request => request.Status);
                entity.HasOne(request => request.User)
                    .WithMany()
                    .HasForeignKey(request => request.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(request => request.Consumable)
                    .WithMany()
                    .HasForeignKey(request => request.ConsumableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConsumableTransaction>(entity =>
            {
                entity.Property(transaction => transaction.Type).HasMaxLength(50);
                entity.Property(transaction => transaction.Reason).HasMaxLength(1000);
                entity.HasIndex(transaction => transaction.ConsumableId);
                entity.HasIndex(transaction => transaction.CreatedAt);
                entity.HasOne(transaction => transaction.Consumable)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.ConsumableId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(transaction => transaction.User)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.Property(record => record.Description).HasMaxLength(2000);
                entity.Property(record => record.PerformedBy).HasMaxLength(255);
                entity.Property(record => record.Status).HasMaxLength(50);
                entity.Property(record => record.Result).HasMaxLength(2000);
                entity.Property(record => record.Cost).HasPrecision(18, 2);
                entity.HasIndex(record => record.Status);
                entity.HasOne(record => record.Equipment)
                    .WithMany()
                    .HasForeignKey(record => record.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Penalty>(entity =>
            {
                entity.Property(penalty => penalty.Reason).HasMaxLength(2000);
                entity.Property(penalty => penalty.Status).HasMaxLength(50);
                entity.Property(penalty => penalty.Amount).HasPrecision(18, 2);
                entity.HasIndex(penalty => penalty.Status);
                entity.HasOne(penalty => penalty.User)
                    .WithMany()
                    .HasForeignKey(penalty => penalty.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(penalty => penalty.Equipment)
                    .WithMany()
                    .HasForeignKey(penalty => penalty.EquipmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(penalty => penalty.BorrowRecord)
                    .WithMany()
                    .HasForeignKey(penalty => penalty.BorrowRecordId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(token => token.TokenHash).HasMaxLength(64);
                entity.HasIndex(token => token.TokenHash).IsUnique();
                entity.HasIndex(token => token.ExpiresAt);
                entity.HasOne(token => token.User)
                    .WithMany()
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.Property(log => log.Username).HasMaxLength(100);
                entity.Property(log => log.Action).HasMaxLength(100);
                entity.Property(log => log.EntityType).HasMaxLength(100);
                entity.Property(log => log.EntityId).HasMaxLength(100);
                entity.Property(log => log.IpAddress).HasMaxLength(64);
                entity.HasIndex(log => log.CreatedAt);
                entity.HasIndex(log => log.UserId);
            });
        }
    }
}
