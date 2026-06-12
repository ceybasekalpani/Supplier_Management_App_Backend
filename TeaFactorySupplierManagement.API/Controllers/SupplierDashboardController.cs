using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class SupplierDashboardController : ControllerBase
    {
        private readonly ISupplierDashboardService _supplierDashboardService;

        public SupplierDashboardController(ISupplierDashboardService supplierDashboardService)
        {
            _supplierDashboardService = supplierDashboardService;
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] string? search,
            [FromQuery] int months = 2,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _supplierDashboardService.GetSuppliersAsync(search, months, activeOnly);
            return Ok(result);
        }

        [HttpGet("suppliers/{regNo:int}")]
        public async Task<IActionResult> GetSupplier([FromRoute] int regNo, [FromQuery] int months = 2)
        {
            var supplier = await _supplierDashboardService.GetSupplierAsync(regNo, months);

            if (supplier == null)
            {
                return NotFound(new { message = "Supplier not found" });
            }

            return Ok(supplier);
        }
    }
}
