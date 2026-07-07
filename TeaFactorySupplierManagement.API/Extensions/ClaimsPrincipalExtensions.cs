using System.Security.Claims;

namespace TeaFactorySupplierManagement.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetDashboardAdminId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("DashboardAdminId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
