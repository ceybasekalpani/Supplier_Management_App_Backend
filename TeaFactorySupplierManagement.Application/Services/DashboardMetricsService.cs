using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardMetricsService : IDashboardMetricsService
    {
        private readonly ApplicationDbContext _context;

        public DashboardMetricsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardMetricsDto> GetMetricsAsync(int months = 6, int recent = 10)
        {
            months = Math.Clamp(months, 1, 12);
            recent = Math.Clamp(recent, 1, 50);

            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var previousMonth = currentMonth.AddMonths(-1);
            var nextMonth = currentMonth.AddMonths(1);
            var chartFromMonth = currentMonth.AddMonths(-(months - 1));

            var cashRows = await _context.CashRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .ToListAsync();

            var fertilizerRows = await _context.FertilizerRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .ToListAsync();

            var itemRows = await _context.ItemRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .ToListAsync();

            var activeSuppliers = await _context.RegSuppliers
                .AsNoTracking()
                .CountAsync(s => s.Active == true);

            var twoMonthCashRows = cashRows
                .Where(r => r.RequestMonth >= previousMonth && r.RequestMonth < nextMonth)
                .ToList();

            var twoMonthFertilizerRows = fertilizerRows
                .Where(r => r.RequestMonth >= previousMonth && r.RequestMonth < nextMonth)
                .ToList();

            var twoMonthItemRows = itemRows
                .Where(r => r.RequestMonth >= previousMonth && r.RequestMonth < nextMonth)
                .ToList();

            var chartCashRows = cashRows
                .Where(r => r.RequestMonth >= chartFromMonth)
                .ToList();

            var chartFertilizerRows = fertilizerRows
                .Where(r => r.RequestMonth >= chartFromMonth)
                .ToList();

            var chartItemRows = itemRows
                .Where(r => r.RequestMonth >= chartFromMonth)
                .ToList();

            return new DashboardMetricsDto
            {
                Stats = new List<DashboardStatDto>
                {
                    new()
                    {
                        Key = "activeSuppliers",
                        Label = "Active Suppliers",
                        Value = activeSuppliers.ToString("N0"),
                        Description = "Registered active suppliers",
                        Tone = "green"
                    },
                    BuildRequestStat(
                        key: "advanceRequests",
                        label: "Advance Requests",
                        statuses: twoMonthCashRows.Select(r => r.Status),
                        tone: "amber"
                    ),
                    BuildRequestStat(
                        key: "fertilizerRequests",
                        label: "Fertilizer Requests",
                        statuses: twoMonthFertilizerRows.Select(r => r.Status),
                        tone: "emerald"
                    ),
                    BuildRequestStat(
                        key: "itemRequests",
                        label: "Item Requests",
                        statuses: twoMonthItemRows.Select(r => r.Status),
                        tone: "sky"
                    )
                },

                MonthlyRequestVolume = BuildMonthlyRequestVolume(
                    currentMonth,
                    months,
                    chartCashRows,
                    chartFertilizerRows,
                    chartItemRows
                ),

                RequestQueue = new List<RequestQueueDto>
                {
                    BuildQueue(
                        "advance",
                        "Advance Requests",
                        twoMonthCashRows.Select(r => r.Status),
                        "/requests?tab=advance"
                    ),
                    BuildQueue(
                        "fertilizer",
                        "Fertilizer Requests",
                        twoMonthFertilizerRows.Select(r => r.Status),
                        "/requests?tab=fertilizer"
                    ),
                    BuildQueue(
                        "items",
                        "Item Requests",
                        twoMonthItemRows.Select(r => r.Status),
                        "/requests?tab=items"
                    )
                },

                RecentActivities = BuildRecentActivities(
                    cashRows,
                    fertilizerRows,
                    itemRows,
                    recent
                )
            };
        }

        private static DashboardStatDto BuildRequestStat(
            string key,
            string label,
            IEnumerable<string?> statuses,
            string tone)
        {
            var statusList = NormalizeStatuses(statuses);
            var pending = statusList.Count(IsPendingStatus);
            var approved = statusList.Count(IsApprovedStatus);
            var rejected = statusList.Count(IsRejectedStatus);
            var total = pending + approved + rejected;

            return new DashboardStatDto
            {
                Key = key,
                Label = label,
                Description = $"Previous + current month | Pending {pending} | Approved {approved} | Rejected {rejected}",
                Tone = tone,
                Pending = pending,
                Approved = approved,
                Rejected = rejected,
                Total = total
            };
        }

        private static List<MonthlyRequestVolumeDto> BuildMonthlyRequestVolume(
            DateTime currentMonth,
            int months,
            List<TeaFactorySupplierManagement.Domain.Entities.CashRequest> cashRows,
            List<TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest> fertilizerRows,
            List<TeaFactorySupplierManagement.Domain.Entities.ItemRequest> itemRows)
        {
            var result = new List<MonthlyRequestVolumeDto>();

            for (var i = months - 1; i >= 0; i--)
            {
                var month = currentMonth.AddMonths(-i);
                var monthKey = ToMonthKey(month);

                var advance = cashRows.Count(r => ToMonthKey(r.RequestMonth) == monthKey);
                var fertilizer = fertilizerRows.Count(r => ToMonthKey(r.RequestMonth) == monthKey);
                var items = itemRows.Count(r => ToMonthKey(r.RequestMonth) == monthKey);

                result.Add(new MonthlyRequestVolumeDto
                {
                    Month = month.ToString("MMM yyyy"),
                    Advance = advance,
                    Fertilizer = fertilizer,
                    Items = items,
                    Total = advance + fertilizer + items
                });
            }

            return result;
        }

        private static RequestQueueDto BuildQueue(
            string key,
            string label,
            IEnumerable<string?> statuses,
            string path)
        {
            var statusList = NormalizeStatuses(statuses);

            return new RequestQueueDto
            {
                Key = key,
                Label = label,
                Pending = statusList.Count(IsPendingStatus),
                Approved = statusList.Count(IsApprovedStatus),
                Rejected = statusList.Count(IsRejectedStatus),
                Total = statusList.Count(status => IsPendingStatus(status) || IsApprovedStatus(status) || IsRejectedStatus(status)),
                Path = path
            };
        }

        private static List<RecentActivityDto> BuildRecentActivities(
            List<TeaFactorySupplierManagement.Domain.Entities.CashRequest> cashRows,
            List<TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest> fertilizerRows,
            List<TeaFactorySupplierManagement.Domain.Entities.ItemRequest> itemRows,
            int recent)
        {
            var activities = new List<RecentActivityDto>();

            activities.AddRange(cashRows.Select(r => new RecentActivityDto
            {
                Id = $"advance-{r.Id}",
                Category = "Advance",
                RequestNo = r.RequestNo,
                RegNo = r.RegNo.ToString(),
                SupplierName = r.Supplier?.RegName ?? string.Empty,
                Description = $"Rs. {r.Amount:N0}",
                Status = r.Status,
                CheckedBy = r.ApprovedBy ?? "-",
                Date = ToDateKey(r.RequestDate),
                ActivityAt = r.UpdatedAt ?? r.CreatedAt
            }));

            activities.AddRange(fertilizerRows.Select(r => new RecentActivityDto
            {
                Id = $"fertilizer-{r.Id}",
                Category = "Fertilizer",
                RequestNo = r.RequestNo,
                RegNo = r.RegNo.ToString(),
                SupplierName = r.Supplier?.RegName ?? string.Empty,
                Description = $"{r.FertilizerType} - {r.Quantity:N0} {r.Unit}",
                Status = r.Status,
                CheckedBy = r.ApprovedBy ?? "-",
                Date = ToDateKey(r.RequestDate),
                ActivityAt = r.UpdatedAt ?? r.CreatedAt
            }));

            activities.AddRange(itemRows.Select(r => new RecentActivityDto
            {
                Id = $"items-{r.Id}",
                Category = "Item",
                RequestNo = r.RequestNo,
                RegNo = r.RegNo.ToString(),
                SupplierName = r.Supplier?.RegName ?? string.Empty,
                Description = $"{r.ItemType} - {r.Quantity:N0} {r.Unit ?? "units"}",
                Status = r.Status,
                CheckedBy = r.ApprovedBy ?? "-",
                Date = ToDateKey(r.RequestDate),
                ActivityAt = r.UpdatedAt ?? r.CreatedAt
            }));

            return activities
                .OrderByDescending(activity => activity.ActivityAt)
                .Take(recent)
                .ToList();
        }

        private static List<string> NormalizeStatuses(IEnumerable<string?> statuses)
        {
            return statuses
                .Where(status => !string.IsNullOrWhiteSpace(status))
                .Select(status => status!.Trim().ToLowerInvariant())
                .ToList();
        }

        private static bool IsPendingStatus(string status)
        {
            return status is "pending" or "new" or "submitted";
        }

        private static bool IsApprovedStatus(string status)
        {
            return status is "approved" or "approve";
        }

        private static bool IsRejectedStatus(string status)
        {
            return status is "rejected" or "reject" or "declined";
        }

        private static string ToMonthKey(DateTime date)
        {
            return date.ToString("yyyy-MM");
        }

        private static string ToDateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}
