using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Factory_Setup")]
    public class FactorySetup : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string FactoryName { get; set; } = string.Empty;

        public bool IsSuper { get; set; } = false;

        public bool IsCashRequest { get; set; } = true;

        public bool IsItemRequest { get; set; } = true;

        public bool IsFertilizerRequest { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdvanceLimit { get; set; }
    }
}