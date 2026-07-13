using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
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
                return ToTrackingDto(existing, await GetApprovedByAsync(existing.IssuedType, existing.RequestId));
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
                return ToTrackingDto(record, request.ApprovedBy);
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
                return ToTrackingDto(record, request.ApprovedBy);
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
            return ToTrackingDto(itemRecord, itemRequest.ApprovedBy);
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
                "account" or "account transfer" or "account_transfer" => "Account Transfer",
                _ => throw new ArgumentException("Payment method must be Cash, Bank Transfer, Cheque, or Account Transfer.")
            };
        }

        private static bool IsDeliveryNoteAdvanceMethod(string? method)
        {
            var normalized = NormalizeMethod(method);
            return normalized == "Cash";
        }
    }
}
