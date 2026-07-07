using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.API.Extensions;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardMetricsService _dashboardMetricsService;
        private readonly IDashboardPermissionService _permissionService;

        public DashboardController(IDashboardMetricsService dashboardMetricsService, IDashboardPermissionService permissionService)
        {
            _dashboardMetricsService = dashboardMetricsService;
            _permissionService = permissionService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics([FromQuery] int months = 6, [FromQuery] int recent = 10)
        {
            var adminId = User.GetDashboardAdminId();
            if (!adminId.HasValue || !await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.Dashboard}.{PermissionActions.View}"))
            {
                return Forbid();
            }

            var metrics = await _dashboardMetricsService.GetMetricsAsync(months, recent);
            return Ok(metrics);
        }
    }
}
