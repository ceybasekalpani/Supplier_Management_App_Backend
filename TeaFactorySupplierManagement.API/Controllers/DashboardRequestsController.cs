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
    public class DashboardRequestsController : ControllerBase
    {
        private static readonly string[] RequestModules =
        {
            PermissionModules.CashRequests,
            PermissionModules.FertilizerRequests,
            PermissionModules.ItemRequests
        };

        private readonly IDashboardRequestService _dashboardRequestService;
        private readonly IDashboardPermissionService _permissionService;
        private readonly ICashRequestService _cashRequestService;

        public DashboardRequestsController(
            IDashboardRequestService dashboardRequestService,
            IDashboardPermissionService permissionService,
            ICashRequestService cashRequestService)
        {
            _dashboardRequestService = dashboardRequestService;
            _permissionService = permissionService;
            _cashRequestService = cashRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            if (!await HasAnyRequestPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

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
                    reviewedBy,
                    GetCurrentDashboardAdminId()
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("advance-limit/{regNo:int}")]
        public async Task<IActionResult> GetAdvanceLimit([FromRoute] int regNo, [FromQuery] DateTime? salaryDate)
        {
            if (!await HasAnyRequestPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

            var limit = await _cashRequestService.GetAdvanceLimitAsync(regNo, salaryDate ?? DateTime.Today);
            return Ok(new { regNo, limit });
        }

        [HttpGet("advance-limits")]
        public async Task<IActionResult> GetAdvanceLimits([FromQuery] DateTime? salaryDate)
        {
            if (!await HasAnyRequestPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

            var limits = await _cashRequestService.GetAdvanceLimitsForAllSuppliersAsync(salaryDate ?? DateTime.Today);
            return Ok(limits);
        }

        private int? GetCurrentDashboardAdminId()
        {
            var adminIdClaim = User.FindFirst("DashboardAdminId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(adminIdClaim, out var adminId) ? adminId : null;
        }

        private async Task<bool> HasAnyRequestPermissionAsync(string action)
        {
            var adminId = User.GetDashboardAdminId();
            if (!adminId.HasValue)
            {
                return false;
            }

            foreach (var module in RequestModules)
            {
                if (await _permissionService.HasPermissionAsync(adminId.Value, $"{module}.{action}"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
