using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Code_Generation")]
    public class CodeGeneration : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string CodePrefix { get; set; } = string.Empty;

        public int Year { get; set; }

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public int LastNo { get; set; }
    }
}
