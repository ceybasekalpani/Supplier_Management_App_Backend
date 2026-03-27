using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Login")]
    public class SupplierLogin : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string RegNo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}