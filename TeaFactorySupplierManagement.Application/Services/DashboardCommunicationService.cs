using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DashboardCommunicationService : IDashboardCommunicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;

        public DashboardCommunicationService(ApplicationDbContext context, IFirebasePushService firebasePushService)
        {
            _context = context;
            _firebasePushService = firebasePushService;
        }

        public async Task<CommunicationDashboardDto> GetAllAsync()
        {
            return new CommunicationDashboardDto
            {
                News = await GetNewsAsync(null),
                Notifications = await GetNotificationsAsync(null)
            };
        }

        public async Task<List<DashboardNewsDto>> GetNewsAsync(string? status)
        {
            var news = await _context.News
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var dismissedCounts = await _context.NewsDismissed
                .AsNoTracking()
                .GroupBy(x => x.NewsId)
                .Select(x => new { NewsId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.NewsId, x => x.Count);

            var rows = news
                .Select(x => ToNewsDto(x, dismissedCounts.TryGetValue(x.Id, out var count) ? count : 0))
                .ToList();

            var normalizedStatus = NormalizeFilter(status);
            if (normalizedStatus == "all")
            {
                return rows;
            }

            return rows.Where(x => x.Status == normalizedStatus).ToList();
        }

        public async Task<DashboardNewsDto> CreateNewsAsync(CreateDashboardNewsDto request, string? createdBy)
        {
            ValidateNews(request.Title, GetNewsContent(request));
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var now = DateTime.Now;
            var news = new News
            {
                Title = request.Title.Trim(),
                Content = GetNewsContent(request),
                Priority = request.Priority <= 0 ? 1 : request.Priority,
                IsActive = isActive,
                ShowPopup = request.ShowPopup,
                StartDate = request.StartDate?.Date,
                EndDate = request.ExpiryDate?.Date,
                TargetRegNo = targetRegNo,
                TargetRoute = targetRoute,
                CreatedBy = CleanActor(createdBy),
                CreatedAt = now
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            if (ShouldPublishNow(request.PublishNow, news.IsActive, news.StartDate, news.EndDate))
            {
                await _firebasePushService.SendNewsAsync(
                    news.Id,
                    news.Title,
                    news.Content,
                    NormalizeAudienceType(request.AudienceType),
                    news.TargetRegNo,
                    news.TargetRoute);
            }

            return ToNewsDto(news, 0);
        }

        public async Task<DashboardNewsDto?> UpdateNewsAsync(int id, UpdateDashboardNewsDto request)
        {
            ValidateNews(request.Title, GetNewsContent(request));
            ValidateAudience(request.AudienceType, request.TargetRegNo, request.TargetRoute);
            var targetRegNo = NormalizeTargetRegNo(request.AudienceType, request.TargetRegNo);
            var targetRoute = NormalizeTargetRoute(request.AudienceType, request.TargetRoute);
            var isActive = request.Active ?? request.IsActive;

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return null;
            }

            news.Title = request.Title.Trim();
            news.Content = GetNewsContent(request);
            news.Priority = request.Priority <= 0 ? 1 : request.Priority;
            news.IsActive = isActive;
            news.ShowPopup = request.ShowPopup;
            news.StartDate = request.StartDate?.Date;
            news.EndDate = request.ExpiryDate?.Date;
            news.TargetRegNo = targetRegNo;
            news.TargetRoute = targetRoute;
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var dismissedCount = await _context.NewsDismissed.CountAsync(x => x.NewsId == id);
            return ToNewsDto(news, dismissedCount);
        }

        public async Task<bool> DeleteNewsAsync(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return false;
            }

            EnsureCanDelete(news.CreatedAt, "News");

            var dismissedRows = await _context.NewsDismissed
                .Where(x => x.NewsId == id)
                .ToListAsync();

            _context.NewsDismissed.RemoveRange(dismissedRows);
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
