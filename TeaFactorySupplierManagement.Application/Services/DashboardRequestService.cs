using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardRequestService : IDashboardRequestService
    {
        private const string RequestStatusOverridePermission = "requests.status_override";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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

        public async Task<DashboardReportFileDto> ExportRequestsAsync(string format, string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var report = await GetRequestsAsync(status, search, fromDate, toDate);
            var normalizedFormat = string.IsNullOrWhiteSpace(format) ? "pdf" : format.Trim().ToLowerInvariant();
            var generatedAt = DateTime.Now;
            var fileStamp = generatedAt.ToString("yyyyMMdd-HHmmss");

            return normalizedFormat switch
            {
                "pdf" => new DashboardReportFileDto
                {
                    FileName = $"request-report-{fileStamp}.pdf",
                    ContentType = "application/pdf",
                    Content = BuildPdfReport(report, generatedAt)
                },
                "doc" or "docs" or "word" => new DashboardReportFileDto
                {
                    FileName = $"request-report-{fileStamp}.doc",
                    ContentType = "application/msword",
                    Content = Encoding.UTF8.GetBytes(BuildDocReportHtml(report, generatedAt))
                },
                _ => throw new ArgumentException("Export format must be pdf or doc.")
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

                await EnsureStatusChangeAllowedAsync(request.Status, normalizedStatus, reviewedByAdminId);

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

                await EnsureStatusChangeAllowedAsync(request.Status, normalizedStatus, reviewedByAdminId);

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

            await EnsureStatusChangeAllowedAsync(itemRequest.Status, normalizedStatus, reviewedByAdminId);

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

        private async Task EnsureStatusChangeAllowedAsync(string currentStatus, string nextStatus, int? adminId)
        {
            var current = NormalizeStatus(currentStatus);
            if (current == nextStatus)
            {
                return;
            }

            var restrictedFlip =
                (current == "approved" && nextStatus == "rejected") ||
                (current == "rejected" && nextStatus == "approved");

            if (!restrictedFlip)
            {
                return;
            }

            if (!adminId.HasValue || !await HasRequestStatusOverridePermissionAsync(adminId.Value))
            {
                throw new UnauthorizedAccessException("Permission required to change an approved request to rejected or a rejected request to approved.");
            }
        }

        private async Task<bool> HasRequestStatusOverridePermissionAsync(int adminId)
        {
            var admin = await _context.SupplierDashboardAdminLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminId && x.IsActive);

            if (admin == null)
            {
                return false;
            }

            if (IsSuperAdmin(admin))
            {
                return true;
            }

            var permission = await _context.SupplierDashboardAdminPermissions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdminId == adminId && x.ModuleId == "requests" && x.IsEnabled);

            if (permission == null)
            {
                return false;
            }

            var subPermissions = ParseSubPermissions(permission.SubPermissionsJson);
            return subPermissions.Contains(RequestStatusOverridePermission, StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> ParseSubPermissions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static bool IsSuperAdmin(SupplierDashboardAdminLogin admin)
        {
            return admin.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("mainadmin", StringComparison.OrdinalIgnoreCase) ||
                   admin.Username.Equals("main_admin", StringComparison.OrdinalIgnoreCase);
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

        private static string BuildDocReportHtml(DashboardRequestsResponseDto report, DateTime generatedAt)
        {
            var rows = BuildFlatRows(report);
            var rowHtml = new StringBuilder();
            foreach (var row in rows)
            {
                rowHtml.AppendLine($"""
                    <tr>
                        <td>{EscapeHtml(row.Category)}</td>
                        <td>{EscapeHtml(row.RequestNo)}</td>
                        <td>{EscapeHtml(row.RegNo)}</td>
                        <td>{EscapeHtml(row.Name)}</td>
                        <td>{EscapeHtml(row.Type)}</td>
                        <td>{EscapeHtml(row.SalaryDate)}</td>
                        <td>{EscapeHtml(row.AmountOrQty)}</td>
                        <td>{EscapeHtml(row.Status)}</td>
                        <td>{EscapeHtml(row.CheckedBy)}</td>
                        <td>{EscapeHtml(row.Remarks)}</td>
                    </tr>
                    """);
            }

            return $$"""
                <!doctype html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <style>
                        body { font-family: Arial, sans-serif; color: #172033; }
                        h1 { text-align: center; margin-bottom: 4px; }
                        .subtitle { text-align: center; color: #5b6475; margin-bottom: 22px; }
                        table { width: 100%; border-collapse: collapse; font-size: 11px; }
                        th { background: #124734; color: white; padding: 7px; border: 1px solid #124734; }
                        td { padding: 6px; border: 1px solid #c9d1d9; vertical-align: top; }
                        .totals { margin-top: 16px; width: 60%; }
                        .totals th { text-align: left; }
                    </style>
                </head>
                <body>
                    <h1>Supplier Request Report</h1>
                    <div class="subtitle">Generated at {{EscapeHtml(generatedAt.ToString("yyyy-MM-dd HH:mm"))}}</div>
                    <table>
                        <thead>
                            <tr>
                                <th>Category</th>
                                <th>Request No</th>
                                <th>Reg No</th>
                                <th>Supplier</th>
                                <th>Type</th>
                                <th>Salary Date</th>
                                <th>Amount / Qty</th>
                                <th>Status</th>
                                <th>Checked By</th>
                                <th>Remarks</th>
                            </tr>
                        </thead>
                        <tbody>{{rowHtml}}</tbody>
                    </table>
                    {{BuildTotalsHtml(report.Totals)}}
                </body>
                </html>
                """;
        }

        private static string BuildTotalsHtml(DashboardRequestReportTotalsDto totals)
        {
            return $$"""
                <table class="totals">
                    <tr><th colspan="2">Report Totals</th></tr>
                    <tr><td>Advance Count</td><td>{{totals.AdvanceCount}}</td></tr>
                    <tr><td>Fertilizer Count</td><td>{{totals.FertilizerCount}}</td></tr>
                    <tr><td>Item Count</td><td>{{totals.ItemCount}}</td></tr>
                    <tr><td>Supplier Count</td><td>{{totals.SupplierCount}}</td></tr>
                    <tr><td>Total Advance Amount</td><td>Rs. {{totals.TotalAdvanceAmount:N2}}</td></tr>
                    <tr><td>Total Fertilizer Quantity</td><td>{{totals.TotalFertilizerQuantity:N2}}</td></tr>
                    <tr><td>Total Item Quantity</td><td>{{totals.TotalItemQuantity:N2}}</td></tr>
                </table>
                """;
        }

        private static byte[] BuildPdfReport(DashboardRequestsResponseDto report, DateTime generatedAt)
        {
            var lines = new List<string>
            {
                "Supplier Request Report",
                $"Generated at {generatedAt:yyyy-MM-dd HH:mm}",
                "",
                "Category | Request No | Reg No | Supplier | Type | Salary Date | Amount/Qty | Status"
            };

            lines.AddRange(BuildFlatRows(report).Select(row =>
                $"{row.Category} | {row.RequestNo} | {row.RegNo} | {row.Name} | {row.Type} | {row.SalaryDate} | {row.AmountOrQty} | {row.Status}"));

            lines.Add("");
            lines.Add("Report Totals");
            lines.Add($"Advance Count: {report.Totals.AdvanceCount}");
            lines.Add($"Fertilizer Count: {report.Totals.FertilizerCount}");
            lines.Add($"Item Count: {report.Totals.ItemCount}");
            lines.Add($"Supplier Count: {report.Totals.SupplierCount}");
            lines.Add($"Total Advance Amount: Rs. {report.Totals.TotalAdvanceAmount:N2}");
            lines.Add($"Total Fertilizer Quantity: {report.Totals.TotalFertilizerQuantity:N2}");
            lines.Add($"Total Item Quantity: {report.Totals.TotalItemQuantity:N2}");

            return SimplePdf.Create("Supplier Request Report", lines);
        }

        private static List<ReportRow> BuildFlatRows(DashboardRequestsResponseDto report)
        {
            return report.Advance.Select(x => ToReportRow("Advance", x))
                .Concat(report.Fertilizer.Select(x => ToReportRow("Fertilizer", x)))
                .Concat(report.Items.Select(x => ToReportRow("Item", x)))
                .ToList();
        }

        private static ReportRow ToReportRow(string category, DashboardRequestRowDto row)
        {
            var amountOrQty = row.Amount.HasValue
                ? $"Rs. {row.Amount.Value:N2}"
                : $"{row.Qty:N2} {row.Unit}".Trim();

            return new ReportRow(
                category,
                row.RequestNo,
                row.RegNo,
                row.Name,
                row.Type,
                row.SalaryDate,
                amountOrQty,
                row.Status,
                row.CheckedBy,
                row.Remarks);
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

        private static string EscapeHtml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private sealed record ReportRow(
            string Category,
            string RequestNo,
            string RegNo,
            string Name,
            string Type,
            string SalaryDate,
            string AmountOrQty,
            string Status,
            string CheckedBy,
            string Remarks);

        private static class SimplePdf
        {
            public static byte[] Create(string title, IReadOnlyList<string> lines)
            {
                var objects = new List<string>();
                var pages = lines.Chunk(42).Select((pageLines, index) => BuildPage(pageLines.ToList(), index + 1)).ToList();

                objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
                var pageRefs = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{3 + i * 2} 0 R"));
                objects.Add($"<< /Type /Pages /Kids [{pageRefs}] /Count {pages.Count} >>");

                for (var i = 0; i < pages.Count; i++)
                {
                    var pageObjectNo = 3 + i * 2;
                    var contentObjectNo = pageObjectNo + 1;
                    objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 << /Type /Font /Subtype /Helvetica /BaseFont /Helvetica >> /F2 << /Type /Font /Subtype /Helvetica-Bold /BaseFont /Helvetica-Bold >> >> >> /Contents {contentObjectNo} 0 R >>");
                    objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(pages[i])} >>\nstream\n{pages[i]}\nendstream");
                }

                var pdf = new StringBuilder();
                pdf.AppendLine("%PDF-1.4");
                var offsets = new List<int> { 0 };

                for (var i = 0; i < objects.Count; i++)
                {
                    offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                    pdf.AppendLine($"{i + 1} 0 obj");
                    pdf.AppendLine(objects[i]);
                    pdf.AppendLine("endobj");
                }

                var xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
                pdf.AppendLine("xref");
                pdf.AppendLine($"0 {objects.Count + 1}");
                pdf.AppendLine("0000000000 65535 f ");
                foreach (var offset in offsets.Skip(1))
                {
                    pdf.AppendLine($"{offset:0000000000} 00000 n ");
                }

                pdf.AppendLine("trailer");
                pdf.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R /Info << /Title ({EscapePdf(title)}) >> >>");
                pdf.AppendLine("startxref");
                pdf.AppendLine(xrefOffset.ToString());
                pdf.AppendLine("%%EOF");

                return Encoding.ASCII.GetBytes(pdf.ToString());
            }

            private static string BuildPage(IReadOnlyList<string> lines, int pageNo)
            {
                var content = new StringBuilder();
                content.AppendLine("BT");
                content.AppendLine("/F2 16 Tf");
                content.AppendLine("50 555 Td");
                content.AppendLine($"({EscapePdf(lines.FirstOrDefault() ?? string.Empty)}) Tj");
                content.AppendLine("/F1 9 Tf");
                content.AppendLine("0 -22 Td");
                foreach (var line in lines.Skip(1))
                {
                    content.AppendLine($"({EscapePdf(TrimForPdf(line))}) Tj");
                    content.AppendLine("0 -12 Td");
                }
                content.AppendLine($"/F1 8 Tf 700 20 Td (Page {pageNo}) Tj");
                content.AppendLine("ET");
                return content.ToString();
            }

            private static string TrimForPdf(string value)
            {
                return value.Length <= 150 ? value : value[..147] + "...";
            }

            private static string EscapePdf(string value)
            {
                var ascii = new string(value.Select(ch => ch <= 126 ? ch : '?').ToArray());
                return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }
        }
    }
}
