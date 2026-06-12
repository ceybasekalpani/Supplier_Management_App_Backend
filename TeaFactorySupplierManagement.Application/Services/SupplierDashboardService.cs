using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class SupplierDashboardService : ISupplierDashboardService
    {
        private readonly ApplicationDbContext _context;

        public SupplierDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SupplierDashboardResponseDto> GetSuppliersAsync(string? search, int months, bool activeOnly)
        {
            months = Math.Clamp(months, 1, 24);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fromMonth = currentMonth.AddMonths(-(months - 1));
            var previousMonth = currentMonth.AddMonths(-1);

            var suppliersQuery = _context.RegSuppliers.AsNoTracking();

            if (activeOnly)
            {
                suppliersQuery = suppliersQuery.Where(s => s.Active == true);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                suppliersQuery = suppliersQuery.Where(s =>
                    s.RegNo.ToString().Contains(term) ||
                    (s.RegName != null && s.RegName.Contains(term)) ||
                    (s.Route != null && s.Route.Contains(term)));
            }

            var suppliers = await suppliersQuery
                .OrderBy(s => s.RegNo)
                .Select(s => new DashboardSupplierDto
                {
                    Id = s.RegNo,
                    RegNo = s.RegNo.ToString(),
                    Name = s.RegName ?? string.Empty,
                    Route = s.Route ?? string.Empty,
                    Phone = s.TelNo ?? string.Empty,
                    Phone2 = s.TelNo_2,
                    Phone3 = s.TelNo_3,
                    Address = s.Address ?? string.Empty,
                    Bank = s.BankName ?? s.Bank ?? s.CustBank ?? string.Empty,
                    Branch = s.BranchCode ?? string.Empty,
                    Payment = s.PayMode ?? s.PayMode_Temp ?? string.Empty,
                    Status = s.Active == true ? "active" : "inactive"
                })
                .ToListAsync();

            await AttachRecentRequestsAsync(suppliers, fromMonth);

            return new SupplierDashboardResponseDto
            {
                Suppliers = suppliers,
                ActiveMonth = ToMonthKey(currentMonth),
                PreviousMonth = ToMonthKey(previousMonth)
            };
        }

        public async Task<DashboardSupplierDto?> GetSupplierAsync(int regNo, int months)
        {
            months = Math.Clamp(months, 1, 24);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var fromMonth = currentMonth.AddMonths(-(months - 1));

            var supplier = await _context.RegSuppliers
                .AsNoTracking()
                .Where(s => s.RegNo == regNo)
                .Select(s => new DashboardSupplierDto
                {
                    Id = s.RegNo,
                    RegNo = s.RegNo.ToString(),
                    Name = s.RegName ?? string.Empty,
                    Route = s.Route ?? string.Empty,
                    Phone = s.TelNo ?? string.Empty,
                    Phone2 = s.TelNo_2,
                    Phone3 = s.TelNo_3,
                    Address = s.Address ?? string.Empty,
                    Bank = s.BankName ?? s.Bank ?? s.CustBank ?? string.Empty,
                    Branch = s.BranchCode ?? string.Empty,
                    Payment = s.PayMode ?? s.PayMode_Temp ?? string.Empty,
                    Status = s.Active == true ? "active" : "inactive"
                })
                .FirstOrDefaultAsync();

            if (supplier == null)
            {
                return null;
            }

            await AttachRecentRequestsAsync(new List<DashboardSupplierDto> { supplier }, fromMonth);
            return supplier;
        }

        private async Task AttachRecentRequestsAsync(List<DashboardSupplierDto> suppliers, DateTime fromMonth)
        {
            if (suppliers.Count == 0)
            {
                return;
            }

            var regNos = suppliers.Select(s => int.Parse(s.RegNo)).ToList();
            var supplierMap = suppliers.ToDictionary(s => int.Parse(s.RegNo));

            var cashRows = await _context.CashRequests
                .AsNoTracking()
                .Where(r => regNos.Contains(r.RegNo) && r.RequestMonth >= fromMonth)
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            var cashRequests = cashRows
                .Select(r => new DashboardCashRequestDto
                {
                    Id = r.Id,
                    RequestNo = r.RequestNo,
                    RegNo = r.RegNo.ToString(),
                    Type = r.RequestType,
                    Amount = r.Amount,
                    Month = r.Month,
                    Date = ToDateKey(r.RequestDate),
                    Status = r.Status,
                    CheckedBy = r.ApprovedBy ?? "-",
                    Remarks = r.Remarks
                })
                .ToList();

            var fertilizerRows = await _context.FertilizerRequests
                .AsNoTracking()
                .Where(r => regNos.Contains(r.RegNo) && r.RequestMonth >= fromMonth)
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            var fertilizerRequests = fertilizerRows
                .Select(r => new DashboardFertilizerRequestDto
                {
                    Id = r.Id,
                    RequestNo = r.RequestNo,
                    RegNo = r.RegNo.ToString(),
                    Type = r.FertilizerType,
                    Qty = r.Quantity,
                    Unit = r.Unit,
                    Month = r.Month,
                    Date = ToDateKey(r.RequestDate),
                    Status = r.Status,
                    CheckedBy = r.ApprovedBy ?? "-",
                    Remarks = r.Remarks
                })
                .ToList();

            var itemRows = await _context.ItemRequests
                .AsNoTracking()
                .Where(r => regNos.Contains(r.RegNo) && r.RequestMonth >= fromMonth)
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            var itemRequests = itemRows
                .Select(r => new DashboardItemRequestDto
                {
                    Id = r.Id,
                    RequestNo = r.RequestNo,
                    RegNo = r.RegNo.ToString(),
                    Type = r.ItemType,
                    Qty = r.Quantity,
                    Unit = r.Unit ?? string.Empty,
                    Month = r.Month,
                    Date = ToDateKey(r.RequestDate),
                    Status = r.Status,
                    CheckedBy = r.ApprovedBy ?? "-",
                    Remarks = r.Remarks
                })
                .ToList();

            foreach (var request in cashRequests)
            {
                if (supplierMap.TryGetValue(int.Parse(request.RegNo), out var supplier))
                {
                    supplier.AdvanceRequests.Add(request);
                }
            }

            foreach (var request in fertilizerRequests)
            {
                if (supplierMap.TryGetValue(int.Parse(request.RegNo), out var supplier))
                {
                    supplier.FertilizerRequests.Add(request);
                }
            }

            foreach (var request in itemRequests)
            {
                if (supplierMap.TryGetValue(int.Parse(request.RegNo), out var supplier))
                {
                    supplier.ItemRequests.Add(request);
                }
            }
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
