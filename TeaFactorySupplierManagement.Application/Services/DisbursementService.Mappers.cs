using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
        private readonly record struct ApprovalKey(string IssuedType, int RequestId);

        private async Task<Dictionary<ApprovalKey, string>> GetApprovalLookupAsync(IEnumerable<ApprovalKey> keys)
        {
            var distinctKeys = keys
                .Where(x => !string.IsNullOrWhiteSpace(x.IssuedType) && x.RequestId > 0)
                .Select(x => new ApprovalKey(NormalizeApprovalIssuedType(x.IssuedType), x.RequestId))
                .Distinct()
                .ToList();

            var approvals = new Dictionary<ApprovalKey, string>();

            var advanceIds = distinctKeys.Where(x => x.IssuedType == "advance").Select(x => x.RequestId).Distinct().ToList();
            if (advanceIds.Count > 0)
            {
                var rows = await _context.CashRequests
                    .AsNoTracking()
                    .Where(x => advanceIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.ApprovedBy })
                    .ToListAsync();

                foreach (var row in rows)
                {
                    approvals[new ApprovalKey("advance", row.Id)] = CleanApprover(row.ApprovedBy);
                }
            }

            var fertilizerIds = distinctKeys.Where(x => x.IssuedType == "fertilizer").Select(x => x.RequestId).Distinct().ToList();
            if (fertilizerIds.Count > 0)
            {
                var rows = await _context.FertilizerRequests
                    .AsNoTracking()
                    .Where(x => fertilizerIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.ApprovedBy })
                    .ToListAsync();

                foreach (var row in rows)
                {
                    approvals[new ApprovalKey("fertilizer", row.Id)] = CleanApprover(row.ApprovedBy);
                }
            }

            var itemIds = distinctKeys.Where(x => x.IssuedType == "items").Select(x => x.RequestId).Distinct().ToList();
            if (itemIds.Count > 0)
            {
                var rows = await _context.ItemRequests
                    .AsNoTracking()
                    .Where(x => itemIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.ApprovedBy })
                    .ToListAsync();

                foreach (var row in rows)
                {
                    approvals[new ApprovalKey("items", row.Id)] = CleanApprover(row.ApprovedBy);
                }
            }

            return approvals;
        }

        private async Task<string> GetApprovedByAsync(string issuedType, int requestId)
        {
            var approvals = await GetApprovalLookupAsync(new[] { new ApprovalKey(issuedType, requestId) });
            return GetApprovedBy(approvals, issuedType, requestId);
        }

        private static ApprovalKey GetApprovalKey(DeliveryNoteDetail detail)
        {
            var issuedType = detail.DisbursementRecord?.IssuedType ?? detail.ItemType;
            return new ApprovalKey(issuedType, detail.RequestId);
        }

        private static string GetApprovedBy(Dictionary<ApprovalKey, string> approvals, string issuedType, int requestId)
        {
            return approvals.TryGetValue(new ApprovalKey(NormalizeApprovalIssuedType(issuedType), requestId), out var approvedBy)
                ? approvedBy
                : string.Empty;
        }

        private static string CleanApprover(string? value)
        {
            var clean = value?.Trim();
            return string.IsNullOrWhiteSpace(clean) || clean == "-" ? string.Empty : clean;
        }

        private static string NormalizeApprovalIssuedType(string issuedType)
        {
            var clean = issuedType.Trim().ToLowerInvariant();
            return clean switch
            {
                "advance" or "cash" => "advance",
                "fertilizer" or "fertiliser" => "fertilizer",
                "item" or "items" => "items",
                _ => clean
            };
        }

        private static DisbursementTrackingDto ToTrackingDto(DisbursementRecord record, string? approvedBy = null)
        {
            var cleanApprovedBy = CleanApprover(approvedBy);

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
                ApprovedBy = cleanApprovedBy,
                CheckedBy = cleanApprovedBy,
                IssueDate = ToDateKey(record.IssueDate),
                Method = record.Method,
                CurrentStatus = record.CurrentStatus,
                CompletedDate = record.CompletedDate.HasValue ? ToDateTimeKey(record.CompletedDate.Value) : null,
                CompletedBy = record.CompletedBy,
                CompletedDevice = record.CompletedDevice,
                DeliveryNoteId = record.DeliveryNoteId,
                DeliveryNoteNo = record.DeliveryNote?.DN_No ?? record.DeliveryNoteNo,
                DispatchedAt = record.DispatchedAt.HasValue ? ToDateKey(record.DispatchedAt.Value) : null
            };
        }

        private static DeliveryNoteDto ToDeliveryNoteDto(DeliveryNoteHeader header, Dictionary<ApprovalKey, string> approvals)
        {
            var details = header.Details
                .OrderBy(x => x.Id)
                .Select(x => ToDeliveryNoteDetailDto(x, GetApprovedBy(approvals, GetApprovalKey(x).IssuedType, GetApprovalKey(x).RequestId)))
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

        private static DeliveryNoteDetailDto ToDeliveryNoteDetailDto(DeliveryNoteDetail detail, string? approvedBy = null)
        {
            var record = detail.DisbursementRecord;
            var cleanApprovedBy = CleanApprover(approvedBy);
            var issuedType = record?.IssuedType ?? detail.ItemType;

            return new DeliveryNoteDetailDto
            {
                Id = detail.Id,
                RequestNo = record?.RequestNo ?? string.Empty,
                SupplierRegNo = detail.SupplierRegNo,
                SupplierName = detail.Supplier?.RegName ?? string.Empty,
                RouteName = detail.Supplier?.Route ?? string.Empty,
                DisbursementRecordId = detail.DisbursementRecordId,
                DeliveryNoteNo = detail.DeliveryNoteNo,
                RequestId = detail.RequestId,
                IssuedType = issuedType,
                IssuedDetails = record?.IssuedDetails ?? string.Empty,
                ItemName = record?.ItemName,
                FertilizerType = issuedType == "fertilizer" ? record?.ItemName : null,
                ItemType = detail.ItemType,
                Amount = detail.Amount,
                Quantity = detail.Quantity,
                Unit = detail.Unit,
                PaymentType = detail.PaymentType,
                ApprovedBy = cleanApprovedBy,
                CheckedBy = cleanApprovedBy,
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

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
