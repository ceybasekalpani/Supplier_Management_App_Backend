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


        public FertilizerRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<FertilizerRequestDto>> GetRequestsByRegNoAsync(int regNo)
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

        public async Task<FertilizerRequestDto> CreateRequestAsync(int regNo, CreateFertilizerRequestDto request)
        {
            if (!await IsFertilizerRequestEnabledAsync())
                throw new InvalidOperationException("Fertilizer request feature is not enabled");

            var now = DateTime.Now;
            var requestNo = await GenerateRequestNumberAsync();

            var fertilizerRequest = _mapper.Map<FertilizerRequest>(request);
            fertilizerRequest.RegNo = regNo;
            fertilizerRequest.RequestNo = requestNo;
            fertilizerRequest.RequestDate = now.Date;
            fertilizerRequest.RequestTime = now.TimeOfDay;
            fertilizerRequest.Month = !string.IsNullOrWhiteSpace(request.Month)
                ? request.Month
                : now.ToString("MMMM yyyy");
            fertilizerRequest.RequestMonth = new DateTime(now.Year, now.Month, 1);
            fertilizerRequest.Status = "pending";

            _context.FertilizerRequests.Add(fertilizerRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<FertilizerRequestDto>(fertilizerRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null)
        {
            var request = await _context.FertilizerRequests.FindAsync(id);
            if (request == null) return false;

            // Log current values
            Console.WriteLine($"=== BEFORE UPDATE ===");
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"Current Status: {request.Status}");
            Console.WriteLine($"Current Remarks: {request.Remarks}");
            Console.WriteLine($"Current DispatchedAt: {request.DispatchedAt}");
            Console.WriteLine($"New Status: {status}");
            Console.WriteLine($"New Remarks: {remarks}");
            Console.WriteLine($"ApprovedBy: {approvedBy}");

            // Update basic fields
            request.Status = status;
            request.UpdatedAt = DateTime.Now;

            // Update remarks if provided
            if (remarks != null)
            {
                request.Remarks = remarks;
                Console.WriteLine($"Remarks updated to: {remarks}");
            }

            // Handle status-specific fields
            if (status == "approved" && approvedBy != null)
            {
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.Now;
                Console.WriteLine($"Approved by: {approvedBy} at {request.ApprovedAt}");
            }
            else if (status == "dispatched")
            {
                request.DispatchedAt = DateTime.Now;
                Console.WriteLine($"Dispatched at: {request.DispatchedAt}");
            }

            Console.WriteLine($"=== AFTER UPDATE ===");
            Console.WriteLine($"Status: {request.Status}");
            Console.WriteLine($"Remarks: {request.Remarks}");
            Console.WriteLine($"DispatchedAt: {request.DispatchedAt}");
            Console.WriteLine($"UpdatedAt: {request.UpdatedAt}");

            // Explicitly mark entity as modified
            _context.Entry(request).State = EntityState.Modified;

            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"SaveChanges Result: {result} rows affected");

            // Verify
            var freshRequest = await _context.FertilizerRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (freshRequest != null)
            {
                Console.WriteLine($"=== VERIFICATION FROM DATABASE ===");
                Console.WriteLine($"Status in DB: {freshRequest.Status}");
                Console.WriteLine($"Remarks in DB: {freshRequest.Remarks}");
                Console.WriteLine($"DispatchedAt in DB: {freshRequest.DispatchedAt}");
                Console.WriteLine($"UpdatedAt in DB: {freshRequest.UpdatedAt}");
            }

            return result > 0;
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

        public async Task<List<string>> GetFertilizerTypesAsync()
        {
            return await _context.FertilizerItemStore
                .Where(f => f.IsActive && f.FertilizerType != null)
                .Select(f => f.FertilizerType!)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var yearMonth = DateTime.Now.ToString("yyyyMM");

            var lastRequest = await _context.FertilizerRequests
                .Where(r => r.RequestNo.StartsWith($"FERT-{yearMonth}"))
                .OrderByDescending(r => r.RequestNo)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var parts = lastRequest.RequestNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                    sequence = lastSeq + 1;
            }

            return $"FERT-{yearMonth}-{sequence:D3}";
        }
    }
}