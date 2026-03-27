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

        private readonly List<string> _itemTypes = new()
        {
            "Pruning Shears", "Harvesting Bag", "Protective Gloves",
            "Rain Coat", "Bamboo Basket", "Harvesting Knife",
            "Weighing Bag", "Sun Hat"
        };

        public ItemRequestService(ApplicationDbContext context, IMapper mapper, IFactorySetupService factorySetupService)
        {
            _context = context;
            _mapper = mapper;
            _factorySetupService = factorySetupService;
        }

        public async Task<List<ItemRequestDto>> GetRequestsByRegNoAsync(string regNo)
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
                .FirstOrDefaultAsync(r => r.Id == id);

            return request != null ? _mapper.Map<ItemRequestDto>(request) : null;
        }

        public async Task<ItemRequestDto> CreateRequestAsync(string regNo, CreateItemRequestDto request)
        {
            if (!await IsItemRequestEnabledAsync())
                throw new InvalidOperationException("Item request feature is not enabled");

            var requestNo = await GenerateRequestNumberAsync();

            var itemRequest = _mapper.Map<ItemRequest>(request);
            itemRequest.RegNo = regNo;
            itemRequest.RequestNo = requestNo;
            itemRequest.RequestDate = DateTime.UtcNow.Date;
            itemRequest.RequestTime = DateTime.UtcNow.TimeOfDay;
            itemRequest.Status = "pending";

            _context.ItemRequests.Add(itemRequest);
            await _context.SaveChangesAsync();

            return _mapper.Map<ItemRequestDto>(itemRequest);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status, string? approvedBy = null)
        {
            var request = await _context.ItemRequests.FindAsync(id);
            if (request == null) return false;

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;

            if (status == "approved" && approvedBy != null)
            {
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.UtcNow;
            }
            else if (status == "issued")
            {
                request.IssuedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
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

        public Task<List<string>> GetItemTypesAsync()
        {
            return Task.FromResult(_itemTypes);
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var yearMonth = DateTime.UtcNow.ToString("yyyyMM");

            var lastRequest = await _context.ItemRequests
                .Where(r => r.RequestNo.StartsWith($"ITEM-{yearMonth}"))
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

            return $"ITEM-{yearMonth}-{sequence:D3}";
        }
    }
}