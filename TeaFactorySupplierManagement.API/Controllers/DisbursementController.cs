using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeaFactorySupplierManagement.API.Extensions;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DisbursementController : ControllerBase
    {
        private readonly IDisbursementService _disbursementService;
        private readonly IDashboardPermissionService _permissionService;

        public DisbursementController(IDisbursementService disbursementService, IDashboardPermissionService permissionService)
        {
            _disbursementService = disbursementService;
            _permissionService = permissionService;
        }

        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue(
            [FromQuery] string? route,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _disbursementService.GetApprovedQueueAsync(route, fromDate, toDate);
            return Ok(result);
        }

        [HttpPost("{issuedType}/{requestId:int}/issue")]
        public async Task<IActionResult> Issue(
            [FromRoute] string issuedType,
            [FromRoute] int requestId,
            [FromBody] IssueDisbursementDto request)
        {
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.Create))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.DisbursementTracking, PermissionActions.View))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.Create))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.View))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.View))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.View))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.Update))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.Disbursements, PermissionActions.Update))
            {
                return Forbid();
            }

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
            if (!await HasPermissionAsync(PermissionModules.DisbursementTracking, PermissionActions.Update))
            {
                return Forbid();
            }

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

        private async Task<bool> HasPermissionAsync(string module, string action)
        {
            var adminId = User.GetDashboardAdminId();
            return adminId.HasValue && await _permissionService.HasPermissionAsync(adminId.Value, $"{module}.{action}");
        }
    }
}
