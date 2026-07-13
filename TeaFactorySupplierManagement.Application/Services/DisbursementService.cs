using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService : IDisbursementService
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
    }
}
