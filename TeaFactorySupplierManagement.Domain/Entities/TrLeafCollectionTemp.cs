using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Tr_LeafCollection_Temp")]
    public partial class TrLeafCollectionTemp
    {
        [Key]
        [Column("Ind")]
        public int Ind { get; set; }

        [Column("RegNo")]
        public int? RegNo { get; set; }

        [Column("Route")]
        [MaxLength(200)]
        public string? Route { get; set; }

        [Column("Dealer")]
        [MaxLength(50)]
        public string? Dealer { get; set; }

        [Column("LeafType")]
        [MaxLength(10)]
        public string? LeafType { get; set; }

        [Column("Qty")]
        public int? Qty { get; set; }

        [Column("Transport", TypeName = "money")]
        public decimal? Transport { get; set; }

        [Column("DayNo")]
        public int? DayNo { get; set; }

        [Column("MonthName")]
        [MaxLength(20)]
        public string? MonthName { get; set; }

        [Column("UserName")]
        [MaxLength(50)]
        public string? UserName { get; set; }

        [Column("LogTime")]
        public DateTime? LogTime { get; set; }

        [Column("PC_Name")]
        [MaxLength(50)]
        public string? PC_Name { get; set; }

        [Column("Gross", TypeName = "money")]
        public decimal? Gross { get; set; }

        [Column("BagWeight", TypeName = "money")]
        public decimal? BagWeight { get; set; }

        [Column("Water")]
        public int? Water { get; set; }

        [Column("Coarse")]
        public int? Coarse { get; set; }

        [Column("Rejected")]
        public int? Rejected { get; set; }

        [Column("Shift")]
        [MaxLength(1)]
        public string? Shift { get; set; }

        [Column("NetWeight", TypeName = "money")]
        public decimal? NetWeight { get; set; }

        [Column("Boild", TypeName = "money")]
        public decimal? Boild { get; set; }

        [Column("Spd", TypeName = "money")]
        public decimal? Spd { get; set; }

        [Column("RouteDeduct", TypeName = "money")]
        public decimal? RouteDeduct { get; set; }

        [Column("RouteDeductPre", TypeName = "money")]
        public decimal? RouteDeductPre { get; set; }

        [Column("Mode")]
        [MaxLength(1)]
        public string? Mode { get; set; }

        [Column("Excess_Leaf", TypeName = "money")]
        public decimal? Excess_Leaf { get; set; }

        [Column("SmallHolderRefNo")]
        public int? SmallHolderRefNo { get; set; }

        [Column("SmallHolderRefName")]
        [MaxLength(1)]
        public string? SmallHolderRefName { get; set; }

        [Column("Transfer", TypeName = "money")]
        public decimal? Transfer { get; set; }

        [Column("TransferDeleted", TypeName = "money")]
        public decimal? TransferDeleted { get; set; }

        [Column("IsTransfered")]
        public bool? IsTransfered { get; set; }

        [Column("IsDeduction")]
        public bool? IsDeduction { get; set; }

        // Navigation property
        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}