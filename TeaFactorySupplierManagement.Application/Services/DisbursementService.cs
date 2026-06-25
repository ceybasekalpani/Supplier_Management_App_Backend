using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
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

        public async Task<DeliveryNoteDto> GenerateDeliveryNoteAsync(GenerateDeliveryNoteDto request, int? createdBy)
        {
            ValidateDeliveryNoteRequest(request);

            var now = DateTime.Now;
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var records = new List<DisbursementRecord>();
            foreach (var selection in request.Records)
            {
                var normalizedType = NormalizeIssuedType(selection.IssuedType);
                if (normalizedType == "advance" && !IsDeliveryNoteAdvanceMethod(selection.Method))
                {
                    continue;
                }

                var record = await EnsureDisbursementRecordAsync(normalizedType, selection.RequestId, selection.Method, createdBy, now);
                if (record.DeliveryNoteId.HasValue || record.CurrentStatus == "dispatched" || record.CurrentStatus == "completed")
                {
                    throw new ArgumentException($"Request {selection.RequestId} ({normalizedType}) is already dispatched.");
                }

                records.Add(record);
            }

            records = records
                .GroupBy(x => new { x.IssuedType, x.RequestId })
                .Select(x => x.First())
                .ToList();

            if (records.Count == 0)
            {
                throw new ArgumentException("No eligible records found. Cash advances, fertilizer, and item records can be dispatched; bank transfer advances are excluded.");
            }

            var deliveryNoteNo = await GenerateDeliveryNoteNoAsync(now);
            var header = new DeliveryNoteHeader
            {
                DN_No = deliveryNoteNo,
                DispatchDate = now,
                BorrowerName = request.BorrowerName.Trim(),
                BorrowerRole = request.BorrowerRole.Trim(),
                VehicleNo = CleanOptional(request.VehicleNo),
                RouteName = CleanOptional(request.RouteName),
                Remarks = CleanOptional(request.Remarks),
                Status = "issued",
                CreatedBy = createdBy,
                CreatedAt = now
            };

            _context.DeliveryNoteHeaders.Add(header);
            await _context.SaveChangesAsync();

            foreach (var record in records)
            {
                record.CurrentStatus = "dispatched";
                record.DeliveryNoteId = header.Id;
                record.DispatchedAt = now;
                record.IssueDate = now.Date;
                record.UpdatedAt = now;

                _context.DeliveryNoteDetails.Add(new DeliveryNoteDetail
                {
                    DeliveryNoteId = header.Id,
                    SupplierRegNo = record.RegNo,
                    DisbursementRecordId = record.Id,
                    RequestId = record.RequestId,
                    ItemType = record.IssuedType,
                    Amount = record.Amount,
                    Quantity = record.Quantity,
                    Unit = record.Unit,
                    PaymentType = record.Method,
                    Status = "dispatched",
                    SupplierSignatureReceived = false,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            foreach (var record in records)
            {
                await SendDisbursementStatusPushAsync(record.RegNo, record.IssuedType, record.RequestId, "dispatched");
            }

            var saved = await GetDeliveryNoteAsync(header.Id);
            return saved ?? throw new InvalidOperationException("Delivery note was generated but could not be loaded.");
        }

        public async Task<List<DeliveryNoteTrackingDto>> GetDeliveryNotesAsync(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.DeliveryNoteHeaders
                .AsNoTracking()
                .Include(x => x.Details)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Status == NormalizeDeliveryNoteStatus(status));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.DispatchDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.DispatchDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                query = query.Where(x =>
                    x.DN_No.Contains(cleanSearch) ||
                    x.BorrowerName.Contains(cleanSearch) ||
                    (x.RouteName != null && x.RouteName.Contains(cleanSearch)) ||
                    (x.VehicleNo != null && x.VehicleNo.Contains(cleanSearch)));
            }

            var rows = await query
                .OrderByDescending(x => x.DispatchDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return rows.Select(ToDeliveryNoteTrackingDto).ToList();
        }

        public async Task<DeliveryNoteDto?> GetDeliveryNoteAsync(int id)
        {
            var header = await _context.DeliveryNoteHeaders
                .AsNoTracking()
                .Include(x => x.Details)
                    .ThenInclude(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id);

            return header == null ? null : ToDeliveryNoteDto(header);
        }

        public async Task<DeliveryNoteDto?> MarkDeliveryNoteReturnedAsync(int id, string? remarks)
        {
            return await UpdateDeliveryNoteStatusAsync(id, "returned", remarks);
        }

        public async Task<DeliveryNoteDto?> MarkDeliveryNoteCompletedAsync(int id, string? remarks)
        {
            return await UpdateDeliveryNoteStatusAsync(id, "completed", remarks);
        }

        public async Task<string?> BuildDeliveryNotePrintHtmlAsync(int id)
        {
            var note = await GetDeliveryNoteAsync(id);
            if (note == null)
            {
                return null;
            }

            var rows = new StringBuilder();
            for (var i = 0; i < note.Details.Count; i++)
            {
                var detail = note.Details[i];
                var amountOrQty = detail.Amount.HasValue
                    ? $"Rs. {detail.Amount.Value:N2}"
                    : $"{detail.Quantity:N2} {detail.Unit}".Trim();

                rows.AppendLine($"""
                    <tr>
                        <td>{i + 1}</td>
                        <td>{EscapeHtml(detail.SupplierRegNo.ToString())}</td>
                        <td>{EscapeHtml(detail.SupplierName)}</td>
                        <td>{EscapeHtml(ToDisplayIssuedType(detail.ItemType))}</td>
                        <td>{EscapeHtml(amountOrQty)}</td>
                        <td class="signature"></td>
                    </tr>
                    """);
            }

            return $$"""
                <!doctype html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Delivery Note {{EscapeHtml(note.DeliveryNoteNo)}}</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 32px; color: #111; }
                        h1 { text-align: center; margin-bottom: 8px; }
                        .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 24px; margin: 24px 0; }
                        table { width: 100%; border-collapse: collapse; margin-top: 16px; }
                        th, td { border: 1px solid #333; padding: 8px; text-align: left; }
                        th { background: #f3f3f3; }
                        .signature { height: 36px; }
                        .footer { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 24px; margin-top: 48px; }
                        .line { border-top: 1px solid #111; padding-top: 6px; text-align: center; }
                    </style>
                </head>
                <body>
                    <h1>Delivery Note / Gate Pass</h1>
                    <div class="meta">
                        <div><strong>DN No:</strong> {{EscapeHtml(note.DeliveryNoteNo)}}</div>
                        <div><strong>Date:</strong> {{EscapeHtml(note.DispatchDate)}}</div>
                        <div><strong>Borrower:</strong> {{EscapeHtml(note.BorrowerName)}}</div>
                        <div><strong>Role:</strong> {{EscapeHtml(note.BorrowerRole)}}</div>
                        <div><strong>Vehicle No:</strong> {{EscapeHtml(note.VehicleNo ?? string.Empty)}}</div>
                        <div><strong>Route:</strong> {{EscapeHtml(note.RouteName ?? string.Empty)}}</div>
                    </div>
                    <table>
                        <thead>
                            <tr>
                                <th>No</th>
                                <th>Supplier Reg No</th>
                                <th>Supplier Name</th>
                                <th>Type</th>
                                <th>Amount / Qty</th>
                                <th>Signature</th>
                            </tr>
                        </thead>
                        <tbody>{{rows}}</tbody>
                    </table>
                    <div class="footer">
                        <div class="line">Issued By</div>
                        <div class="line">Borrower Signature</div>
                        <div class="line">Checked By</div>
                    </div>
                </body>
                </html>
                """;
        }

        public async Task<List<DisbursementTrackingDto>> GetTrackingAsync(string? issuedType, string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.DisbursementRecords
                .AsNoTracking()
                .Include(x => x.DeliveryNote)
                .AsQueryable();

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

        private async Task<DeliveryNoteDto?> UpdateDeliveryNoteStatusAsync(int id, string status, string? remarks)
        {
            var normalizedStatus = NormalizeDeliveryNoteStatus(status);
            var now = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var header = await _context.DeliveryNoteHeaders
                .Include(x => x.Details)
                    .ThenInclude(x => x.DisbursementRecord)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (header == null)
            {
                return null;
            }

            header.Status = normalizedStatus;
            header.Remarks = string.IsNullOrWhiteSpace(remarks) ? header.Remarks : remarks.Trim();
            header.CompletedAt = normalizedStatus == "completed" ? now : header.CompletedAt;
            header.UpdatedAt = now;

            foreach (var detail in header.Details)
            {
                detail.Status = normalizedStatus == "completed" ? "delivered" : normalizedStatus;
                detail.SupplierSignatureReceived = normalizedStatus is "returned" or "completed";
                detail.UpdatedAt = now;

                if (detail.DisbursementRecord != null && normalizedStatus == "completed")
                {
                    detail.DisbursementRecord.CurrentStatus = "completed";
                    detail.DisbursementRecord.CompletedDate = now.Date;
                    detail.DisbursementRecord.UpdatedAt = now;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return await GetDeliveryNoteAsync(id);
        }

        private async Task<string> GenerateDeliveryNoteNoAsync(DateTime now)
        {
            const string prefix = "DN";
            var sequence = await _context.CodeGenerations
                .FirstOrDefaultAsync(x => x.CodePrefix == prefix && x.Year == now.Year);

            if (sequence == null)
            {
                sequence = new CodeGeneration
                {
                    CodePrefix = prefix,
                    Year = now.Year,
                    Description = "Delivery Note Sequence",
                    LastNo = 0,
                    CreatedAt = now
                };
                _context.CodeGenerations.Add(sequence);
            }

            sequence.LastNo += 1;
            sequence.UpdatedAt = now;
            await _context.SaveChangesAsync();

            return $"{prefix}-{now.Year}-{sequence.LastNo:0000}";
        }

        private async Task<DisbursementRecord> EnsureDisbursementRecordAsync(
            string normalizedType,
            int requestId,
            string? method,
            int? createdBy,
            DateTime now)
        {
            var existing = await _context.DisbursementRecords
                .FirstOrDefaultAsync(x => x.IssuedType == normalizedType && x.RequestId == requestId);

            if (existing != null)
            {
                if (normalizedType == "advance" && IsDeliveryNoteAdvanceMethod(method))
                {
                    existing.Method = NormalizeMethod(method);
                }

                return existing;
            }

            var issuedBy = createdBy.HasValue ? createdBy.Value.ToString() : "Dashboard Admin";

            if (normalizedType == "advance")
            {
                var request = await _context.CashRequests
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

                if (request == null)
                {
                    throw new ArgumentException($"Approved advance request {requestId} was not found.");
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
                    Method = NormalizeMethod(method),
                    CurrentStatus = "awaiting",
                    IssuedBy = issuedBy,
                    CreatedAt = now
                };

                _context.DisbursementRecords.Add(record);
                await _context.SaveChangesAsync();
                return record;
            }

            if (normalizedType == "fertilizer")
            {
                var request = await _context.FertilizerRequests
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

                if (request == null)
                {
                    throw new ArgumentException($"Approved fertilizer request {requestId} was not found.");
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
                    Method = "Physical Delivery",
                    CurrentStatus = "awaiting",
                    IssuedBy = issuedBy,
                    CreatedAt = now
                };

                _context.DisbursementRecords.Add(record);
                await _context.SaveChangesAsync();
                return record;
            }

            var itemRequest = await _context.ItemRequests
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == requestId && x.Status == "approved");

            if (itemRequest == null)
            {
                throw new ArgumentException($"Approved item request {requestId} was not found.");
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
                Method = "Physical Delivery",
                CurrentStatus = "awaiting",
                IssuedBy = issuedBy,
                CreatedAt = now
            };

            _context.DisbursementRecords.Add(itemRecord);
            await _context.SaveChangesAsync();
            return itemRecord;
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
                CompletedDevice = record.CompletedDevice,
                DeliveryNoteId = record.DeliveryNoteId,
                DeliveryNoteNo = record.DeliveryNote?.DN_No,
                DispatchedAt = record.DispatchedAt.HasValue ? ToDateKey(record.DispatchedAt.Value) : null
            };
        }

        private static DeliveryNoteDto ToDeliveryNoteDto(DeliveryNoteHeader header)
        {
            var details = header.Details
                .OrderBy(x => x.Id)
                .Select(ToDeliveryNoteDetailDto)
                .ToList();

            return new DeliveryNoteDto
            {
                Id = header.Id,
                DeliveryNoteNo = header.DN_No,
                DispatchDate = ToDateTimeKey(header.DispatchDate),
                BorrowerName = header.BorrowerName,
                BorrowerRole = header.BorrowerRole,
                VehicleNo = header.VehicleNo,
                RouteName = header.RouteName,
                Status = header.Status,
                CreatedBy = header.CreatedBy,
                CreatedAt = ToDateTimeKey(header.CreatedAt),
                CompletedAt = header.CompletedAt.HasValue ? ToDateTimeKey(header.CompletedAt.Value) : null,
                Remarks = header.Remarks,
                TotalRecords = details.Count,
                TotalAmount = details.Sum(x => x.Amount ?? 0),
                TotalQuantity = details.Sum(x => x.Quantity ?? 0),
                PrintUrl = $"/api/Disbursement/delivery-notes/{header.Id}/print",
                Details = details
            };
        }

        private static DeliveryNoteDetailDto ToDeliveryNoteDetailDto(DeliveryNoteDetail detail)
        {
            return new DeliveryNoteDetailDto
            {
                Id = detail.Id,
                SupplierRegNo = detail.SupplierRegNo,
                SupplierName = detail.Supplier?.RegName ?? string.Empty,
                RouteName = detail.Supplier?.Route ?? string.Empty,
                DisbursementRecordId = detail.DisbursementRecordId,
                RequestId = detail.RequestId,
                ItemType = detail.ItemType,
                Amount = detail.Amount,
                Quantity = detail.Quantity,
                Unit = detail.Unit,
                PaymentType = detail.PaymentType,
                Status = detail.Status,
                SupplierSignatureReceived = detail.SupplierSignatureReceived,
                Remarks = detail.Remarks
            };
        }

        private static DeliveryNoteTrackingDto ToDeliveryNoteTrackingDto(DeliveryNoteHeader header)
        {
            return new DeliveryNoteTrackingDto
            {
                Id = header.Id,
                DeliveryNoteNo = header.DN_No,
                DispatchDate = ToDateTimeKey(header.DispatchDate),
                BorrowerName = header.BorrowerName,
                BorrowerRole = header.BorrowerRole,
                VehicleNo = header.VehicleNo,
                RouteName = header.RouteName,
                Status = header.Status,
                TotalRecords = header.Details.Count,
                PrintUrl = $"/api/Disbursement/delivery-notes/{header.Id}/print"
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
                "awaiting" or "dispatched" or "returned" or "completed" or "cancelled" => normalized,
                _ => throw new ArgumentException("Status must be awaiting, dispatched, returned, completed, or cancelled.")
            };
        }

        private static string NormalizeDeliveryNoteStatus(string status)
        {
            var normalized = status.Trim().ToLowerInvariant();
            return normalized switch
            {
                "issued" or "returned" or "completed" or "cancelled" => normalized,
                _ => throw new ArgumentException("Delivery note status must be issued, returned, completed, or cancelled.")
            };
        }

        private static string NormalizeMethod(string? method)
        {
            var cleanMethod = method?.Trim();
            if (string.IsNullOrWhiteSpace(cleanMethod))
            {
                throw new ArgumentException("Payment method is required for advance disbursement.");
            }

            return cleanMethod.ToLowerInvariant() switch
            {
                "cash" => "Cash",
                "bank" or "bank transfer" or "bank_transfer" => "Bank Transfer",
                "cheque" or "check" => "Cheque",
                _ => throw new ArgumentException("Payment method must be Cash, Bank Transfer, or Cheque.")
            };
        }

        private static bool IsDeliveryNoteAdvanceMethod(string? method)
        {
            var normalized = NormalizeMethod(method);
            return normalized is "Cash" or "Cheque";
        }

        private static void ValidateDeliveryNoteRequest(GenerateDeliveryNoteDto request)
        {
            if (request.Records.Count == 0)
            {
                throw new ArgumentException("At least one disbursement record is required.");
            }

            if (string.IsNullOrWhiteSpace(request.BorrowerName))
            {
                throw new ArgumentException("Borrower name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.BorrowerRole))
            {
                throw new ArgumentException("Borrower role is required.");
            }
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ToDisplayIssuedType(string value)
        {
            return value switch
            {
                "advance" => "Cash Advance",
                "fertilizer" => "Fertilizer",
                "items" => "Item",
                _ => value
            };
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

        private static string ToDateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        private static string ToDateTimeKey(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}
