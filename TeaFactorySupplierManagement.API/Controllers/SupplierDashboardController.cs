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
    public class SupplierDashboardController : ControllerBase
    {
        private readonly ISupplierDashboardService _supplierDashboardService;
        private readonly IDashboardPermissionService _permissionService;

        public SupplierDashboardController(ISupplierDashboardService supplierDashboardService, IDashboardPermissionService permissionService)
        {
            _supplierDashboardService = supplierDashboardService;
            _permissionService = permissionService;
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] string? search,
            [FromQuery] int months = 2,
            [FromQuery] bool activeOnly = true)
        {
            if (!await HasSuppliersViewPermissionAsync())
            {
                return Forbid();
            }

            var result = await _supplierDashboardService.GetSuppliersAsync(search, months, activeOnly);
            return Ok(result);
        }

        [HttpGet("suppliers/{regNo:int}")]
        public async Task<IActionResult> GetSupplier([FromRoute] int regNo, [FromQuery] int months = 2)
        {
            if (!await HasSuppliersViewPermissionAsync())
            {
                return Forbid();
            }

            var supplier = await _supplierDashboardService.GetSupplierAsync(regNo, months);

            if (supplier == null)
            {
                return NotFound(new { message = "Supplier not found" });
            }

            return Ok(supplier);
        }

        private async Task<bool> HasSuppliersViewPermissionAsync()
        {
            var adminId = User.GetDashboardAdminId();
            return adminId.HasValue && await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.Suppliers}.{PermissionActions.View}");
        }
    }
}
