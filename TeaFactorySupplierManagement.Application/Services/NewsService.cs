using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Notifications;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class NewsService : INewsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public NewsService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // In NewsService.cs - Update GetActiveNewsAsync to use AutoMapper consistently
        public async Task<List<NewsDto>> GetActiveNewsAsync(int regNo)
        {
            var today = DateTime.Now.Date;

            var news = await _context.News
                .Where(n => n.IsActive &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today))
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            var dismissedNewsIds = await _context.NewsDismissed
                .Where(nd => nd.RegNo == regNo)
                .Select(nd => nd.NewsId)
                .ToListAsync();

            var dismissedSet = new HashSet<int>(dismissedNewsIds);

            // Filter out dismissed news before mapping
            var activeNews = news.Where(n => !dismissedSet.Contains(n.Id)).ToList();

            // Use AutoMapper for all mappings
            return _mapper.Map<List<NewsDto>>(activeNews);
        }

        // In NewsService.cs - Update the GetActivePopupNewsAsync method
        public async Task<NewsDto?> GetActivePopupNewsAsync(int regNo)
        {
            var today = DateTime.Now.Date;

            // First get active popup news
            var news = await _context.News
                .Where(n => n.IsActive &&
                            n.ShowPopup &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today))
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (news == null)
                return null;

            // Check if this specific news was dismissed by the user
            var isDismissed = await _context.NewsDismissed
                .AnyAsync(nd => nd.NewsId == news.Id && nd.RegNo == regNo);

            if (isDismissed)
                return null;

            return _mapper.Map<NewsDto>(news);
        }

        // In NewsService.cs - Add null check for DismissNewsAsync
        public async Task DismissNewsAsync(int newsId, int regNo)
        {
            // Check if news exists and is active
            var news = await _context.News.FindAsync(newsId);
            if (news == null)
                throw new ArgumentException($"News with ID {newsId} not found");

            // Check if news is still active (optional)
            var today = DateTime.Now.Date;
            if (!news.IsActive ||
                (news.StartDate.HasValue && news.StartDate > today) ||
                (news.EndDate.HasValue && news.EndDate < today))
            {
                throw new InvalidOperationException("Cannot dismiss inactive or expired news");
            }

            var exists = await _context.NewsDismissed
                .AnyAsync(nd => nd.NewsId == newsId && nd.RegNo == regNo);

            if (!exists)
            {
                _context.NewsDismissed.Add(new NewsDismissed
                {
                    NewsId = newsId,
                    RegNo = regNo,
                    DismissedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasActivePopupNewsAsync(int regNo)
        {
            var popupNews = await GetActivePopupNewsAsync(regNo);
            return popupNews != null;
        }
    }
}