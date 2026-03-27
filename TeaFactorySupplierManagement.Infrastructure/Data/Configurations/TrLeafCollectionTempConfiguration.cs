using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Infrastructure.Data.Configurations
{
    public class TrLeafCollectionTempConfiguration : IEntityTypeConfiguration<TrLeafCollectionTemp>
    {
        public void Configure(EntityTypeBuilder<TrLeafCollectionTemp> builder)
        {
            builder.ToTable("Tr_LeafCollection_Temp");
            builder.HasKey(l => l.Ind);

            builder.Property(l => l.Ind)
                .HasColumnName("Ind")
                .ValueGeneratedOnAdd();

            builder.Property(l => l.RegNo)
                .HasColumnName("RegNo")
                .HasMaxLength(50);

            builder.Property(l => l.DayNo)
                .HasColumnName("DayNo");

            builder.Property(l => l.MonthName)
                .HasColumnName("MonthName")
                .HasMaxLength(20);

            builder.Property(l => l.LeafType)
                .HasColumnName("LeafType")
                .HasMaxLength(10);

            builder.Property(l => l.GrossQty)
                .HasColumnName("GrossQty")
                .HasColumnType("decimal(18,2)");

            builder.Property(l => l.WaterWgt)
                .HasColumnName("WaterWgt")
                .HasColumnType("decimal(18,2)");

            builder.Property(l => l.NoofBags)
                .HasColumnName("NoofBags");

            builder.Property(l => l.Qty)
                .HasColumnName("Qty")
                .HasColumnType("decimal(18,2)");

            builder.Property(l => l.LogTime)
                .HasColumnName("LogTime");

            builder.HasOne(l => l.Supplier)
                .WithMany(s => s.LeafCollections)
                .HasForeignKey(l => l.RegNo)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}