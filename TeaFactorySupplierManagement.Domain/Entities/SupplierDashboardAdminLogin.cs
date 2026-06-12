using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("supplier_dashboard_AdminLogin")]
    public class SupplierDashboardAdminLogin : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? PhoneNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Admin";

        public string? AvatarBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<SupplierDashboardAdminPermission> Permissions { get; set; } = new List<SupplierDashboardAdminPermission>();
    }
}
