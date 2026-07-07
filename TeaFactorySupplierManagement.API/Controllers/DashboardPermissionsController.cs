using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.API.Extensions;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardPermissionsController : ControllerBase
    {
        private readonly IDashboardPermissionService _dashboardPermissionService;

        public DashboardPermissionsController(IDashboardPermissionService dashboardPermissionService)
        {
            _dashboardPermissionService = dashboardPermissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            if (!await HasPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _dashboardPermissionService.GetSettingsAsync();
            return Ok(result);
        }

        [HttpGet("{adminId:int}")]
        public async Task<IActionResult> GetUserPermissions([FromRoute] int adminId)
        {
            // Every admin may always read their own permission set (needed to render their
            // own UI) - viewing someone else's requires permissionManagement.view.
            var isOwnPermissions = User.GetDashboardAdminId() == adminId;
            if (!isOwnPermissions && !await HasPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _dashboardPermissionService.GetUserPermissionsAsync(adminId);
            if (result == null)
            {
                return NotFound(new { message = "Dashboard user not found." });
            }

            return Ok(result);
        }

        [HttpPut("{adminId:int}")]
        public async Task<IActionResult> SaveUserPermissions(
            [FromRoute] int adminId,
            [FromBody] SaveDashboardUserPermissionsDto request)
        {
            if (!await HasPermissionAsync(PermissionActions.Assign))
            {
                return Forbid();
            }

            var result = await _dashboardPermissionService.SaveUserPermissionsAsync(adminId, request);
            if (result == null)
            {
                return NotFound(new { message = "Dashboard user not found." });
            }

            return Ok(result);
        }

        private async Task<bool> HasPermissionAsync(string action)
        {
            var adminId = User.GetDashboardAdminId();
            return adminId.HasValue && await _dashboardPermissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.PermissionManagement}.{action}");
        }
    }
}
