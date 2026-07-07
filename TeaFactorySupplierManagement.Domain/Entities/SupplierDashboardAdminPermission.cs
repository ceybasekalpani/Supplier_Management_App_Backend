using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    /// <summary>
    /// Grants a single permission from the catalog to a single dashboard admin user.
    /// Assignment is always per-user (never per-role) - a row's presence with
    /// <see cref="IsGranted"/> true means that specific admin can perform that specific
    /// module/action.
    /// </summary>
    [Table("supplier_dashboard_AdminPermissions")]
    public class SupplierDashboardAdminPermission : BaseEntity
    {
        public int AdminId { get; set; }

        public int PermissionId { get; set; }

        public bool IsGranted { get; set; }

        [ForeignKey(nameof(AdminId))]
        public virtual SupplierDashboardAdminLogin? Admin { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public virtual Permission? Permission { get; set; }
    }
}
