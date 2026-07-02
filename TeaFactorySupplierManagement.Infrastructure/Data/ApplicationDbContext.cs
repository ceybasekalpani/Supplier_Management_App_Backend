using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing tables - use existing database tables
        public DbSet<RegSupplier> RegSuppliers { get; set; }
        public DbSet<TrLeafCollectionTemp> TrLeafCollectionsTemp { get; set; }

        // New tables
        public DbSet<SupplierLogin> SupplierLogins { get; set; }
        public DbSet<FactorySetup> FactorySetups { get; set; }
        public DbSet<CashRequest> CashRequests { get; set; }
        public DbSet<FertilizerRequest> FertilizerRequests { get; set; }
        public DbSet<ItemRequest> ItemRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<NotificationRead> NotificationRead { get; set; }
        public DbSet<NotificationDismissed> NotificationDismissed { get; set; }
        public DbSet<NewsDismissed> NewsDismissed { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<FertilizerItemStore> FertilizerItemStore { get; set; }
        public DbSet<SupplierDashboardAdminLogin> SupplierDashboardAdminLogins { get; set; }
        public DbSet<SupplierDashboardAdminPermission> SupplierDashboardAdminPermissions { get; set; }
        public DbSet<DisbursementRecord> DisbursementRecords { get; set; }
        public DbSet<MobileDeviceToken> MobileDeviceTokens { get; set; }
        public DbSet<CodeGeneration> CodeGenerations { get; set; }
        public DbSet<DeliveryNoteHeader> DeliveryNoteHeaders { get; set; }
        public DbSet<DeliveryNoteDetail> DeliveryNoteDetails { get; set; }

        // View - read-only, joined from Reg_Supplier + Supplier_Management_Login
        public DbSet<SupplierLoginView> SupplierLoginView { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RegSupplier - use existing table
            modelBuilder.Entity<RegSupplier>(entity =>
            {
                entity.HasKey(e => e.RegNo);
                entity.ToTable("Reg_Supplier");
                entity.Property(e => e.RegNo).HasColumnName("RegNo");
            });

            // Configure TrLeafCollectionTemp - use existing table
            modelBuilder.Entity<TrLeafCollectionTemp>(entity =>
            {
                entity.ToTable("Tr_LeafCollection_Temp");
                entity.HasKey(e => e.Ind);
            });

            // Configure NewsDismissed composite key
            modelBuilder.Entity<NewsDismissed>(entity =>
            {
                // Explicitly configure composite primary key
                entity.HasKey(nd => new { nd.NewsId, nd.RegNo });

                // Configure table name
                entity.ToTable("Supplier_Management_News_Dismissed");

                // Configure relationships
                entity.HasOne(nd => nd.News)
                    .WithMany(n => n.DismissedBy)
                    .HasForeignKey(nd => nd.NewsId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(nd => nd.Supplier)
                    .WithMany(s => s.NewsDismissed)
                    .HasForeignKey(nd => nd.RegNo)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure index
                entity.HasIndex(nd => nd.RegNo);
            });

            // Configure NotificationDismissed composite key
            modelBuilder.Entity<NotificationDismissed>(entity =>
            {
                entity.HasKey(nd => new { nd.NotificationId, nd.RegNo });

                entity.ToTable("Supplier_Management_Notification_Dismissed");

                entity.HasOne(nd => nd.Notification)
                    .WithMany(n => n.DismissedBy)
                    .HasForeignKey(nd => nd.NotificationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(nd => nd.Supplier)
                    .WithMany(s => s.NotificationDismissed)
                    .HasForeignKey(nd => nd.RegNo)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(nd => nd.RegNo);
            });

            // *** FIX: Configure NotificationRead composite key ***
            modelBuilder.Entity<NotificationRead>(entity =>
            {
                // Explicitly configure composite primary key
                entity.HasKey(nr => new { nr.NotificationId, nr.RegNo });

                // Configure table name
                entity.ToTable("Supplier_Management_Notification_Read");

                // Configure relationships
                entity.HasOne(nr => nr.Notification)
                    .WithMany(n => n.ReadBy)
                    .HasForeignKey(nr => nr.NotificationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(nr => nr.Supplier)
                    .WithMany(s => s.NotificationReads)
                    .HasForeignKey(nr => nr.RegNo)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure indexes
                entity.HasIndex(nr => nr.RegNo);
                entity.HasIndex(nr => nr.NotificationId);
            });

            // Configure indexes for junction tables
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.IsActive, n.StartDate, n.EndDate, n.TargetRegNo, n.TargetRoute });

            modelBuilder.Entity<News>()
                .HasIndex(n => new { n.IsActive, n.ShowPopup, n.StartDate, n.EndDate, n.TargetRegNo, n.TargetRoute });

            // Configure relationships
            modelBuilder.Entity<SupplierLogin>()
                .HasOne(l => l.Supplier)
                .WithMany(s => s.Logins)
                .HasForeignKey(l => l.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSettings>()
                .HasOne(u => u.Supplier)
                .WithMany(s => s.Settings)
                .HasForeignKey(u => u.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CashRequest>()
                .HasOne(c => c.Supplier)
                .WithMany(s => s.CashRequests)
                .HasForeignKey(c => c.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FertilizerRequest>()
                .HasOne(f => f.Supplier)
                .WithMany(s => s.FertilizerRequests)
                .HasForeignKey(f => f.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemRequest>()
                .HasOne(i => i.Supplier)
                .WithMany(s => s.ItemRequests)
                .HasForeignKey(i => i.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrLeafCollectionTemp>()
                .HasOne(l => l.Supplier)
                .WithMany(s => s.LeafCollections)
                .HasForeignKey(l => l.RegNo)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure unique constraints
            modelBuilder.Entity<SupplierLogin>()
                .HasIndex(l => l.Username)
                .IsUnique();

            modelBuilder.Entity<CashRequest>()
                .HasIndex(c => c.RequestNo)
                .IsUnique();

            modelBuilder.Entity<FertilizerRequest>()
                .HasIndex(f => f.RequestNo)
                .IsUnique();

            modelBuilder.Entity<ItemRequest>()
                .HasIndex(i => i.RequestNo)
                .IsUnique();

            modelBuilder.Entity<UserSettings>()
                .HasIndex(u => u.RegNo)
                .IsUnique();

            modelBuilder.Entity<FactorySetup>()
                .HasIndex(f => f.FactoryName)
                .IsUnique();

            modelBuilder.Entity<SupplierDashboardAdminLogin>(entity =>
            {
                entity.ToTable("supplier_dashboard_AdminLogin");
                entity.HasIndex(a => a.Username).IsUnique();
                entity.HasIndex(a => a.Email);
                entity.Property(a => a.IsActive).HasDefaultValue(true);
                entity.Property(a => a.Role).HasDefaultValue("Admin");
                entity.HasMany(a => a.Permissions)
                    .WithOne(p => p.Admin)
                    .HasForeignKey(p => p.AdminId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SupplierDashboardAdminPermission>(entity =>
            {
                entity.ToTable("supplier_dashboard_AdminPermissions");
                entity.HasIndex(p => new { p.AdminId, p.ModuleId }).IsUnique();
                entity.Property(p => p.IsEnabled).HasDefaultValue(false);
            });

            modelBuilder.Entity<DisbursementRecord>(entity =>
            {
                entity.ToTable("Supplier_Management_Disbursement_Records");
                entity.HasIndex(d => new { d.IssuedType, d.RequestId }).IsUnique();
                entity.HasIndex(d => new { d.CurrentStatus, d.IssueDate });
                entity.HasIndex(d => d.RegNo);
                entity.HasIndex(d => d.DeliveryNoteId);
                entity.HasIndex(d => d.DeliveryNoteNo);
                entity.HasOne(d => d.DeliveryNote)
                    .WithMany(h => h.DisbursementRecords)
                    .HasForeignKey(d => d.DeliveryNoteNo)
                    .HasPrincipalKey(h => h.DN_No)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CodeGeneration>(entity =>
            {
                entity.ToTable("Supplier_Management_Code_Generation");
                entity.HasIndex(x => new { x.CodePrefix, x.Year }).IsUnique();
            });

            modelBuilder.Entity<DeliveryNoteHeader>(entity =>
            {
                entity.ToTable("Supplier_Management_DeliveryNote_Header");
                entity.HasAlternateKey(x => x.DN_No);
                entity.HasIndex(x => new { x.Status, x.DispatchDate });
            });

            modelBuilder.Entity<DeliveryNoteDetail>(entity =>
            {
                entity.ToTable("Supplier_Management_DeliveryNote_Detail");
                entity.HasIndex(x => x.DeliveryNoteId);
                entity.HasIndex(x => x.DeliveryNoteNo);
                entity.HasIndex(x => x.SupplierRegNo);
                entity.HasIndex(x => x.DisbursementRecordId).IsUnique();
                entity.HasOne(x => x.DeliveryNote)
                    .WithMany(x => x.Details)
                    .HasForeignKey(x => x.DeliveryNoteNo)
                    .HasPrincipalKey(x => x.DN_No)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Supplier)
                    .WithMany()
                    .HasForeignKey(x => x.SupplierRegNo)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.DisbursementRecord)
                    .WithMany()
                    .HasForeignKey(x => x.DisbursementRecordId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MobileDeviceToken>(entity =>
            {
                entity.ToTable("Supplier_Management_MobileDeviceTokens");
                entity.HasIndex(x => x.FcmToken).IsUnique();
                entity.HasIndex(x => new { x.RegNo, x.IsActive });
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.LastSeenAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(x => x.Supplier)
                    .WithMany()
                    .HasForeignKey(x => x.RegNo)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<News>()
                .HasOne(n => n.TargetSupplier)
                .WithMany()
                .HasForeignKey(n => n.TargetRegNo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.TargetSupplier)
                .WithMany()
                .HasForeignKey(n => n.TargetRegNo)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure default values
            modelBuilder.Entity<CashRequest>()
                .Property(c => c.Status)
                .HasDefaultValue("pending");

            modelBuilder.Entity<FertilizerRequest>()
                .Property(f => f.Status)
                .HasDefaultValue("pending");

            modelBuilder.Entity<ItemRequest>()
                .Property(i => i.Status)
                .HasDefaultValue("pending");

            // Map SupplierLoginView to the database view (keyless - read only)
            modelBuilder.Entity<SupplierLoginView>()
                .HasNoKey()
                .ToView("vw_SupplierLoginDetails");
        }
    }
}
