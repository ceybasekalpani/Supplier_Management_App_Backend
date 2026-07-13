using Microsoft.EntityFrameworkCore;
using System.Data;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
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
                if (normalizedType == "advance" && !IsDeliveryNoteAdvanceMethod(record.Method))
                {
                    continue;
                }

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
                throw new ArgumentException("No eligible records found. Only cash advances, fertilizer, and item records can be dispatched; cheque, bank transfer, and account transfer advances are excluded.");
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
                record.DeliveryNoteNo = header.DN_No;
                record.DispatchedAt = now;
                record.IssueDate = now.Date;
                record.UpdatedAt = now;

                _context.DeliveryNoteDetails.Add(new DeliveryNoteDetail
                {
                    DeliveryNoteId = header.Id,
                    DeliveryNoteNo = header.DN_No,
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
                .Include(x => x.Details)
                    .ThenInclude(x => x.DisbursementRecord)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (header == null)
            {
                return null;
            }

            var approvals = await GetApprovalLookupAsync(header.Details.Select(GetApprovalKey));
            return ToDeliveryNoteDto(header, approvals);
        }

        public async Task<DeliveryNoteDto?> MarkDeliveryNoteReturnedAsync(int id, string? remarks)
        {
            return await UpdateDeliveryNoteStatusAsync(id, "returned", remarks);
        }

        public async Task<DeliveryNoteDto?> MarkDeliveryNoteCompletedAsync(int id, string? remarks, string? completedBy, string? completedDevice)
        {
            return await UpdateDeliveryNoteStatusAsync(id, "completed", remarks, completedBy, completedDevice);
        }

        private async Task<DeliveryNoteDto?> UpdateDeliveryNoteStatusAsync(int id, string status, string? remarks, string? completedBy = null, string? completedDevice = null)
        {
            var normalizedStatus = NormalizeDeliveryNoteStatus(status);
            var now = DateTime.Now;
            var cleanCompletedBy = string.IsNullOrWhiteSpace(completedBy) ? "Dashboard Admin" : completedBy.Trim();
            var cleanCompletedDevice = string.IsNullOrWhiteSpace(completedDevice) ? null : completedDevice.Trim();

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
                    detail.DisbursementRecord.CompletedDate = now;
                    detail.DisbursementRecord.CompletedBy = cleanCompletedBy;
                    detail.DisbursementRecord.CompletedDevice = cleanCompletedDevice;
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

        private static string NormalizeDeliveryNoteStatus(string status)
        {
            var normalized = status.Trim().ToLowerInvariant();
            return normalized switch
            {
                "issued" or "returned" or "completed" or "cancelled" => normalized,
                _ => throw new ArgumentException("Delivery note status must be issued, returned, completed, or cancelled.")
            };
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
    }
}
