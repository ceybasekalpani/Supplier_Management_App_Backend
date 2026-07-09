using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.API.Extensions;
using TeaFactorySupplierManagement.Application.DTOs.Factory;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FactoryController : ControllerBase
    {
        private readonly IFactorySetupService _factorySetupService;
        private readonly IDashboardPermissionService _permissionService;

        public FactoryController(IFactorySetupService factorySetupService, IDashboardPermissionService permissionService)
        {
            _factorySetupService = factorySetupService;
            _permissionService = permissionService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetFactorySettings()
        {
            var settings = await _factorySetupService.GetFactorySetupAsync();
            return Ok(settings);
        }

        [HttpGet("features")]
        public async Task<IActionResult> GetFactoryFeatures()
        {
            var features = await _factorySetupService.GetFactoryFeaturesAsync();
            return Ok(features);
        }

        [HttpPut("salary-date")]
        public async Task<IActionResult> UpdateSalaryDate([FromBody] UpdateSalaryDateDto request)
        {
            var adminId = User.GetDashboardAdminId();
            var canUpdate = adminId.HasValue &&
                await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.Settings}.{PermissionActions.Update}");

            if (!canUpdate)
            {
                return Forbid();
            }

            var updated = await _factorySetupService.UpdateSalaryDateAsync(request.SalaryDate);
            return Ok(updated);
        }
    }
}