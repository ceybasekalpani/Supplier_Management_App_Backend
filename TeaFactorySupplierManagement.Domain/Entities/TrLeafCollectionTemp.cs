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
        [MaxLength(50)]
        public string? RegNo { get; set; }

        [Column("DayNo")]
        public int? DayNo { get; set; }

        [Column("MonthName")]
        [MaxLength(20)]
        public string? MonthName { get; set; }

        [Column("LeafType")]
        [MaxLength(10)]
        public string? LeafType { get; set; }

        [Column("GrossQty", TypeName = "decimal(18,2)")]
        public decimal? GrossQty { get; set; }

        [Column("WaterWgt", TypeName = "decimal(18,2)")]
        public decimal? WaterWgt { get; set; }

        [Column("NoofBags")]
        public int? NoofBags { get; set; }

        [Column("Qty", TypeName = "decimal(18,2)")]
        public decimal? Qty { get; set; }

        [Column("LogTime")]
        public DateTime? LogTime { get; set; }

        // Navigation property
        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}