using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Reg_Settings")]
    public partial class RegSettings
    {
        [Column("ActiveMonth")]
        public string ActiveMonth { get; set; } = string.Empty;

        [Column("NextMonth")]
        public string NextMonth { get; set; } = string.Empty;

        [Column("ActAdvRate", TypeName = "decimal(18,2)")]
        public decimal ActAdvRate { get; set; }

        [Column("ActAdvRate_Super", TypeName = "decimal(18,2)")]
        public decimal ActAdvRate_Super { get; set; }

        [Column("NxtAdvRate", TypeName = "decimal(18,2)")]
        public decimal NxtAdvRate { get; set; }

        [Column("NxtAdvRate_Super", TypeName = "decimal(18,2)")]
        public decimal NxtAdvRate_Super { get; set; }

        [Column("ActFPayDate")]
        public DateTime ActFPayDate { get; set; }
    }
}
