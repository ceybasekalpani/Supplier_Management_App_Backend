using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class CommunicationsController : ControllerBase
    {
        private readonly IDashboardCommunicationService _communicationService;

        public CommunicationsController(IDashboardCommunicationService communicationService)
        {
            _communicationService = communicationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _communicationService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("news")]
        public async Task<IActionResult> GetNews([FromQuery] string? status)
        {
            var result = await _communicationService.GetNewsAsync(status);
            return Ok(result);
        }

        [HttpPost("news")]
        public async Task<IActionResult> CreateNews([FromBody] CreateDashboardNewsDto request)
        {
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
            var result = await _communicationService.GetNotificationsAsync(status);
            return Ok(result);
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateDashboardNotificationDto request)
        {
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
    }
}
