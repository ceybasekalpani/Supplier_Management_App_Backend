using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Leaf;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class LeafService : ILeafService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFactorySetupService _factorySetupService;

        public LeafService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<MonthlyLeafSummaryDto> GetMonthlyLeafCollectionAsync(int regNo, int year, int month)
        {
            // DB stores MonthName as "Mar-2026" format
            var monthName = new DateTime(year, month, 1).ToString("MMM-yyyy");
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo
                         && l.MonthName == monthName)
                .OrderBy(l => l.DayNo)
                .ToListAsync();

            // Group all records by day and aggregate — one entry per day regardless of shifts
            var grouped = collections
                .Where(c => (c.DayNo ?? 0) > 0)
                .GroupBy(c => c.DayNo!.Value)
                .OrderBy(g => g.Key);

            var leafData = new List<LeafCollectionDto>();
            decimal totalGross = 0;
            decimal totalNet = 0;
            decimal totalSuperNet = 0;

            foreach (var dayGroup in grouped)
            {
                decimal dayGross = 0;
                decimal dayNormalNet = 0;
                decimal daySuperNet = 0;
                decimal dayWater = 0;
                int dayBags = 0;

                foreach (var c in dayGroup)
                {
                    dayGross += c.Gross ?? 0;
                    dayWater += c.Water ?? 0;
                    dayBags += c.Qty ?? 1;

                    var net = c.NetWeight ?? 0;
                    if (hasSuper && c.LeafType == "Super")
                        daySuperNet += net;
                    else
                        dayNormalNet += net;
                }

                totalGross += dayGross;
                totalNet += dayNormalNet;
                totalSuperNet += daySuperNet;

                leafData.Add(new LeafCollectionDto
                {
                    Day = dayGroup.Key,
                    Gross = dayGross,
                    Bags = dayBags,
                    Water = dayWater,
                    NetWeight = dayNormalNet,
                    SuperNetWeight = hasSuper ? daySuperNet : null
                });
            }

            return new MonthlyLeafSummaryDto
            {
                Month = monthName,
                Year = year,
                TotalGross = totalGross,
                TotalNet = hasSuper ? totalNet : totalNet + totalSuperNet,
                TotalSuperNet = hasSuper ? totalSuperNet : 0,
                TotalDays = leafData.Count,
                Collections = leafData,
                HasSuper = hasSuper
            };
        }

        public async Task<List<SixMonthHistoryDto>> GetSixMonthHistoryAsync(int regNo)
        {
            // Use local server time so month boundaries match the factory's timezone
            var today = DateTime.Now;
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();
            var result = new List<SixMonthHistoryDto>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = today.AddMonths(-i);
                var year = targetDate.Year;
                var month = targetDate.Month;
                var monthName = targetDate.ToString("MMM-yyyy");

                var collections = await _context.TrLeafCollectionsTemp
                    .Where(l => l.RegNo == regNo
                             && l.MonthName == monthName)
                    .ToListAsync();

                decimal totalGross = 0;
                decimal totalNet = 0;
                decimal totalSuperNet = 0;
                var uniqueDays = new HashSet<int>();

                foreach (var c in collections)
                {
                    var day = c.DayNo ?? 0;
                    if (day == 0) continue;

                    uniqueDays.Add(day);
                    totalGross += c.Gross ?? 0;

                    var net = c.NetWeight ?? 0;
                    if (hasSuper && c.LeafType == "Super")
                        totalSuperNet += net;
                    else
                        totalNet += net;
                }

                result.Add(new SixMonthHistoryDto
                {
                    MonthKey = $"{year}-{month:D2}",
                    MonthLabel = targetDate.ToString("MMM yyyy"),
                    TotalGross = totalGross,
                    TotalNet = hasSuper ? totalNet : totalNet + totalSuperNet,
                    TotalSuperNet = hasSuper ? totalSuperNet : 0,
                    Days = uniqueDays.Count
                });
            }

            return result;
        }

        public async Task<TodayLeafDto> GetTodayLeafCollectionAsync(int regNo)
        {
            var today = DateTime.Now.Date;
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo && l.LogTime!.Value.Date == today)
                .ToListAsync();

            decimal normalNet = 0;
            decimal superNet = 0;
            int bagCount = 0;

            foreach (var c in collections)
            {
                bagCount += c.Qty ?? 1;
                var net = c.NetWeight ?? 0;

                if (hasSuper && c.LeafType == "Super")
                    superNet += net;
                else
                    normalNet += net;
            }

            return new TodayLeafDto
            {
                NormalNet = normalNet,
                SuperNet = hasSuper ? superNet : 0,
                HasSuper = hasSuper,
                BagCount = bagCount
            };
        }

        public async Task<decimal> GetCurrentMonthTotalAsync(int regNo)
        {
            var monthName = DateTime.Now.ToString("MMM-yyyy");

            return await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo
                         && l.MonthName == monthName)
                .SumAsync(c => c.NetWeight ?? 0);
        }

        public async Task<object> GetDiagnosticsAsync(int regNo)
        {
            var totalCount = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo)
                .CountAsync();

            var sample = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo)
                .OrderByDescending(l => l.LogTime)
                .Take(5)
                .Select(l => new
                {
                    l.Ind,
                    l.RegNo,
                    l.DayNo,
                    l.MonthName,
                    LogTime = l.LogTime.ToString(),
                    l.Gross,
                    l.NetWeight,
                    l.LeafType,
                    l.Qty
                })
                .ToListAsync();

            var distinctMonths = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo)
                .Select(l => new { l.MonthName, Year = (int?)l.LogTime!.Value.Year })
                .Distinct()
                .OrderByDescending(x => x.Year)
                .Take(10)
                .ToListAsync();

            var allRegNos = await _context.TrLeafCollectionsTemp
                .Select(l => l.RegNo)
                .Distinct()
                .OrderBy(r => r)
                .Take(20)
                .ToListAsync();

            return new
            {
                queriedRegNo = regNo,
                totalRecordsForThisRegNo = totalCount,
                recentSampleRecords = sample,
                distinctMonthsForThisRegNo = distinctMonths,
                allRegNosInTable = allRegNos
            };
        }

        public async Task<Dictionary<int, LeafCollectionDto>> GetLeafDataByMonthAsync(int regNo, int year, int month)
        {
            var monthName = new DateTime(year, month, 1).ToString("MMM-yyyy");
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo
                         && l.MonthName == monthName)
                .ToListAsync();

            var result = new Dictionary<int, LeafCollectionDto>();

            foreach (var c in collections)
            {
                var day = c.DayNo ?? 0;
                if (day == 0) continue;

                var gross = c.Gross ?? 0;
                var water = c.Water ?? 0;
                var net = c.NetWeight ?? 0;
                var bags = c.Qty ?? 1;
                var isSuper = hasSuper && c.LeafType == "Super";

                if (result.TryGetValue(day, out var existing))
                {
                    existing.Gross += gross;
                    existing.Water += water;
                    existing.Bags += bags;
                    if (isSuper)
                        existing.SuperNetWeight = (existing.SuperNetWeight ?? 0) + net;
                    else
                        existing.NetWeight += net;
                }
                else
                {
                    result[day] = new LeafCollectionDto
                    {
                        Day = day,
                        Gross = gross,
                        Bags = bags,
                        Water = water,
                        NetWeight = isSuper ? 0 : net,
                        SuperNetWeight = isSuper ? net : null
                    };
                }
            }

            return result;
        }
    }
}
