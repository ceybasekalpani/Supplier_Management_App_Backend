using System.Globalization;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class CashRequestService : ICashRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFactorySetupService _factorySetupService;

        public CashRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<CashRequestDto>> GetRequestsByRegNoAsync(int regNo)
        {
            var requests = await _context.CashRequests
                .Where(r => r.RegNo == regNo)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<CashRequestDto>>(requests);
        }

        public async Task<CashRequestDto?> GetRequestByIdAsync(int id)
        {
            var request = await _context.CashRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            return request != null ? _mapper.Map<CashRequestDto>(request) : null;
        }

        public async Task<CashRequestDto> CreateRequestAsync(int regNo, CreateCashRequestDto request)
        {
            if (!await IsCashRequestEnabledAsync())
                throw new InvalidOperationException("Cash request feature is not enabled");

            if (IsAdvanceType(request.RequestType))
            {
                var limit = await GetAdvanceLimitAsync(regNo, DateTime.Today);
                if (limit.HasValue && request.Amount > limit.Value)
                    throw new InvalidOperationException(
                        $"Requested amount exceeds your maximum advance limit of Rs. {limit.Value:N2}.");
            }

            var now = DateTime.Now;

            // Auto-generate request number
            var requestNo = await GenerateRequestNumberAsync(request.RequestType);

            var cashRequest = _mapper.Map<CashRequest>(request);
            cashRequest.RegNo = regNo;
            cashRequest.RequestNo = requestNo;

            cashRequest.RequestDate = now.Date;
            cashRequest.RequestTime = now.TimeOfDay;
            cashRequest.Month = !string.IsNullOrWhiteSpace(request.SalaryDate)
                ? request.SalaryDate
                : !string.IsNullOrWhiteSpace(request.Month)
                ? request.Month
                : now.ToString("MMMM yyyy");
            cashRequest.RequestMonth = new DateTime(now.Year, now.Month, 1);
            cashRequest.Status = "pending";

            _context.CashRequests.Add(cashRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<CashRequestDto>(cashRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null)
        {
            var request = await _context.CashRequests.FindAsync(id);
            if (request == null) return false;

            if (status == "approved" && IsAdvanceType(request.RequestType))
            {
                var limit = await GetAdvanceLimitAsync(request.RegNo, DateTime.Today);
                if (limit.HasValue && request.Amount > limit.Value)
                    throw new InvalidOperationException(
                        $"Cannot approve: requested amount exceeds the supplier's maximum advance limit of Rs. {limit.Value:N2}.");
            }

            request.Status = status;
            request.UpdatedAt = DateTime.Now;
            request.Remarks = remarks;

            if (status == "approved" && approvedBy != null)
            {
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.Now;
            }
            else if (status == "paid")
            {
                request.PaidAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var request = await _context.CashRequests.FindAsync(id);
            if (request == null) return false;

            _context.CashRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsCashRequestEnabledAsync()
        {
            var factorySetup = await _context.FactorySetups.FirstOrDefaultAsync();
            return factorySetup?.IsCashRequest ?? true;
        }

        public async Task<decimal?> GetAdvanceLimitAsync(int regNo, DateTime salaryDate)
        {
            var settings = await _context.RegSettings.FirstOrDefaultAsync();
            if (settings == null) return null;

            var supplier = await _context.RegSuppliers.FirstOrDefaultAsync(s => s.RegNo == regNo);
            if (supplier == null) return null;

            var (activeMonth, nextMonth, cutoffDay) = DeriveSalaryPeriod(salaryDate);

            // The advance rates in Reg_Settings are only valid for the period they were configured
            // for. Refuse to apply them to a different salary period rather than silently using
            // stale rates.
            if (settings.ActiveMonth != activeMonth || settings.NextMonth != nextMonth)
                return null;

            var rows = await _context.TrLeafCollections
                .Where(l => l.RegNo == regNo && l.Route == supplier.Route &&
                    ((l.MonthName == activeMonth && l.DayNo >= cutoffDay) ||
                     (l.MonthName == nextMonth && l.DayNo < cutoffDay)))
                .ToListAsync();

            return CalculateAdvanceLimit(rows, activeMonth, nextMonth, settings);
        }

        public async Task<List<SupplierAdvanceLimitDto>> GetAdvanceLimitsForAllSuppliersAsync(DateTime salaryDate)
        {
            var settings = await _context.RegSettings.FirstOrDefaultAsync();
            if (settings == null) return new List<SupplierAdvanceLimitDto>();

            var (activeMonth, nextMonth, cutoffDay) = DeriveSalaryPeriod(salaryDate);

            // The advance rates in Reg_Settings are only valid for the period they were configured
            // for. Refuse to apply them to a different salary period rather than silently using
            // stale rates.
            if (settings.ActiveMonth != activeMonth || settings.NextMonth != nextMonth)
                return new List<SupplierAdvanceLimitDto>();

            // Join to Reg_Supplier to restrict each collection row to the supplier's own route
            // (a RegNo can otherwise appear against more than one route in Tr_LeafCollection).
            var rows = await (
                from l in _context.TrLeafCollections
                join s in _context.RegSuppliers on l.RegNo equals s.RegNo
                where l.RegNo != null && l.Route == s.Route &&
                    ((l.MonthName == activeMonth && l.DayNo >= cutoffDay) ||
                     (l.MonthName == nextMonth && l.DayNo < cutoffDay))
                select l
            ).ToListAsync();

            if (rows.Count == 0) return new List<SupplierAdvanceLimitDto>();

            return rows
                .GroupBy(l => l.RegNo!.Value)
                .Select(g => new SupplierAdvanceLimitDto
                {
                    RegNo = g.Key,
                    SupplierName = g.First().RegName ?? string.Empty,
                    MaxAdvanceLimit = CalculateAdvanceLimit(g, activeMonth, nextMonth, settings)
                })
                .OrderBy(x => x.RegNo)
                .ToList();
        }

        private static (string ActiveMonth, string NextMonth, int CutoffDay) DeriveSalaryPeriod(DateTime salaryDate)
        {
            var activeMonth = salaryDate.ToString("MMM-yyyy", CultureInfo.InvariantCulture);
            var nextMonth = salaryDate.AddMonths(1).ToString("MMM-yyyy", CultureInfo.InvariantCulture);
            return (activeMonth, nextMonth, salaryDate.Day);
        }

        private static decimal CalculateAdvanceLimit(
            IEnumerable<TrLeafCollection> rows, string activeMonth, string nextMonth, RegSettings settings)
        {
            static decimal NetOf(IEnumerable<TrLeafCollection> g) =>
                g.Sum(x => x.GrossQty ?? 0) - g.Sum(x => x.WaterWgt ?? 0) - g.Sum(x => x.BagWgt ?? 0)
                - g.Sum(x => x.CoarseWgt ?? 0) - g.Sum(x => x.BoiledWgt ?? 0) - g.Sum(x => x.RejectedWgt ?? 0);

            var activeRows = rows.Where(l => l.MonthName == activeMonth);
            var nextRows = rows.Where(l => l.MonthName == nextMonth);

            var normalActive = NetOf(activeRows.Where(l => l.LeafType != "Super"));
            var superActive = NetOf(activeRows.Where(l => l.LeafType == "Super"));
            var normalNext = NetOf(nextRows.Where(l => l.LeafType != "Super"));
            var superNext = NetOf(nextRows.Where(l => l.LeafType == "Super"));

            return (settings.ActAdvRate * normalActive) + (settings.ActAdvRate_Super * superActive)
                 + (settings.NxtAdvRate * normalNext) + (settings.NxtAdvRate_Super * superNext);
        }

        private static bool IsAdvanceType(string requestType) =>
            string.Equals(requestType?.Trim(), "ADVANCE", StringComparison.OrdinalIgnoreCase);

        private async Task<string> GenerateRequestNumberAsync(string requestType)
        {
            var prefix = requestType.ToUpper() == "ADVANCE" ? "ADV" : "LOAN";
            var yearMonth = DateTime.Now.ToString("yyyyMM");

            var lastRequest = await _context.CashRequests
                .Where(r => r.RequestNo.StartsWith($"{prefix}-{yearMonth}"))
                .OrderByDescending(r => r.RequestNo)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var parts = lastRequest.RequestNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                    sequence = lastSeq + 1;
            }

            return $"{prefix}-{yearMonth}-{sequence:D3}";
        }
    }
}
