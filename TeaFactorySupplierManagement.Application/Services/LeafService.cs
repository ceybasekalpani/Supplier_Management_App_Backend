using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Leaf;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
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
            var monthName = new DateTime(year, month, 1).ToString("MMMM");
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo && l.MonthName == monthName && l.LogTime!.Value.Year == year)
                .OrderBy(l => l.DayNo)
                .ToListAsync();

            var leafData = new List<LeafCollectionDto>();
            decimal totalGross = 0;
            decimal totalNet = 0;
            decimal totalSuperNet = 0;
            var daysWithData = new HashSet<int>();

            foreach (var collection in collections)
            {
                var day = collection.DayNo ?? 0;
                if (day == 0) continue;

                daysWithData.Add(day);

                var gross = collection.Gross ?? 0;
                var water = collection.Water ?? 0;
                var netWeight = collection.NetWeight ?? 0;

                totalGross += gross;

                if (collection.LeafType == "Super" && hasSuper)
                {
                    totalSuperNet += netWeight;
                    leafData.Add(new LeafCollectionDto
                    {
                        Day = day,
                        Gross = gross,
                        Bags = 1,
                        Water = water,
                        NetWeight = 0,
                        SuperNetWeight = netWeight
                    });
                }
                else
                {
                    totalNet += netWeight;
                    leafData.Add(new LeafCollectionDto
                    {
                        Day = day,
                        Gross = gross,
                        Bags = 1,
                        Water = water,
                        NetWeight = netWeight,
                        SuperNetWeight = null
                    });
                }
            }

            return new MonthlyLeafSummaryDto
            {
                Month = monthName,
                Year = year,
                TotalGross = totalGross,
                TotalNet = hasSuper ? totalNet : totalNet + totalSuperNet,
                TotalSuperNet = hasSuper ? totalSuperNet : 0,
                TotalDays = daysWithData.Count,
                Collections = leafData,
                HasSuper = hasSuper
            };
        }

        public async Task<List<SixMonthHistoryDto>> GetSixMonthHistoryAsync(int regNo)
        {
            var today = DateTime.UtcNow;
            var sixMonthsAgo = today.AddMonths(-5);
            var result = new List<SixMonthHistoryDto>();

            for (int i = 0; i < 6; i++)
            {
                var targetDate = sixMonthsAgo.AddMonths(i);
                var year = targetDate.Year;
                var month = targetDate.Month;
                var monthName = targetDate.ToString("MMMM");

                var collections = await _context.TrLeafCollectionsTemp
                    .Where(l => l.RegNo == regNo &&
                                l.MonthName == monthName &&
                                l.LogTime!.Value.Year == year)
                    .ToListAsync();

                decimal totalGross = 0;
                decimal totalNet = 0;
                var daysWithData = new HashSet<int>();

                foreach (var collection in collections)
                {
                    var day = collection.DayNo ?? 0;
                    if (day > 0)
                    {
                        daysWithData.Add(day);
                        totalGross += collection.Gross ?? 0;
                        totalNet += collection.NetWeight ?? 0;
                    }
                }

                result.Add(new SixMonthHistoryDto
                {
                    MonthKey = $"{year}-{month:D2}",
                    MonthLabel = targetDate.ToString("MMM yyyy"),
                    TotalGross = totalGross,
                    TotalNet = totalNet,
                    TotalSuperNet = 0,
                    Days = daysWithData.Count
                });
            }

            return result;
        }

        public async Task<TodayLeafDto> GetTodayLeafCollectionAsync(int regNo)
        {
            var today = DateTime.UtcNow.Date;
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo && l.LogTime!.Value.Date == today)
                .ToListAsync();

            decimal normalNet = 0;
            decimal superNet = 0;
            int bagCount = 0;

            foreach (var collection in collections)
            {
                var netWeight = collection.NetWeight ?? 0;
                bagCount++;

                if (collection.LeafType == "Super" && hasSuper)
                {
                    superNet += netWeight;
                }
                else
                {
                    normalNet += netWeight;
                }
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
            var today = DateTime.UtcNow;
            var monthName = today.ToString("MMMM");

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo && l.MonthName == monthName)
                .ToListAsync();

            return collections.Sum(c => c.NetWeight ?? 0);
        }

        public async Task<Dictionary<int, LeafCollectionDto>> GetLeafDataByMonthAsync(int regNo, int year, int month)
        {
            var monthName = new DateTime(year, month, 1).ToString("MMMM");
            var hasSuper = await _factorySetupService.IsSuperLeafEnabledAsync();

            var collections = await _context.TrLeafCollectionsTemp
                .Where(l => l.RegNo == regNo && l.MonthName == monthName && l.LogTime!.Value.Year == year)
                .ToListAsync();

            var result = new Dictionary<int, LeafCollectionDto>();

            foreach (var collection in collections)
            {
                var day = collection.DayNo ?? 0;
                if (day == 0) continue;

                var gross = collection.Gross ?? 0;
                var water = collection.Water ?? 0;
                var netWeight = collection.NetWeight ?? 0;

                result[day] = new LeafCollectionDto
                {
                    Day = day,
                    Gross = gross,
                    Bags = 1,
                    Water = water,
                    NetWeight = collection.LeafType == "Super" && hasSuper ? 0 : netWeight,
                    SuperNetWeight = collection.LeafType == "Super" && hasSuper ? netWeight : null
                };
            }

            return result;
        }
    }
}