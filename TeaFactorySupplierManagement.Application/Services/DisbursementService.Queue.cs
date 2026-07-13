using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
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
                    PaymentMethod = GetQueuePaymentMethod(record, x.Supplier),
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
                    PaymentMethod = GetQueuePaymentMethod(record, x.Supplier),
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
                    PaymentMethod = GetQueuePaymentMethod(record, x.Supplier),
                    TrackingStatus = record?.CurrentStatus
                };
            }).ToList();
        }

        private static string? GetQueuePaymentMethod(DisbursementRecord? record, RegSupplier? supplier)
        {
            if (!string.IsNullOrWhiteSpace(record?.Method))
            {
                return record.Method.Trim();
            }

            return CleanOptional(supplier?.PayMode) ?? CleanOptional(supplier?.PayMode_Temp);
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
    }
}
