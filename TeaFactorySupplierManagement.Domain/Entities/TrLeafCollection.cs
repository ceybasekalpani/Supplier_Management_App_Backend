using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Tr_LeafCollection")]
    public partial class TrLeafCollection
    {
        [Key]
        [Column("Ind")]
        public int Ind { get; set; }

        [Column("RegNo")]
        public int? RegNo { get; set; }

        [Column("Route")]
        [MaxLength(50)]
        public string? Route { get; set; }

        [Column("RegName")]
        [MaxLength(100)]
        public string? RegName { get; set; }

        [Column("LeafType")]
        [MaxLength(10)]
        public string? LeafType { get; set; }

        [Column("DayNo")]
        public int? DayNo { get; set; }

        [Column("MonthName")]
        [MaxLength(20)]
        public string? MonthName { get; set; }

        [Column("GrossQty")]
        public int? GrossQty { get; set; }

        [Column("WaterWgt")]
        public int? WaterWgt { get; set; }

        [Column("BagWgt")]
        public int? BagWgt { get; set; }

        [Column("CoarseWgt")]
        public int? CoarseWgt { get; set; }

        [Column("RejectedWgt")]
        public int? RejectedWgt { get; set; }

        [Column("BoiledWgt")]
        public int? BoiledWgt { get; set; }
    }
}
