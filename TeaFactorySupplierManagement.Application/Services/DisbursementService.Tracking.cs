using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
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

            var approvals = await GetApprovalLookupAsync(rows.Select(x => new ApprovalKey(x.IssuedType, x.RequestId)));
            return rows.Select(x => ToTrackingDto(x, GetApprovedBy(approvals, x.IssuedType, x.RequestId))).ToList();
        }

        public async Task<DisbursementTrackingDto?> MarkReceivedAsync(int id, string? completedBy, string? completedDevice)
        {
            var record = await _context.DisbursementRecords.FindAsync(id);
            if (record == null)
            {
                return null;
            }

            var now = DateTime.Now;
            record.CurrentStatus = "completed";
            record.CompletedDate = now;
            record.CompletedBy = string.IsNullOrWhiteSpace(completedBy) ? "Dashboard Admin" : completedBy.Trim();
            record.CompletedDevice = string.IsNullOrWhiteSpace(completedDevice) ? null : completedDevice.Trim();
            record.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return ToTrackingDto(record, await GetApprovedByAsync(record.IssuedType, record.RequestId));
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
    }
}
