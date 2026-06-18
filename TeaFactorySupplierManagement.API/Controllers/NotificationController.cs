using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int? GetCurrentRegNo()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return null;
            }
            return regNo;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var notifications = await _notificationService.GetNotificationsForSupplierAsync(regNo.Value);
            return Ok(notifications);
        }

        [HttpGet("{id:int}")]
        [HttpGet("/api/mobile/notifications/{id:int}")]
        public async Task<IActionResult> GetNotificationById([FromRoute] int id)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var notification = await _notificationService.GetNotificationByIdAsync(id, regNo.Value);
            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            return Ok(notification);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var count = await _notificationService.GetUnreadCountAsync(regNo.Value);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            await _notificationService.MarkAsReadAsync(id, regNo.Value);
            return Ok(new { message = "Notification marked as read" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DismissNotification(int id)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            await _notificationService.DismissAsync(id, regNo.Value);
            return Ok(new { message = "Notification dismissed" });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            await _notificationService.MarkAllAsReadAsync(regNo.Value);
            return Ok(new { message = "All notifications marked as read" });
        }
    }
}
