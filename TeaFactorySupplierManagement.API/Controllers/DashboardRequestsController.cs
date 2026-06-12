using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardRequestsController : ControllerBase
    {
        private readonly IDashboardRequestService _dashboardRequestService;

        public DashboardRequestsController(IDashboardRequestService dashboardRequestService)
        {
            _dashboardRequestService = dashboardRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _dashboardRequestService.GetRequestsAsync(status, search, fromDate, toDate);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{requestType}/{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            [FromRoute] string requestType,
            [FromRoute] int id,
            [FromBody] DashboardUpdateRequestStatusDto request)
        {
            try
            {
                var reviewedBy =
                    User.Identity?.Name ??
                    User.FindFirst("unique_name")?.Value ??
                    User.FindFirst("name")?.Value ??
                    "Dashboard Admin";

                var updated = await _dashboardRequestService.UpdateRequestStatusAsync(
                    requestType,
                    id,
                    request.Status,
                    request.Remarks,
                    reviewedBy
                );

                if (updated == null)
                {
                    return NotFound(new { message = "Request not found" });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}