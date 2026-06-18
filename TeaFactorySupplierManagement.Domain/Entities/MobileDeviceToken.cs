using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_MobileDeviceTokens")]
    public class MobileDeviceToken : BaseEntity
    {
        public int RegNo { get; set; }

        [Required]
        public string FcmToken { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Platform { get; set; }

        [MaxLength(200)]
        public string? DeviceId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RegNo))]
        public virtual RegSupplier? Supplier { get; set; }
    }
}
