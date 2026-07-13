using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DashboardRequestService
    {
        private async Task<List<DashboardRequestRowDto>> GetAdvanceRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.CashRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            query = ApplyCommonFilters(query, search, fromDate, toDate);

            var rows = await query
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            return rows.Select(ToAdvanceRow).ToList();
        }

        private async Task<List<DashboardRequestRowDto>> GetFertilizerRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FertilizerRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            query = ApplyCommonFilters(query, search, fromDate, toDate);

            var rows = await query
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            return rows.Select(ToFertilizerRow).ToList();
        }

        private async Task<List<DashboardRequestRowDto>> GetItemRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.ItemRequests
                .AsNoTracking()
                .Include(r => r.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            query = ApplyCommonFilters(query, search, fromDate, toDate);

            var rows = await query
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.RequestTime)
                .ToListAsync();

            return rows.Select(ToItemRow).ToList();
        }

        private static IQueryable<T> ApplyCommonFilters<T>(IQueryable<T> query, string? search, DateTime? fromDate, DateTime? toDate)
            where T : class
        {
            if (typeof(T) == typeof(TeaFactorySupplierManagement.Domain.Entities.CashRequest))
            {
                var typed = (IQueryable<TeaFactorySupplierManagement.Domain.Entities.CashRequest>)query;
                typed = ApplyCashFilters(typed, search, fromDate, toDate);
                return (IQueryable<T>)typed;
            }

            if (typeof(T) == typeof(TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest))
            {
                var typed = (IQueryable<TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest>)query;
                typed = ApplyFertilizerFilters(typed, search, fromDate, toDate);
                return (IQueryable<T>)typed;
            }

            var itemTyped = (IQueryable<TeaFactorySupplierManagement.Domain.Entities.ItemRequest>)query;
            itemTyped = ApplyItemFilters(itemTyped, search, fromDate, toDate);
            return (IQueryable<T>)itemTyped;
        }

        private static IQueryable<TeaFactorySupplierManagement.Domain.Entities.CashRequest> ApplyCashFilters(
            IQueryable<TeaFactorySupplierManagement.Domain.Entities.CashRequest> query,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.RegNo.ToString().Contains(search) ||
                    (r.Supplier != null && r.Supplier.RegName != null && r.Supplier.RegName.Contains(search)));
            }

            return query;
        }

        private static IQueryable<TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest> ApplyFertilizerFilters(
            IQueryable<TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest> query,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.RegNo.ToString().Contains(search) ||
                    r.FertilizerType.Contains(search) ||
                    (r.Supplier != null && r.Supplier.RegName != null && r.Supplier.RegName.Contains(search)));
            }

            return query;
        }

        private static IQueryable<TeaFactorySupplierManagement.Domain.Entities.ItemRequest> ApplyItemFilters(
            IQueryable<TeaFactorySupplierManagement.Domain.Entities.ItemRequest> query,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.RegNo.ToString().Contains(search) ||
                    r.ItemType.Contains(search) ||
                    (r.Supplier != null && r.Supplier.RegName != null && r.Supplier.RegName.Contains(search)));
            }

            return query;
        }

        private static DashboardRequestRowDto ToAdvanceRow(TeaFactorySupplierManagement.Domain.Entities.CashRequest request)
        {
            return new DashboardRequestRowDto
            {
                Id = request.Id,
                RequestNo = request.RequestNo,
                RegNo = request.RegNo.ToString(),
                Name = request.Supplier?.RegName ?? string.Empty,
                Type = request.RequestType,
                Amount = request.Amount,
                Month = request.Month,
                SalaryDate = request.Month,
                Date = ToDateKey(request.RequestDate),
                Status = request.Status,
                CheckedBy = request.ApprovedBy ?? "-",
                Remarks = request.Remarks ?? string.Empty,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

        private static DashboardRequestRowDto ToFertilizerRow(TeaFactorySupplierManagement.Domain.Entities.FertilizerRequest request)
        {
            return new DashboardRequestRowDto
            {
                Id = request.Id,
                RequestNo = request.RequestNo,
                RegNo = request.RegNo.ToString(),
                Name = request.Supplier?.RegName ?? string.Empty,
                Type = request.FertilizerType,
                Qty = request.Quantity,
                Unit = request.Unit,
                Month = request.Month,
                SalaryDate = request.Month,
                Date = ToDateKey(request.RequestDate),
                Status = request.Status,
                CheckedBy = request.ApprovedBy ?? "-",
                Remarks = request.Remarks ?? string.Empty,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

        private static DashboardRequestRowDto ToItemRow(TeaFactorySupplierManagement.Domain.Entities.ItemRequest request)
        {
            return new DashboardRequestRowDto
            {
                Id = request.Id,
                RequestNo = request.RequestNo,
                RegNo = request.RegNo.ToString(),
                Name = request.Supplier?.RegName ?? string.Empty,
                Type = request.ItemType,
                Qty = request.Quantity,
                Unit = request.Unit ?? "units",
                Month = request.Month,
                SalaryDate = request.Month,
                Date = ToDateKey(request.RequestDate),
                Status = request.Status,
                CheckedBy = request.ApprovedBy ?? "-",
                Remarks = request.Remarks ?? string.Empty,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

        private static string NormalizeRequestType(string requestType)
        {
            var normalized = requestType.Trim().ToLowerInvariant();

            return normalized switch
            {
                "advance" or "cash" => "advance",
                "fertilizer" => "fertilizer",
                "item" or "items" => "items",
                _ => throw new ArgumentException("Invalid request type")
            };
        }

        private static string ToDisplayRequestType(string requestType)
        {
            return requestType switch
            {
                "advance" => "Advance",
                "fertilizer" => "Fertilizer",
                "items" => "Item",
                _ => "Request"
            };
        }

        private static string ToDisplayStatus(string status)
        {
            return status switch
            {
                "approved" => "Approved",
                "rejected" => "Rejected",
                _ => "Updated"
            };
        }

        private static string NormalizeStatus(string status)
        {
            var normalized = status.Trim().ToLowerInvariant();
            if (!AllowedStatuses.Contains(normalized))
            {
                throw new ArgumentException("Status must be pending, approved, or rejected");
            }

            return normalized;
        }

        private static string? NormalizeOptionalStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return NormalizeStatus(status);
        }

        private static string ToDateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}
