using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DisbursementService : IDisbursementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;
        private readonly ILogger<DisbursementService> _logger;

        public DisbursementService(
            ApplicationDbContext context,
            IFirebasePushService firebasePushService,
            ILogger<DisbursementService> logger)
        {
            _context = context;
            _firebasePushService = firebasePushService;
            _logger = logger;
        }

        public async Task<DisbursementQueueDto> GetApprovedQueueAsync(string? route, DateTime? fromDate, DateTime? toDate)
        {
            var normalizedRoute = string.IsNullOrWhiteSpace(route) || route.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? null
                : route.Trim();

            var disbursements = await _context.DisbursementRecords
                .AsNoTracking()
                .ToListAsync();

            return new DisbursementQueueDto
            {
                Advances = await GetApprovedAdvancesAsync(normalizedRoute, fromDate, toDate, disbursements),
                Fertilizers = await GetApprovedFertilizersAsync(normalizedRoute, fromDate, toDate, disbursements),
                Items = await GetApprovedItemsAsync(normalizedRoute, fromDate, toDate, disbursements)
            };
        }

        public async Task<DisbursementTrackingDto?> IssueAsync(string issuedType, int requestId, string? method, string? issuedBy)
        {
            var normalizedType = NormalizeIssuedType(issuedType);
            var now = DateTime.Now;
            var cleanMethod = normalizedType == "advance"
                ? NormalizeMethod(method)
                : "Physical Delivery";
            var cleanIssuedBy = string.IsNullOrWhiteSpace(issuedBy) ? "Dashboard Admin" : issuedBy.Trim();

            var existing = await _context.DisbursementRecords
                .FirstOrDefaultAsync(x => x.IssuedType == normalizedType && x.RequestId == requestId);

            if (existing != null)
            {
                return ToTrackingDto(existing);
            }

            if (normalizedType == "advance")
            {
                var request = await _context.CashRequests
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

                if (request == null)
                {
                    return null;
                }

                request.PaidAt = now;
                request.UpdatedAt = now;

                var record = new DisbursementRecord
                {
                    IssuedType = "advance",
                    RequestId = request.Id,
                    RequestNo = request.RequestNo,
                    RegNo = request.RegNo,
                    SupplierName = request.Supplier?.RegName ?? string.Empty,
                    Route = request.Supplier?.Route ?? string.Empty,
                    IssuedDetails = $"Advance - Rs. {request.Amount:N0}",
                    Amount = request.Amount,
                    RequestDate = request.RequestDate,
                    ApprovedDate = request.ApprovedAt,
                    IssueDate = now.Date,
                    Method = cleanMethod,
                    CurrentStatus = "awaiting",
                    IssuedBy = cleanIssuedBy,
                    CreatedAt = now
                };

                _context.DisbursementRecords.Add(record);
                await _context.SaveChangesAsync();
                await SendDisbursementStatusPushAsync(request.RegNo, normalizedType, request.Id, "issued");
                return ToTrackingDto(record);
            }

            if (normalizedType == "fertilizer")
            {
                var request = await _context.FertilizerRequests
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

                if (request == null)
                {
                    return null;
                }

                request.DispatchedAt = now;
                request.UpdatedAt = now;

                var record = new DisbursementRecord
                {
                    IssuedType = "fertilizer",
                    RequestId = request.Id,
                    RequestNo = request.RequestNo,
                    RegNo = request.RegNo,
                    SupplierName = request.Supplier?.RegName ?? string.Empty,
                    Route = request.Supplier?.Route ?? string.Empty,
                    IssuedDetails = $"{request.FertilizerType} - {request.Quantity:N0} {request.Unit}",
                    ItemName = request.FertilizerType,
                    Quantity = request.Quantity,
                    Unit = request.Unit,
                    RequestDate = request.RequestDate,
                    ApprovedDate = request.ApprovedAt,
                    IssueDate = now.Date,
                    Method = cleanMethod,
                    CurrentStatus = "awaiting",
                    IssuedBy = cleanIssuedBy,
                    CreatedAt = now
                };

                _context.DisbursementRecords.Add(record);
                await _context.SaveChangesAsync();
                await SendDisbursementStatusPushAsync(request.RegNo, normalizedType, request.Id, "issued");
                return ToTrackingDto(record);
            }

            var itemRequest = await _context.ItemRequests
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

            if (itemRequest == null)
            {
                return null;
            }

            itemRequest.IssuedAt = now;
            itemRequest.UpdatedAt = now;

            var itemRecord = new DisbursementRecord
            {
                IssuedType = "items",
                RequestId = itemRequest.Id,
                RequestNo = itemRequest.RequestNo,
                RegNo = itemRequest.RegNo,
                SupplierName = itemRequest.Supplier?.RegName ?? string.Empty,
                Route = itemRequest.Supplier?.Route ?? string.Empty,
                IssuedDetails = $"{itemRequest.ItemType} - {itemRequest.Quantity:N0} {itemRequest.Unit ?? "units"}",
                ItemName = itemRequest.ItemType,
                Quantity = itemRequest.Quantity,
                Unit = itemRequest.Unit ?? "units",
                RequestDate = itemRequest.RequestDate,
                ApprovedDate = itemRequest.ApprovedAt,
                IssueDate = now.Date,
                Method = cleanMethod,
                CurrentStatus = "awaiting",
                IssuedBy = cleanIssuedBy,
                CreatedAt = now
            };

            _context.DisbursementRecords.Add(itemRecord);
            await _context.SaveChangesAsync();
            await SendDisbursementStatusPushAsync(itemRequest.RegNo, normalizedType, itemRequest.Id, "issued");
            return ToTrackingDto(itemRecord);
        }

        private async Task SendDisbursementStatusPushAsync(int regNo, string requestType, int requestId, string status)
        {
            try
            {
                await _firebasePushService.SendDisbursementStatusChangedAsync(regNo, requestType, requestId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Disbursement was saved, but Firebase push failed. RegNo={RegNo}, RequestType={RequestType}, RequestId={RequestId}, Status={Status}.",
                    regNo,
                    requestType,
                    requestId,
                    status);
            }
        }

        public async Task<List<DisbursementTrackingDto>> GetTrackingAsync(string? issuedType, string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.DisbursementRecords.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(issuedType) && !issuedType.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedType = NormalizeIssuedType(issuedType);
                query = query.Where(x => x.IssuedType == normalizedType);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedStatus = NormalizeTrackingStatus(status);
                query = query.Where(x => x.CurrentStatus == normalizedStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.IssueDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.IssueDate <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                query = query.Where(x =>
                    x.RegNo.ToString().Contains(cleanSearch) ||
                    x.SupplierName.Contains(cleanSearch) ||
                    x.IssuedDetails.Contains(cleanSearch));
            }

            var rows = await query
                .OrderByDescending(x => x.IssueDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return rows.Select(ToTrackingDto).ToList();
        }

        public async Task<DisbursementTrackingDto?> MarkReceivedAsync(int id, string? completedBy, string? completedDevice)
        {
            var record = await _context.DisbursementRecords.FindAsync(id);
            if (record == null)
            {
                return null;
            }

            record.CurrentStatus = "completed";
            record.CompletedDate = DateTime.Now.Date;
            record.CompletedBy = string.IsNullOrWhiteSpace(completedBy) ? "Dashboard Admin" : completedBy.Trim();
            record.CompletedDevice = string.IsNullOrWhiteSpace(completedDevice) ? null : completedDevice.Trim();
            record.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return ToTrackingDto(record);
        }

        private async Task<List<DisbursementQueueRowDto>> GetApprovedAdvancesAsync(string? route, DateTime? fromDate, DateTime? toDate, List<DisbursementRecord> records)
        {
            var query = _context.CashRequests
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.Status == "approved");

            query = ApplyQueueFilters(query, route, fromDate, toDate);
            var rows = await query.OrderByDescending(x => x.ApprovedAt ?? x.RequestDate).ToListAsync();

            return rows.Select(x =>
            {
                var record = records.FirstOrDefault(r => r.IssuedType == "advance" && r.RequestId == x.Id);
                return new DisbursementQueueRowDto
                {
                    Id = x.Id,
                    RequestNo = x.RequestNo,
                    RegNo = x.RegNo.ToString(),
                    SupplierName = x.Supplier?.RegName ?? string.Empty,
                    Route = x.Supplier?.Route ?? string.Empty,
                    ApprovedDate = ToDateKey(x.ApprovedAt ?? x.RequestDate),
                    ApprovedAmount = x.Amount,
                    Issued = record != null,
                    PaymentMethod = record?.Method,
                    TrackingStatus = record?.CurrentStatus
                };
            }).ToList();
        }

        private async Task<List<DisbursementQueueRowDto>> GetApprovedFertilizersAsync(string? route, DateTime? fromDate, DateTime? toDate, List<DisbursementRecord> records)
        {
            var query = _context.FertilizerRequests
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.Status == "approved");

            query = ApplyQueueFilters(query, route, fromDate, toDate);
            var rows = await query.OrderByDescending(x => x.ApprovedAt ?? x.RequestDate).ToListAsync();

            return rows.Select(x =>
            {
                var record = records.FirstOrDefault(r => r.IssuedType == "fertilizer" && r.RequestId == x.Id);
                return new DisbursementQueueRowDto
                {
                    Id = x.Id,
                    RequestNo = x.RequestNo,
                    RegNo = x.RegNo.ToString(),
                    SupplierName = x.Supplier?.RegName ?? string.Empty,
                    Route = x.Supplier?.Route ?? string.Empty,
                    ApprovedDate = ToDateKey(x.ApprovedAt ?? x.RequestDate),
                    FertilizerType = x.FertilizerType,
                    ApprovedQty = x.Quantity,
                    Unit = x.Unit,
                    Issued = record != null,
                    PaymentMethod = record?.Method,
                    TrackingStatus = record?.CurrentStatus
                };
            }).ToList();
        }

        private async Task<List<DisbursementQueueRowDto>> GetApprovedItemsAsync(string? route, DateTime? fromDate, DateTime? toDate, List<DisbursementRecord> records)
        {
            var query = _context.ItemRequests
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Where(x => x.Status == "approved");

            query = ApplyQueueFilters(query, route, fromDate, toDate);
            var rows = await query.OrderByDescending(x => x.ApprovedAt ?? x.RequestDate).ToListAsync();

            return rows.Select(x =>
            {
                var record = records.FirstOrDefault(r => r.IssuedType == "items" && r.RequestId == x.Id);
                return new DisbursementQueueRowDto
                {
                    Id = x.Id,
                    RequestNo = x.RequestNo,
                    RegNo = x.RegNo.ToString(),
                    SupplierName = x.Supplier?.RegName ?? string.Empty,
                    Route = x.Supplier?.Route ?? string.Empty,
                    ApprovedDate = ToDateKey(x.ApprovedAt ?? x.RequestDate),
                    ItemType = x.ItemType,
                    ApprovedQty = x.Quantity,
                    Unit = x.Unit ?? "units",
                    Issued = record != null,
                    PaymentMethod = record?.Method,
                    TrackingStatus = record?.CurrentStatus
                };
            }).ToList();
        }

        private static IQueryable<T> ApplyQueueFilters<T>(IQueryable<T> query, string? route, DateTime? fromDate, DateTime? toDate)
            where T : class
        {
            if (typeof(T) == typeof(CashRequest))
            {
                var typed = (IQueryable<CashRequest>)query;
                typed = ApplyCashQueueFilters(typed, route, fromDate, toDate);
                return (IQueryable<T>)typed;
            }

            if (typeof(T) == typeof(FertilizerRequest))
            {
                var typed = (IQueryable<FertilizerRequest>)query;
                typed = ApplyFertilizerQueueFilters(typed, route, fromDate, toDate);
                return (IQueryable<T>)typed;
            }

            var itemTyped = (IQueryable<ItemRequest>)query;
            itemTyped = ApplyItemQueueFilters(itemTyped, route, fromDate, toDate);
            return (IQueryable<T>)itemTyped;
        }

        private static IQueryable<CashRequest> ApplyCashQueueFilters(IQueryable<CashRequest> query, string? route, DateTime? fromDate, DateTime? toDate)
        {
            if (!string.IsNullOrWhiteSpace(route))
            {
                query = query.Where(x => x.Supplier != null && x.Supplier.Route == route);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) <= toDate.Value.Date);
            }

            return query;
        }

        private static IQueryable<FertilizerRequest> ApplyFertilizerQueueFilters(IQueryable<FertilizerRequest> query, string? route, DateTime? fromDate, DateTime? toDate)
        {
            if (!string.IsNullOrWhiteSpace(route))
            {
                query = query.Where(x => x.Supplier != null && x.Supplier.Route == route);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) <= toDate.Value.Date);
            }

            return query;
        }

        private static IQueryable<ItemRequest> ApplyItemQueueFilters(IQueryable<ItemRequest> query, string? route, DateTime? fromDate, DateTime? toDate)
        {
            if (!string.IsNullOrWhiteSpace(route))
            {
                query = query.Where(x => x.Supplier != null && x.Supplier.Route == route);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => (x.ApprovedAt ?? x.RequestDate) <= toDate.Value.Date);
            }

            return query;
        }

        private static DisbursementTrackingDto ToTrackingDto(DisbursementRecord record)
        {
            return new DisbursementTrackingDto
            {
                Id = record.Id,
                IssuedType = record.IssuedType,
                RequestId = record.RequestId,
                RequestNo = record.RequestNo,
                RegNo = record.RegNo.ToString(),
                SupplierName = record.SupplierName,
                Route = record.Route,
                IssuedDetails = record.IssuedDetails,
                ItemName = record.ItemName,
                Amount = record.Amount,
                Qty = record.Quantity,
                Unit = record.Unit ?? string.Empty,
                RequestDate = ToDateKey(record.RequestDate),
                ApprovedDate = record.ApprovedDate.HasValue ? ToDateKey(record.ApprovedDate.Value) : string.Empty,
                IssueDate = ToDateKey(record.IssueDate),
                Method = record.Method,
                CurrentStatus = record.CurrentStatus,
                CompletedDate = record.CompletedDate.HasValue ? ToDateKey(record.CompletedDate.Value) : null,
                CompletedBy = record.CompletedBy,
                CompletedDevice = record.CompletedDevice
            };
        }

        private static string NormalizeIssuedType(string issuedType)
        {
            var normalized = issuedType.Trim().ToLowerInvariant();
            return normalized switch
            {
                "advance" or "cash" => "advance",
                "fertilizer" => "fertilizer",
                "item" or "items" => "items",
                _ => throw new ArgumentException("Invalid disbursement type.")
            };
        }

        private static string NormalizeTrackingStatus(string status)
        {
            var normalized = status.Trim().ToLowerInvariant();
            return normalized switch
            {
                "awaiting" or "completed" => normalized,
                _ => throw new ArgumentException("Status must be awaiting or completed.")
            };
        }

        private static string NormalizeMethod(string? method)
        {
            var cleanMethod = method?.Trim();
            if (string.IsNullOrWhiteSpace(cleanMethod))
            {
                throw new ArgumentException("Payment method is required for advance disbursement.");
            }

            return cleanMethod;
        }

        private static string ToDateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
    }
}
