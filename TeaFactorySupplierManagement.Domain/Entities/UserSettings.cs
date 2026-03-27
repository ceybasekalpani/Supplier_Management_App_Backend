using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Settings")]
    public class UserSettings : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string RegNo { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Theme { get; set; } = "light";

        [MaxLength(20)]
        public string? Language { get; set; } = "english";

        public int? FontSize { get; set; } = 50;

        [MaxLength(500)]
        public string? ProfileImage { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? AccountNumber { get; set; }

        [MaxLength(100)]
        public string? AccountHolder { get; set; }

        [MaxLength(100)]
        public string? Branch { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}