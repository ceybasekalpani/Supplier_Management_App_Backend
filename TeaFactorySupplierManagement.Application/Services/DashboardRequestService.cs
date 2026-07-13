using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Permissions;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DashboardRequestService : IDashboardRequestService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending",
            "approved",
            "rejected"
        };

        private readonly ApplicationDbContext _context;
        private readonly IFirebasePushService _firebasePushService;
        private readonly IDashboardPermissionService _permissionService;
        private readonly ICashRequestService _cashRequestService;
        private readonly ILogger<DashboardRequestService> _logger;

        public DashboardRequestService(
            ApplicationDbContext context,
            IFirebasePushService firebasePushService,
            IDashboardPermissionService permissionService,
            ICashRequestService cashRequestService,
            ILogger<DashboardRequestService> logger)
        {
            _context = context;
            _firebasePushService = firebasePushService;
            _permissionService = permissionService;
            _cashRequestService = cashRequestService;
            _logger = logger;
        }

        public async Task<DashboardRequestsResponseDto> GetRequestsAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var normalizedStatus = NormalizeOptionalStatus(status);
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var advance = await GetAdvanceRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate);
            var fertilizer = await GetFertilizerRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate);
            var items = await GetItemRequestsAsync(normalizedStatus, normalizedSearch, fromDate, toDate);

            return new DashboardRequestsResponseDto
            {
                Advance = advance,
                Fertilizer = fertilizer,
                Items = items,
                Totals = BuildTotals(advance, fertilizer, items)
            };
        }

        public async Task<DashboardRequestRowDto?> UpdateRequestStatusAsync(string requestType, int id, string status, string? remarks, string? reviewedBy, int? reviewedByAdminId)
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

                await EnsureStatusChangeAllowedAsync(request.Status, normalizedStatus, reviewedByAdminId, normalizedType);

                if (normalizedStatus == "approved" &&
                    string.Equals(request.RequestType?.Trim(), "ADVANCE", StringComparison.OrdinalIgnoreCase))
                {
                    var limit = await _cashRequestService.GetAdvanceLimitAsync(request.RegNo, DateTime.Today);
                    if (limit.HasValue && request.Amount > limit.Value)
                        throw new InvalidOperationException(
                            $"Cannot approve: requested amount (Rs. {request.Amount:N2}) exceeds the supplier's maximum advance limit of Rs. {limit.Value:N2}.");
                }

                var previousStatus = request.Status;
                request.Status = normalizedStatus;
                request.Remarks = remarks;
                request.ApprovedBy = reviewer;
                request.ApprovedAt = now;
                request.UpdatedAt = now;
                var requestSummary = $"Rs. {request.Amount:N2}";
                AddRequestStatusNotification(request.RegNo, normalizedType, request.Id, normalizedStatus, previousStatus, requestSummary, remarks, reviewer, now);

                await _context.SaveChangesAsync();
                await SendRequestStatusPushAsync(request.RegNo, normalizedType, request.Id, normalizedStatus, remarks, requestSummary);
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

                await EnsureStatusChangeAllowedAsync(request.Status, normalizedStatus, reviewedByAdminId, normalizedType);

                var previousStatus = request.Status;
                request.Status = normalizedStatus;
                request.Remarks = remarks;
                request.ApprovedBy = reviewer;
                request.ApprovedAt = now;
                request.UpdatedAt = now;
                var requestSummary = $"{request.FertilizerType} - {request.Quantity:N2} {request.Unit}";
                AddRequestStatusNotification(request.RegNo, normalizedType, request.Id, normalizedStatus, previousStatus, requestSummary, remarks, reviewer, now);

                await _context.SaveChangesAsync();
                await SendRequestStatusPushAsync(request.RegNo, normalizedType, request.Id, normalizedStatus, remarks, requestSummary);
                return ToFertilizerRow(request);
            }

            var itemRequest = await _context.ItemRequests
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (itemRequest == null)
            {
                return null;
            }

            await EnsureStatusChangeAllowedAsync(itemRequest.Status, normalizedStatus, reviewedByAdminId, normalizedType);

            var previousItemStatus = itemRequest.Status;
            itemRequest.Status = normalizedStatus;
            itemRequest.Remarks = remarks;
            itemRequest.ApprovedBy = reviewer;
            itemRequest.ApprovedAt = now;
            itemRequest.UpdatedAt = now;
            var itemSummary = $"{itemRequest.ItemType} - {itemRequest.Quantity:N0} {itemRequest.Unit ?? "units"}";
            AddRequestStatusNotification(itemRequest.RegNo, normalizedType, itemRequest.Id, normalizedStatus, previousItemStatus, itemSummary, remarks, reviewer, now);

            await _context.SaveChangesAsync();
            await SendRequestStatusPushAsync(itemRequest.RegNo, normalizedType, itemRequest.Id, normalizedStatus, remarks, itemSummary);
            return ToItemRow(itemRequest);
        }

        private async Task SendRequestStatusPushAsync(int regNo, string requestType, int requestId, string status, string? remarks, string requestSummary)
        {
            try
            {
                await _firebasePushService.SendRequestStatusChangedAsync(regNo, requestType, requestId, status, remarks: remarks, requestSummary: requestSummary);
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

        private void AddRequestStatusNotification(
            int regNo,
            string requestType,
            int requestId,
            string nextStatus,
            string previousStatus,
            string requestSummary,
            string? remarks,
            string createdBy,
            DateTime now)
        {
            if (string.Equals(previousStatus, nextStatus, StringComparison.OrdinalIgnoreCase) ||
                nextStatus is not ("approved" or "rejected"))
            {
                return;
            }

            _context.Notifications.Add(new Notification
            {
                Title = BuildRequestStatusTitle(requestType, nextStatus),
                Message = BuildRequestStatusMessage(requestType, nextStatus, requestSummary, remarks),
                Type = "request_status",
                Priority = nextStatus == "rejected" ? 2 : 1,
                IsActive = true,
                StartDate = now,
                TargetRegNo = regNo,
                CreatedBy = createdBy,
                CreatedAt = now
            });
        }

        private static string BuildRequestStatusTitle(string requestType, string status)
        {
            return $"{ToDisplayRequestType(requestType)} request {ToDisplayStatus(status)}";
        }

        private static string BuildRequestStatusMessage(string requestType, string status, string requestSummary, string? remarks)
        {
            var cleanSummary = string.IsNullOrWhiteSpace(requestSummary) ? string.Empty : $" ({requestSummary.Trim()})";
            var message = $"Your {ToDisplayRequestType(requestType).ToLowerInvariant()} request{cleanSummary} has been {ToDisplayStatus(status).ToLowerInvariant()}.";

            if (status == "rejected" && !string.IsNullOrWhiteSpace(remarks))
            {
                message += $" Reason: {remarks.Trim()}";
            }

            return message;
        }

        private async Task EnsureStatusChangeAllowedAsync(string currentStatus, string nextStatus, int? adminId, string requestType)
        {
            var current = NormalizeStatus(currentStatus);
            if (current == nextStatus)
            {
                return;
            }

            var permissionCode = $"{RequestTypeToPermissionModule(requestType)}.{PermissionActions.Approve}";

            if (!adminId.HasValue || !await _permissionService.HasPermissionAsync(adminId.Value, permissionCode))
            {
                throw new UnauthorizedAccessException("Permission required to approve or reject this request.");
            }
        }

        private static string RequestTypeToPermissionModule(string requestType)
        {
            return requestType switch
            {
                "advance" => PermissionModules.CashRequests,
                "fertilizer" => PermissionModules.FertilizerRequests,
                "items" => PermissionModules.ItemRequests,
                _ => throw new ArgumentException("Invalid request type")
            };
        }

        private static DashboardRequestReportTotalsDto BuildTotals(
            List<DashboardRequestRowDto> advance,
            List<DashboardRequestRowDto> fertilizer,
            List<DashboardRequestRowDto> items)
        {
            var supplierCount = advance
                .Concat(fertilizer)
                .Concat(items)
                .Select(x => x.RegNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return new DashboardRequestReportTotalsDto
            {
                AdvanceCount = advance.Count,
                FertilizerCount = fertilizer.Count,
                ItemCount = items.Count,
                SupplierCount = supplierCount,
                TotalAdvanceAmount = advance.Sum(x => x.Amount ?? 0),
                TotalFertilizerQuantity = fertilizer.Sum(x => x.Qty ?? 0),
                TotalItemQuantity = items.Sum(x => x.Qty ?? 0)
            };
        }
    }
}
