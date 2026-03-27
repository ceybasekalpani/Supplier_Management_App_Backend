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

        // Existing tables
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
        public DbSet<NotificationRead> NotificationReads { get; set; }
        public DbSet<NewsDismissed> NewsDismissed { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys
            modelBuilder.Entity<NotificationRead>()
                .HasKey(nr => new { nr.NotificationId, nr.RegNo });

            modelBuilder.Entity<NewsDismissed>()
                .HasKey(nd => new { nd.NewsId, nd.RegNo });

            // Configure relationships
            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.Logins)
                .WithOne(l => l.Supplier)
                .HasForeignKey(l => l.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.Settings)
                .WithOne(s => s.Supplier)
                .HasForeignKey(s => s.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.CashRequests)
                .WithOne(c => c.Supplier)
                .HasForeignKey(c => c.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.FertilizerRequests)
                .WithOne(f => f.Supplier)
                .HasForeignKey(f => f.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.ItemRequests)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.RegNo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegSupplier>()
                .HasMany(r => r.LeafCollections)
                .WithOne(l => l.Supplier)
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

            // Configure FactorySetup to have only one record
            modelBuilder.Entity<FactorySetup>()
                .HasIndex(f => f.FactoryName)
                .IsUnique();

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
        }
    }
}