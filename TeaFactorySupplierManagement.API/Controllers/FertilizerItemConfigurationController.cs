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
    public class FertilizerItemConfigurationController : ControllerBase
    {
        private readonly IFertilizerItemConfigurationService _configurationService;
        private readonly IDashboardPermissionService _permissionService;

        public FertilizerItemConfigurationController(IFertilizerItemConfigurationService configurationService, IDashboardPermissionService permissionService)
        {
            _configurationService = configurationService;
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = true)
        {
            var adminId = User.GetDashboardAdminId();
            var canView = adminId.HasValue && (
                await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.FertilizerConfiguration}.{PermissionActions.View}") ||
                await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.ItemConfiguration}.{PermissionActions.View}"));

            if (!canView)
            {
                return Forbid();
            }

            var result = await _configurationService.GetAllAsync(includeInactive);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFertilizerItemConfigurationDto request)
        {
            if (!await HasCategoryPermissionAsync(request.Category, PermissionActions.Create))
            {
                return Forbid();
            }

            try
            {
                var result = await _configurationService.CreateAsync(request.Category, request.Name);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{category}/{id:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] string category,
            [FromRoute] int id,
            [FromBody] UpdateFertilizerItemConfigurationDto request)
        {
            if (!await HasCategoryPermissionAsync(category, PermissionActions.Update))
            {
                return Forbid();
            }

            try
            {
                var result = await _configurationService.UpdateAsync(category, id, request.Name, request.IsActive);

                if (result == null)
                {
                    return NotFound(new { message = "Configuration item not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPatch("{category}/{id:int}/active")]
        public async Task<IActionResult> SetActive(
            [FromRoute] string category,
            [FromRoute] int id,
            [FromBody] UpdateFertilizerItemConfigurationDto request)
        {
            if (!await HasCategoryPermissionAsync(category, PermissionActions.Update))
            {
                return Forbid();
            }

            try
            {
                var result = await _configurationService.SetActiveAsync(category, id, request.IsActive ?? true);

                if (result == null)
                {
                    return NotFound(new { message = "Configuration item not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<bool> HasCategoryPermissionAsync(string category, string action)
        {
            var adminId = User.GetDashboardAdminId();
            if (!adminId.HasValue)
            {
                return false;
            }

            var module = category?.Trim().ToLowerInvariant() switch
            {
                "fertilizer" or "fertilizers" => PermissionModules.FertilizerConfiguration,
                "item" or "items" => PermissionModules.ItemConfiguration,
                _ => null
            };

            if (module == null)
            {
                return false;
            }

            return await _permissionService.HasPermissionAsync(adminId.Value, $"{module}.{action}");
        }
    }
}
