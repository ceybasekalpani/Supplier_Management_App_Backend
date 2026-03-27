using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Infrastructure.Data.Configurations
{
    public class RegSupplierConfiguration : IEntityTypeConfiguration<RegSupplier>
    {
        public void Configure(EntityTypeBuilder<RegSupplier> builder)
        {
            builder.ToTable("Reg_Supplier");
            builder.HasKey(r => r.RegNo);

            builder.Property(r => r.RegNo)
                .HasColumnName("RegNo")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.RegName)
                .HasColumnName("RegName")
                .HasMaxLength(200);

            builder.Property(r => r.Route)
                .HasColumnName("Route")
                .HasMaxLength(200);

            builder.Property(r => r.Address)
                .HasColumnName("Address")
                .HasMaxLength(500);

            builder.Property(r => r.TelNo)
                .HasColumnName("TelNo")
                .HasMaxLength(15);

            builder.Property(r => r.IDNo)
                .HasColumnName("IDNo")
                .HasMaxLength(20);

            builder.Property(r => r.BankName)
                .HasColumnName("BankName")
                .HasMaxLength(100);

            builder.Property(r => r.AccCode)
                .HasColumnName("AccCode")
                .HasMaxLength(50);

            builder.Property(r => r.AccName)
                .HasColumnName("AccName")
                .HasMaxLength(200);

            builder.Property(r => r.Active)
                .HasColumnName("Active");

            // Ignore properties not needed from database
            builder.Ignore(r => r.Logins);
            builder.Ignore(r => r.Settings);
            builder.Ignore(r => r.CashRequests);
            builder.Ignore(r => r.FertilizerRequests);
            builder.Ignore(r => r.ItemRequests);
            builder.Ignore(r => r.LeafCollections);
        }
    }
}