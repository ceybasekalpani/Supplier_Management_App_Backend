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

        public async Task<List<NotificationDto>> GetNotificationsForSupplierAsync(string regNo)
        {
            var today = DateTime.UtcNow;

            var notifications = await _context.Notifications
                .Where(n => n.IsActive &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today) &&
                            (n.TargetRegNo == null || n.TargetRegNo == regNo))
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            var readNotifications = await _context.NotificationReads
                .Where(nr => nr.RegNo == regNo)
                .Select(nr => nr.NotificationId)
                .ToHashSetAsync();

            var result = new List<NotificationDto>();
            foreach (var notification in notifications)
            {
                result.Add(new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    CreatedAt = notification.CreatedAt,
                    IsRead = readNotifications.Contains(notification.Id)
                });
            }

            return result;
        }

        public async Task<int> GetUnreadCountAsync(string regNo)
        {
            var notifications = await GetNotificationsForSupplierAsync(regNo);
            return notifications.Count(n => !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string regNo)
        {
            var exists = await _context.NotificationReads
                .AnyAsync(nr => nr.NotificationId == notificationId && nr.RegNo == regNo);

            if (!exists)
            {
                _context.NotificationReads.Add(new NotificationRead
                {
                    NotificationId = notificationId,
                    RegNo = regNo,
                    ReadAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string regNo)
        {
            var notifications = await GetNotificationsForSupplierAsync(regNo);
            var unreadIds = notifications.Where(n => !n.IsRead).Select(n => n.Id);

            foreach (var id in unreadIds)
            {
                var exists = await _context.NotificationReads
                    .AnyAsync(nr => nr.NotificationId == id && nr.RegNo == regNo);

                if (!exists)
                {
                    _context.NotificationReads.Add(new NotificationRead
                    {
                        NotificationId = id,
                        RegNo = regNo,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}