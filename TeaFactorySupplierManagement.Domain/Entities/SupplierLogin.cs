using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Login")]
    public class SupplierLogin : BaseEntity
    {
        [Required]
        public int RegNo { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string? PinHash { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}