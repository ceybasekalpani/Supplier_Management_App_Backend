using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Domain.Permissions
{
    public static class PermissionEvaluationHelpers
    {
        public static bool IsSuperAdmin(SupplierDashboardAdminLogin admin)
        {
            return admin.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("mainadmin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("main_admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
