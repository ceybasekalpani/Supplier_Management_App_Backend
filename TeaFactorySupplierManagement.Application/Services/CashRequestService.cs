using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class CashRequestService : ICashRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFactorySetupService _factorySetupService;

        public CashRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<CashRequestDto>> GetRequestsByRegNoAsync(int regNo)
        {
            var requests = await _context.CashRequests
                .Where(r => r.RegNo == regNo)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<CashRequestDto>>(requests);
        }

        public async Task<CashRequestDto?> GetRequestByIdAsync(int id)
        {
            var request = await _context.CashRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            return request != null ? _mapper.Map<CashRequestDto>(request) : null;
        }

        public async Task<CashRequestDto> CreateRequestAsync(int regNo, CreateCashRequestDto request)
        {
            if (!await IsCashRequestEnabledAsync())
                throw new InvalidOperationException("Cash request feature is not enabled");

            var now = DateTime.Now;

            // Auto-generate request number
            var requestNo = await GenerateRequestNumberAsync(request.RequestType);

            var cashRequest = _mapper.Map<CashRequest>(request);
            cashRequest.RegNo = regNo;
            cashRequest.RequestNo = requestNo;

            cashRequest.RequestDate = now.Date;
            cashRequest.RequestTime = now.TimeOfDay;
            cashRequest.Month = !string.IsNullOrWhiteSpace(request.SalaryDate)
                ? request.SalaryDate
                : !string.IsNullOrWhiteSpace(request.Month)
                ? request.Month
                : now.ToString("MMMM yyyy");
            cashRequest.RequestMonth = new DateTime(now.Year, now.Month, 1);
            cashRequest.Status = "pending";

            _context.CashRequests.Add(cashRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<CashRequestDto>(cashRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null)
        {
            var request = await _context.CashRequests.FindAsync(id);
            if (request == null) return false;

            request.Status = status;
            request.UpdatedAt = DateTime.Now;
            request.Remarks = remarks;

            if (status == "approved" && approvedBy != null)
            {
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.Now;
            }
            else if (status == "paid")
            {
                request.PaidAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var request = await _context.CashRequests.FindAsync(id);
            if (request == null) return false;

            _context.CashRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsCashRequestEnabledAsync()
        {
            var factorySetup = await _context.FactorySetups.FirstOrDefaultAsync();
            return factorySetup?.IsCashRequest ?? true;
        }

        public async Task<decimal?> GetAdvanceLimitAsync()
        {
            var factorySetup = await _context.FactorySetups.FirstOrDefaultAsync();
            return factorySetup?.AdvanceLimit;
        }

        private async Task<string> GenerateRequestNumberAsync(string requestType)
        {
            var prefix = requestType.ToUpper() == "ADVANCE" ? "ADV" : "LOAN";
            var yearMonth = DateTime.Now.ToString("yyyyMM");

            var lastRequest = await _context.CashRequests
                .Where(r => r.RequestNo.StartsWith($"{prefix}-{yearMonth}"))
                .OrderByDescending(r => r.RequestNo)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var parts = lastRequest.RequestNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                    sequence = lastSeq + 1;
            }

            return $"{prefix}-{yearMonth}-{sequence:D3}";
        }
    }
}
