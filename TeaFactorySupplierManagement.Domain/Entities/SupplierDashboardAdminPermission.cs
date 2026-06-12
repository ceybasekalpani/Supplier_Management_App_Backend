using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("supplier_dashboard_AdminPermissions")]
    public class SupplierDashboardAdminPermission : BaseEntity
    {
        public int AdminId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ModuleId { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }

        public string SubPermissionsJson { get; set; } = "[]";

        [ForeignKey(nameof(AdminId))]
        public virtual SupplierDashboardAdminLogin? Admin { get; set; }
    }
}
