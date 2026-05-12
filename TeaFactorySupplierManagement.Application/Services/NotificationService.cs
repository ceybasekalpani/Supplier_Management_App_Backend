using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Notifications;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public NotificationService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // In NotificationService.cs - Update the GetNotificationsForSupplierAsync method
        public async Task<List<NotificationDto>> GetNotificationsForSupplierAsync(int regNo)
        {
            var today = DateTime.Now;

            var notifications = await _context.Notifications
                .Where(n => n.IsActive &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today) &&
                            (n.TargetRegNo == null || n.TargetRegNo == regNo))
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var readNotificationIds = await _context.NotificationRead
                .Where(nr => nr.RegNo == regNo)
                .Select(nr => nr.NotificationId)
                .ToListAsync();

            var readSet = new HashSet<int>(readNotificationIds);

            var dismissedNotificationIds = await _context.NotificationDismissed
                .Where(nd => nd.RegNo == regNo)
                .Select(nd => nd.NotificationId)
                .ToListAsync();

            var dismissedSet = new HashSet<int>(dismissedNotificationIds);

            return notifications
                .Where(n => !dismissedSet.Contains(n.Id))
                .Select(notification => new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title ?? string.Empty,
                    Message = notification.Message ?? string.Empty,
                    Type = notification.Type ?? "info",
                    CreatedAt = notification.CreatedAt,
                    IsRead = readSet.Contains(notification.Id)
                }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(int regNo)
        {
            var notifications = await GetNotificationsForSupplierAsync(regNo);
            return notifications.Count(n => !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, int regNo)
        {
            // Check if notification exists
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                throw new ArgumentException($"Notification with ID {notificationId} not found");

            var exists = await _context.NotificationRead
                .AnyAsync(nr => nr.NotificationId == notificationId && nr.RegNo == regNo);

            if (!exists)
            {
                _context.NotificationRead.Add(new NotificationRead
                {
                    NotificationId = notificationId,
                    RegNo = regNo,
                    ReadAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task DismissAsync(int notificationId, int regNo)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                throw new ArgumentException($"Notification with ID {notificationId} not found");

            var exists = await _context.NotificationDismissed
                .AnyAsync(nd => nd.NotificationId == notificationId && nd.RegNo == regNo);

            if (!exists)
            {
                _context.NotificationDismissed.Add(new NotificationDismissed
                {
                    NotificationId = notificationId,
                    RegNo = regNo,
                    DismissedAt = DateTime.Now,
                    Title = notification.Title ?? string.Empty,
                    Message = notification.Message ?? string.Empty
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int regNo)
        {
            var notifications = await GetNotificationsForSupplierAsync(regNo);
            var unreadIds = notifications.Where(n => !n.IsRead).Select(n => n.Id).ToList();

            foreach (var id in unreadIds)
            {
                var exists = await _context.NotificationRead
                    .AnyAsync(nr => nr.NotificationId == id && nr.RegNo == regNo);

                if (!exists)
                {
                    _context.NotificationRead.Add(new NotificationRead
                    {
                        NotificationId = id,
                        RegNo = regNo,
                        ReadAt = DateTime.Now
                    });
                }
            }

            if (unreadIds.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}