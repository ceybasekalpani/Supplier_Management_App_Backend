using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Factory;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class FactorySetupService : IFactorySetupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FactorySetupService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<FactorySetupDto?> GetFactorySetupAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();
            return setup != null ? _mapper.Map<FactorySetupDto>(setup) : null;
        }

        public async Task<FactoryFeaturesDto> GetFactoryFeaturesAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();

            if (setup == null)
            {
                // Return default values if no setup exists
                return new FactoryFeaturesDto
                {
                    IsSuper = false,
                    IsCashRequestEnabled = true,
                    IsItemRequestEnabled = true,
                    IsFertilizerRequestEnabled = true
                };
            }

            return _mapper.Map<FactoryFeaturesDto>(setup);
        }

        public async Task<bool> IsSuperLeafEnabledAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();
            return setup?.IsSuper ?? false;
        }

        public async Task<bool> IsCashRequestEnabledAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();
            return setup?.IsCashRequest ?? true;
        }

        public async Task<bool> IsItemRequestEnabledAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();
            return setup?.IsItemRequest ?? true;
        }

        public async Task<bool> IsFertilizerRequestEnabledAsync()
        {
            var setup = await _context.FactorySetups.FirstOrDefaultAsync();
            return setup?.IsFertilizerRequest ?? true;
        }
    }
}