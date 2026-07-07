using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    
    [Table("supplier_dashboard_Permissions")]
    public class Permission : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ModuleLabel { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public virtual ICollection<SupplierDashboardAdminPermission> AdminGrants { get; set; } = new List<SupplierDashboardAdminPermission>();
    }
}
