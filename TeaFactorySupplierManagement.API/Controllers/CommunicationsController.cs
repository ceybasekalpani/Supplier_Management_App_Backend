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
    public class CommunicationsController : ControllerBase
    {
        private readonly IDashboardCommunicationService _communicationService;
        private readonly IDashboardPermissionService _permissionService;

        public CommunicationsController(IDashboardCommunicationService communicationService, IDashboardPermissionService permissionService)
        {
            _communicationService = communicationService;
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var adminId = User.GetDashboardAdminId();
            var canView = adminId.HasValue && (
                await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.News}.{PermissionActions.View}") ||
                await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.Notifications}.{PermissionActions.View}"));

            if (!canView)
            {
                return Forbid();
            }

            var result = await _communicationService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("news")]
        public async Task<IActionResult> GetNews([FromQuery] string? status)
        {
            if (!await HasPermissionAsync(PermissionModules.News, PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _communicationService.GetNewsAsync(status);
            return Ok(result);
        }

        [HttpPost("news")]
        public async Task<IActionResult> CreateNews([FromBody] CreateDashboardNewsDto request)
        {
            if (!await HasPermissionAsync(PermissionModules.News, PermissionActions.Create))
            {
                return Forbid();
            }

            try
            {
                var result = await _communicationService.CreateNewsAsync(request, GetDashboardUser());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("news/{id:int}")]
        public async Task<IActionResult> UpdateNews([FromRoute] int id, [FromBody] UpdateDashboardNewsDto request)
        {
            if (!await HasPermissionAsync(PermissionModules.News, PermissionActions.Update))
            {
                return Forbid();
            }

            try
            {
                var result = await _communicationService.UpdateNewsAsync(id, request);
                if (result == null)
                {
                    return NotFound(new { message = "News item not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("news/{id:int}")]
        public async Task<IActionResult> DeleteNews([FromRoute] int id)
        {
            if (!await HasPermissionAsync(PermissionModules.News, PermissionActions.Delete))
            {
                return Forbid();
            }

            try
            {
                var deleted = await _communicationService.DeleteNewsAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "News item not found." });
                }

                return Ok(new { message = "News deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] string? status)
        {
            if (!await HasPermissionAsync(PermissionModules.Notifications, PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _communicationService.GetNotificationsAsync(status);
            return Ok(result);
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateDashboardNotificationDto request)
        {
            if (!await HasPermissionAsync(PermissionModules.Notifications, PermissionActions.Create))
            {
                return Forbid();
            }

            try
            {
                var result = await _communicationService.CreateNotificationAsync(request, GetDashboardUser());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("notifications/{id:int}")]
        public async Task<IActionResult> UpdateNotification([FromRoute] int id, [FromBody] UpdateDashboardNotificationDto request)
        {
            if (!await HasPermissionAsync(PermissionModules.Notifications, PermissionActions.Update))
            {
                return Forbid();
            }

            try
            {
                var result = await _communicationService.UpdateNotificationAsync(id, request);
                if (result == null)
                {
                    return NotFound(new { message = "Notification not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("notifications/{id:int}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] int id)
        {
            if (!await HasPermissionAsync(PermissionModules.Notifications, PermissionActions.Delete))
            {
                return Forbid();
            }

            try
            {
                var deleted = await _communicationService.DeleteNotificationAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Notification not found." });
                }

                return Ok(new { message = "Notification deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private string? GetDashboardUser()
        {
            return User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;
        }

        private async Task<bool> HasPermissionAsync(string module, string action)
        {
            var adminId = User.GetDashboardAdminId();
            return adminId.HasValue && await _permissionService.HasPermissionAsync(adminId.Value, $"{module}.{action}");
        }
    }
}
