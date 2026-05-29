using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.API.Controllers
{
    /// <summary>
    /// Returns a per-month summary of cash / fertilizer / item requests
    /// so the mobile app can show a "Monthly Account Summary" view.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountSummaryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountSummaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentRegNo()
        {
            var claim = User.FindFirst("RegNo")?.Value;
            return int.TryParse(claim, out int regNo) ? regNo : null;
        }

        /// <summary>
        /// GET api/accountsummary/monthly-requests?months=6
        /// Returns one summary object per calendar month for the last N months
        /// (default 6, max 24). Each object contains totals for cash,
        /// fertilizer and item requests. Individual requests now include
        /// a full requestDate (ISO 8601) so the mobile client can display
        /// the exact day the request was created.
        /// </summary>
        [HttpGet("monthly-requests")]
        public async Task<IActionResult> GetMonthlyRequestSummary([FromQuery] int months = 6)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            // Clamp to a sensible range
            if (months < 1)  months = 1;
            if (months > 24) months = 24;

            var today = DateTime.Now;
            var result = new List<object>();

            // Build a list of (year, month, label, key) tuples for the requested range
            var monthSlots = Enumerable.Range(0, months)
                .Select(i => today.AddMonths(-(months - 1 - i)))
                .Select(d => new
                {
                    d.Year,
                    d.Month,
                    Label = d.ToString("MMM yyyy"),
                    Key   = d.ToString("yyyy-MM")
                })
                .ToList();

            // Pull all requests for this supplier in bulk (one query each) then
            // group in memory to avoid N+1.
            var cashRequests = await _context.CashRequests
                .Where(r => r.RegNo == regNo
                         && r.RequestMonth >= today.AddMonths(-months).Date)
                .Select(r => new
                {
                    Year        = r.RequestMonth.Year,
                    Month       = r.RequestMonth.Month,
                    // RequestDate is the exact creation date stored on the record
                    RequestDate = r.RequestDate,
                    r.Amount,
                    r.Status
                })
                .ToListAsync();

            var fertRequests = await _context.FertilizerRequests
                .Where(r => r.RegNo == regNo
                         && r.RequestMonth >= today.AddMonths(-months).Date)
                .Select(r => new
                {
                    Year           = r.RequestMonth.Year,
                    Month          = r.RequestMonth.Month,
                    RequestDate    = r.RequestDate,
                    r.Quantity,
                    r.Unit,
                    r.Status,
                    r.FertilizerType
                })
                .ToListAsync();

            var itemRequests = await _context.ItemRequests
                .Where(r => r.RegNo == regNo
                         && r.RequestMonth >= today.AddMonths(-months).Date)
                .Select(r => new
                {
                    Year        = r.RequestMonth.Year,
                    Month       = r.RequestMonth.Month,
                    RequestDate = r.RequestDate,
                    r.Quantity,
                    r.Unit,
                    r.Status,
                    r.ItemType
                })
                .ToListAsync();

            foreach (var slot in monthSlots)
            {
                var cash = cashRequests
                    .Where(r => r.Year == slot.Year && r.Month == slot.Month)
                    .ToList();

                var fert = fertRequests
                    .Where(r => r.Year == slot.Year && r.Month == slot.Month)
                    .ToList();

                var item = itemRequests
                    .Where(r => r.Year == slot.Year && r.Month == slot.Month)
                    .ToList();

                result.Add(new
                {
                    monthKey   = slot.Key,
                    monthLabel = slot.Label,

                    cash = new
                    {
                        count         = cash.Count,
                        totalAmount   = cash.Sum(r => r.Amount),
                        pendingCount  = cash.Count(r => r.Status == "pending"),
                        approvedCount = cash.Count(r => r.Status == "approved"),
                        paidCount     = cash.Count(r => r.Status == "paid"),
                        requests      = cash.Select(r => new
                        {
                            // Full ISO date string, e.g. "2026-05-14" — client parses this
                            requestDate = r.RequestDate.ToString("yyyy-MM-dd"),
                            amount      = r.Amount,
                            status      = r.Status
                        })
                    },

                    fertilizer = new
                    {
                        count           = fert.Count,
                        totalQuantity   = fert.Sum(r => r.Quantity),
                        pendingCount    = fert.Count(r => r.Status == "pending"),
                        approvedCount   = fert.Count(r => r.Status == "approved"),
                        dispatchedCount = fert.Count(r => r.Status == "dispatched"),
                        requests        = fert.Select(r => new
                        {
                            requestDate    = r.RequestDate.ToString("yyyy-MM-dd"),
                            fertilizerType = r.FertilizerType,
                            quantity       = r.Quantity,
                            unit           = r.Unit,
                            status         = r.Status
                        })
                    },

                    item = new
                    {
                        count         = item.Count,
                        totalQuantity = item.Sum(r => r.Quantity),
                        pendingCount  = item.Count(r => r.Status == "pending"),
                        approvedCount = item.Count(r => r.Status == "approved"),
                        issuedCount   = item.Count(r => r.Status == "issued"),
                        requests      = item.Select(r => new
                        {
                            requestDate = r.RequestDate.ToString("yyyy-MM-dd"),
                            itemType    = r.ItemType,
                            quantity    = r.Quantity,
                            unit        = r.Unit,
                            status      = r.Status
                        })
                    }
                });
            }

            return Ok(result);
        }
    }
}