using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_FertilizerItem_Store")]
    public class FertilizerItemStore : BaseEntity
    {
        [MaxLength(100)]
        public string? FertilizerType { get; set; }

        [MaxLength(100)]
        public string? ItemType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
