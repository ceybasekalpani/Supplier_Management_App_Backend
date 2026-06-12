using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DisbursementController : ControllerBase
    {
        private readonly IDisbursementService _disbursementService;

        public DisbursementController(IDisbursementService disbursementService)
        {
            _disbursementService = disbursementService;
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue(
            [FromQuery] string? route,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var result = await _disbursementService.GetApprovedQueueAsync(route, fromDate, toDate);
            return Ok(result);
        }

        [HttpPost("{issuedType}/{requestId:int}/issue")]
        public async Task<IActionResult> Issue(
            [FromRoute] string issuedType,
            [FromRoute] int requestId,
            [FromBody] IssueDisbursementDto request)
        {
            try
            {
                var issuedBy = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;
                var result = await _disbursementService.IssueAsync(issuedType, requestId, request.Method, issuedBy);

                if (result == null)
                {
                    return NotFound(new { message = "Approved request not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("tracking")]
        public async Task<IActionResult> GetTracking(
            [FromQuery] string? issuedType,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _disbursementService.GetTrackingAsync(issuedType, status, search, fromDate, toDate);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("tracking/{id:int}/received")]
        public async Task<IActionResult> MarkReceived([FromRoute] int id, [FromBody] MarkDisbursementReceivedDto request)
        {
            var completedBy = string.IsNullOrWhiteSpace(request.CompletedBy)
                ? User.Identity?.Name ?? User.FindFirst("unique_name")?.Value
                : request.CompletedBy;

            var result = await _disbursementService.MarkReceivedAsync(id, completedBy, request.CompletedDevice);

            if (result == null)
            {
                return NotFound(new { message = "Disbursement tracking record not found." });
            }

            return Ok(result);
        }
    }
}
