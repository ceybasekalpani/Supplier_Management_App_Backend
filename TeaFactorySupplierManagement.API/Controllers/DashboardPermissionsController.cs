using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

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
            var result = await _dashboardPermissionService.GetSettingsAsync();
            return Ok(result);
        }

        [HttpGet("{adminId:int}")]
        public async Task<IActionResult> GetUserPermissions([FromRoute] int adminId)
        {
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
            var result = await _dashboardPermissionService.SaveUserPermissionsAsync(adminId, request);
            if (result == null)
            {
                return NotFound(new { message = "Dashboard user not found." });
            }

            return Ok(result);
        }
    }
}
