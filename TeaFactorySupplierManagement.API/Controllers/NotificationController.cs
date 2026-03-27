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

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var notifications = await _notificationService.GetNotificationsForSupplierAsync(regNo);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(regNo);
            return Ok(new { unreadCount = count });
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, regNo);
            return Ok(new { message = "Marked as read" });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(regNo);
            return Ok(new { message = "All notifications marked as read" });
        }
    }
}