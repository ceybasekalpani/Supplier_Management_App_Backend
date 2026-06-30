using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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

        [HttpPost("delivery-notes")]
        public async Task<IActionResult> GenerateDeliveryNote([FromBody] GenerateDeliveryNoteDto request)
        {
            try
            {
                var result = await _disbursementService.GenerateDeliveryNoteAsync(request, GetCurrentDashboardAdminId());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("delivery-notes")]
        public async Task<IActionResult> GetDeliveryNotes(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var result = await _disbursementService.GetDeliveryNotesAsync(status, search, fromDate, toDate);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("delivery-notes/{id:int}")]
        public async Task<IActionResult> GetDeliveryNote([FromRoute] int id)
        {
            var result = await _disbursementService.GetDeliveryNoteAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Delivery note not found." });
            }

            return Ok(result);
        }

        [HttpGet("delivery-notes/{id:int}/print")]
        public async Task<IActionResult> PrintDeliveryNote([FromRoute] int id)
        {
            var html = await _disbursementService.BuildDeliveryNotePrintHtmlAsync(id);
            if (html == null)
            {
                return NotFound(new { message = "Delivery note not found." });
            }

            return Content(html, "text/html");
        }

        [HttpPost("delivery-notes/{id:int}/returned")]
        public async Task<IActionResult> MarkDeliveryNoteReturned([FromRoute] int id, [FromBody] MarkDeliveryNoteStatusDto? request)
        {
            var result = await _disbursementService.MarkDeliveryNoteReturnedAsync(id, request?.Remarks);
            if (result == null)
            {
                return NotFound(new { message = "Delivery note not found." });
            }

            return Ok(result);
        }

        [HttpPost("delivery-notes/{id:int}/completed")]
        public async Task<IActionResult> MarkDeliveryNoteCompleted([FromRoute] int id, [FromBody] MarkDeliveryNoteStatusDto? request)
        {
            var completedBy = string.IsNullOrWhiteSpace(request?.CompletedBy)
                ? User.Identity?.Name ?? User.FindFirst("unique_name")?.Value
                : request.CompletedBy;

            var result = await _disbursementService.MarkDeliveryNoteCompletedAsync(id, request?.Remarks, completedBy, request?.CompletedDevice);
            if (result == null)
            {
                return NotFound(new { message = "Delivery note not found." });
            }

            return Ok(result);
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

        private int? GetCurrentDashboardAdminId()
        {
            var adminIdClaim = User.FindFirst("DashboardAdminId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(adminIdClaim, out var adminId) ? adminId : null;
        }
    }
}
