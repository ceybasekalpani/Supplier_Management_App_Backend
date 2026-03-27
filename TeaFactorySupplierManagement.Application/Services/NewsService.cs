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

        public async Task<List<NewsDto>> GetActiveNewsAsync(string regNo)
        {
            var today = DateTime.UtcNow;

            var news = await _context.News
                .Where(n => n.IsActive &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today))
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dismissedNews = await _context.NewsDismissed
                .Where(nd => nd.RegNo == regNo)
                .Select(nd => nd.NewsId)
                .ToHashSetAsync();

            return news
                .Where(n => !dismissedNews.Contains(n.Id))
                .Select(n => _mapper.Map<NewsDto>(n))
                .ToList();
        }

        public async Task<NewsDto?> GetActivePopupNewsAsync(string regNo)
        {
            var today = DateTime.UtcNow;

            var news = await _context.News
                .Where(n => n.IsActive &&
                            n.ShowPopup &&
                            (n.StartDate == null || n.StartDate <= today) &&
                            (n.EndDate == null || n.EndDate >= today))
                .OrderByDescending(n => n.Priority)
                .FirstOrDefaultAsync();

            if (news == null) return null;

            var isDismissed = await _context.NewsDismissed
                .AnyAsync(nd => nd.NewsId == news.Id && nd.RegNo == regNo);

            if (isDismissed) return null;

            return _mapper.Map<NewsDto>(news);
        }

        public async Task DismissNewsAsync(int newsId, string regNo)
        {
            var exists = await _context.NewsDismissed
                .AnyAsync(nd => nd.NewsId == newsId && nd.RegNo == regNo);

            if (!exists)
            {
                _context.NewsDismissed.Add(new NewsDismissed
                {
                    NewsId = newsId,
                    RegNo = regNo,
                    DismissedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasActivePopupNewsAsync(string regNo)
        {
            var popupNews = await GetActivePopupNewsAsync(regNo);
            return popupNews != null;
        }
    }
}