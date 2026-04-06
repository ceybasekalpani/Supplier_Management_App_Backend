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
                .HasColumnName("RegNo");

            builder.Property(l => l.Route)
                .HasColumnName("Route")
                .HasMaxLength(200);

            builder.Property(l => l.Dealer)
                .HasColumnName("Dealer")
                .HasMaxLength(50);

            builder.Property(l => l.LeafType)
                .HasColumnName("LeafType")
                .HasMaxLength(10);

            builder.Property(l => l.Qty)
                .HasColumnName("Qty");

            builder.Property(l => l.Transport)
                .HasColumnName("Transport")
                .HasColumnType("money");

            builder.Property(l => l.DayNo)
                .HasColumnName("DayNo");

            builder.Property(l => l.MonthName)
                .HasColumnName("MonthName")
                .HasMaxLength(20);

            builder.Property(l => l.UserName)
                .HasColumnName("UserName")
                .HasMaxLength(50);

            builder.Property(l => l.LogTime)
                .HasColumnName("LogTime");

            builder.Property(l => l.PC_Name)
                .HasColumnName("PC_Name")
                .HasMaxLength(50);

            builder.Property(l => l.Gross)
                .HasColumnName("Gross")
                .HasColumnType("money");

            builder.Property(l => l.BagWeight)
                .HasColumnName("BagWeight")
                .HasColumnType("money");

            builder.Property(l => l.Water)
                .HasColumnName("Water");

            builder.Property(l => l.Coarse)
                .HasColumnName("Coarse");

            builder.Property(l => l.Rejected)
                .HasColumnName("Rejected");

            builder.Property(l => l.Shift)
                .HasColumnName("Shift")
                .HasMaxLength(1);

            builder.Property(l => l.NetWeight)
                .HasColumnName("NetWeight")
                .HasColumnType("money");

            builder.Property(l => l.Boild)
                .HasColumnName("Boild")
                .HasColumnType("money");

            builder.Property(l => l.Spd)
                .HasColumnName("Spd")
                .HasColumnType("money");

            builder.Property(l => l.RouteDeduct)
                .HasColumnName("RouteDeduct")
                .HasColumnType("money");

            builder.Property(l => l.RouteDeductPre)
                .HasColumnName("RouteDeductPre")
                .HasColumnType("money");

            builder.Property(l => l.Mode)
                .HasColumnName("Mode")
                .HasMaxLength(1);

            builder.Property(l => l.Excess_Leaf)
                .HasColumnName("Excess_Leaf")
                .HasColumnType("money");

            builder.Property(l => l.SmallHolderRefNo)
                .HasColumnName("SmallHolderRefNo");

            builder.Property(l => l.SmallHolderRefName)
                .HasColumnName("SmallHolderRefName")
                .HasMaxLength(1);

            builder.Property(l => l.Transfer)
                .HasColumnName("Transfer")
                .HasColumnType("money");

            builder.Property(l => l.TransferDeleted)
                .HasColumnName("TransferDeleted")
                .HasColumnType("money");

            builder.Property(l => l.IsTransfered)
                .HasColumnName("IsTransfered");

            builder.Property(l => l.IsDeduction)
                .HasColumnName("IsDeduction");

            builder.HasOne(l => l.Supplier)
                .WithMany(s => s.LeafCollections)
                .HasForeignKey(l => l.RegNo)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}