using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardMetricsService _dashboardMetricsService;

        public DashboardController(IDashboardMetricsService dashboardMetricsService)
        {
            _dashboardMetricsService = dashboardMetricsService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics([FromQuery] int months = 6, [FromQuery] int recent = 10)
        {
            var metrics = await _dashboardMetricsService.GetMetricsAsync(months, recent);
            return Ok(metrics);
        }
    }
}
