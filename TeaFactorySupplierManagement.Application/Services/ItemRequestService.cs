using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class ItemRequestService : IItemRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFactorySetupService _factorySetupService;


        public ItemRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<ItemRequestDto>> GetRequestsByRegNoAsync(int regNo)
        {
            var requests = await _context.ItemRequests
                .Where(r => r.RegNo == regNo)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<ItemRequestDto>>(requests);
        }

        public async Task<ItemRequestDto?> GetRequestByIdAsync(int id)
        {
            var request = await _context.ItemRequests
                .AsNoTracking() // Add this to avoid tracking issues
                .FirstOrDefaultAsync(r => r.Id == id);

            return request != null ? _mapper.Map<ItemRequestDto>(request) : null;
        }

        public async Task<ItemRequestDto> CreateRequestAsync(int regNo, CreateItemRequestDto request)
        {
            if (!await IsItemRequestEnabledAsync())
                throw new InvalidOperationException("Item request feature is not enabled");

            var now = DateTime.Now;
            var requestNo = await GenerateRequestNumberAsync();

            var itemRequest = _mapper.Map<ItemRequest>(request);
            itemRequest.RegNo = regNo;
            itemRequest.RequestNo = requestNo;
            itemRequest.RequestDate = now.Date;
            itemRequest.RequestTime = now.TimeOfDay;
            itemRequest.Month = now.ToString("MMMM yyyy");
            itemRequest.RequestMonth = new DateTime(now.Year, now.Month, 1);
            itemRequest.Status = "pending";
            itemRequest.CreatedAt = now; // Ensure CreatedAt is set

            _context.ItemRequests.Add(itemRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<ItemRequestDto>(itemRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null, string? remarks = null)
        {
            // Use FindAsync with tracking
            var request = await _context.ItemRequests.FindAsync(id);
            if (request == null) return false;

            // Log current values
            Console.WriteLine($"=== BEFORE UPDATE ===");
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"Current Status: {request.Status}");
            Console.WriteLine($"Current Remarks: {request.Remarks}");
            Console.WriteLine($"Current IssuedAt: {request.IssuedAt}");
            Console.WriteLine($"New Status: {status}");
            Console.WriteLine($"New Remarks: {remarks}");
            Console.WriteLine($"ApprovedBy: {approvedBy}");

            // Update basic fields
            request.Status = status;
            request.UpdatedAt = DateTime.Now;

            // Update remarks - IMPORTANT: Only update if remarks is provided
            // If remarks is null, keep existing remarks
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
            else if (status == "issued")
            {
                request.IssuedAt = DateTime.Now;
                Console.WriteLine($"Issued at: {request.IssuedAt}");
            }

            Console.WriteLine($"=== AFTER UPDATE ===");
            Console.WriteLine($"Status: {request.Status}");
            Console.WriteLine($"Remarks: {request.Remarks}");
            Console.WriteLine($"IssuedAt: {request.IssuedAt}");
            Console.WriteLine($"UpdatedAt: {request.UpdatedAt}");

            // Explicitly mark entity as modified
            _context.Entry(request).State = EntityState.Modified;

            // Save changes and get result
            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"SaveChanges Result: {result} rows affected");

            // Verify if changes were saved by querying fresh
            var freshRequest = await _context.ItemRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (freshRequest != null)
            {
                Console.WriteLine($"=== VERIFICATION FROM DATABASE ===");
                Console.WriteLine($"Status in DB: {freshRequest.Status}");
                Console.WriteLine($"Remarks in DB: {freshRequest.Remarks}");
                Console.WriteLine($"IssuedAt in DB: {freshRequest.IssuedAt}");
                Console.WriteLine($"UpdatedAt in DB: {freshRequest.UpdatedAt}");
            }

            return result > 0;
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var request = await _context.ItemRequests.FindAsync(id);
            if (request == null) return false;

            _context.ItemRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsItemRequestEnabledAsync()
        {
            var factorySetup = await _context.FactorySetups.FirstOrDefaultAsync();
            return factorySetup?.IsItemRequest ?? true;
        }

        public async Task<List<string>> GetItemTypesAsync()
        {
            return await _context.FertilizerItemStore
                .Where(f => f.IsActive && f.ItemType != null)
                .Select(f => f.ItemType!)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var yearMonth = DateTime.Now.ToString("yyyyMM");

            var lastRequest = await _context.ItemRequests
                .Where(r => r.RequestNo.StartsWith($"ITEM-{yearMonth}"))
                .OrderByDescending(r => r.RequestNo)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var parts = lastRequest.RequestNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                    sequence = lastSeq + 1;
            }

            return $"ITEM-{yearMonth}-{sequence:D3}";
        }
    }
}