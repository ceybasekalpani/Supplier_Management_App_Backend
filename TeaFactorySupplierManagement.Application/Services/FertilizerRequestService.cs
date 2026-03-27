using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class FertilizerRequestService : IFertilizerRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFactorySetupService _factorySetupService;

        private readonly List<string> _fertilizerTypes = new()
        {
            "Urea", "Potash", "TSP", "Dolomite", "NPK Mixture", "Organic Compost"
        };

        public FertilizerRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<FertilizerRequestDto>> GetRequestsByRegNoAsync(string regNo)
        {
            var requests = await _context.FertilizerRequests
                .Where(r => r.RegNo == regNo)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<FertilizerRequestDto>>(requests);
        }

        public async Task<FertilizerRequestDto?> GetRequestByIdAsync(int id)
        {
            var request = await _context.FertilizerRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            return request != null ? _mapper.Map<FertilizerRequestDto>(request) : null;
        }

        public async Task<FertilizerRequestDto> CreateRequestAsync(string regNo, CreateFertilizerRequestDto request)
        {
            if (!await IsFertilizerRequestEnabledAsync())
                throw new InvalidOperationException("Fertilizer request feature is not enabled");

            var requestNo = await GenerateRequestNumberAsync();

            var fertilizerRequest = _mapper.Map<FertilizerRequest>(request);
            fertilizerRequest.RegNo = regNo;
            fertilizerRequest.RequestNo = requestNo;
            fertilizerRequest.RequestDate = DateTime.UtcNow.Date;
            fertilizerRequest.RequestTime = DateTime.UtcNow.TimeOfDay;
            fertilizerRequest.Status = "pending";

            _context.FertilizerRequests.Add(fertilizerRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<FertilizerRequestDto>(fertilizerRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null)
        {
            var request = await _context.FertilizerRequests.FindAsync(id);
            if (request == null) return false;

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;

            if (status == "approved" && approvedBy != null)
            {
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.UtcNow;
            }
            else if (status == "dispatched")
            {
                request.DispatchedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var request = await _context.FertilizerRequests.FindAsync(id);
            if (request == null) return false;

            _context.FertilizerRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFertilizerRequestEnabledAsync()
        {
            var factorySetup = await _context.FactorySetups.FirstOrDefaultAsync();
            return factorySetup?.IsFertilizerRequest ?? true;
        }

        public Task<List<string>> GetFertilizerTypesAsync()
        {
            return Task.FromResult(_fertilizerTypes);
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");

            var lastRequest = await _context.FertilizerRequests
                .Where(r => r.RequestNo.StartsWith($"FERT-{yearMonth}"))
                .OrderByDescending(r => r.RequestNo)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var parts = lastRequest.RequestNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"FERT-{yearMonth}-{sequence:D3}";
        }
    }
}