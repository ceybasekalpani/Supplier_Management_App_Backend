using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardRequestService : IDashboardRequestService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending",
            "approved",
            "rejected"
        };

        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;
        private readonly ILogger<DashboardRequestService> _logger;

        public DashboardRequestService(
            ApplicationDbContext context,
            IFirebasePushService firebasePushService,
            ILogger<DashboardRequestService> logger)
        {
            _context = context;
            _firebasePushService = firebasePushService;
            _logger = logger;
        }

        public async Task<DashboardRequestsResponseDto> GetRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var normalizedStatus = NormalizeOptionalStatus(status);
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            return new DashboardRequestsResponseDto
            {
                Advance = await GetAdvanceRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate),
                Fertilizer = await GetFertilizerRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate),
                Items = await GetItemRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate)
            };
        }

        public async Task<DashboardRequestRowDto?> UpdateRequestStatusAsync(string requestType, int id, string status, string? remarks, string? reviewedBy)
        {
            var normalizedType = NormalizeRequestType(requestType);
            var normalizedStatus = NormalizeStatus(status);
            var reviewer = string.IsNullOrWhiteSpace(reviewedBy) ? "Dashboard Admin" : reviewedBy.Trim();
            var now = DateTime.Now;

            if (normalizedType == "advance")
            {
                var request = await _context.CashRequests
                    .Include(r => r.Supplier)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    return null;
                }

                request.Status = normalizedStatus;
                request.Remarks = remarks;
                request.ApprovedBy = reviewer;
                request.ApprovedAt = now;
                request.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await SendRequestStatusPushAsync(request.RegNo, normalizedType, request.Id, normalizedStatus);
                return ToAdvanceRow(request);
            }

            if (normalizedType == "fertilizer")
            {
                var request = await _context.FertilizerRequests
                    .Include(r => r.Supplier)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    return null;
                }

                request.Status = normalizedStatus;
                request.Remarks = remarks;
                request.ApprovedBy = reviewer;
                request.ApprovedAt = now;
                request.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await SendRequestStatusPushAsync(request.RegNo, normalizedType, request.Id, normalizedStatus);
                return ToFertilizerRow(request);
            }

            var itemRequest = await _context.ItemRequests
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (itemRequest == null)
            {
                return null;
            }

            itemRequest.Status = normalizedStatus;
            itemRequest.Remarks = remarks;
            itemRequest.ApprovedBy = reviewer;
            itemRequest.ApprovedAt = now;
            itemRequest.UpdatedAt = now;

            await _context.SaveChangesAsync();
            await SendRequestStatusPushAsync(itemRequest.RegNo, normalizedType, itemRequest.Id, normalizedStatus);
            return ToItemRow(itemRequest);
        }

        private async Task SendRequestStatusPushAsync(int regNo, string requestType, int requestId, string status)
        {
            try
            {
                await _firebasePushService.SendRequestStatusChangedAsync(regNo, requestType, requestId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Request status was saved, but Firebase push failed. RegNo={RegNo}, RequestType={RequestType}, RequestId={RequestId}, Status={Status}.",
                    regNo,
                    requestType,
                    requestId,
                    status);
            }
        }

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
