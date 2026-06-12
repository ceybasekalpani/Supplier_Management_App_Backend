using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class FertilizerItemConfigurationService : IFertilizerItemConfigurationService
    {
        private readonly ApplicationDbContext _context;

        public FertilizerItemConfigurationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FertilizerItemConfigurationsDto> GetAllAsync(bool includeInactive = true)
        {
            var query = _context.FertilizerItemStore.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            var rows = await query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.FertilizerType ?? x.ItemType)
                .ToListAsync();

            return new FertilizerItemConfigurationsDto
            {
                Fertilizers = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.FertilizerType))
                    .Select(x => ToDto(x, "fertilizer"))
                    .ToList(),
                Items = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.ItemType))
                    .Select(x => ToDto(x, "items"))
                    .ToList()
            };
        }

        public async Task<FertilizerItemConfigurationRowDto> CreateAsync(string category, string name)
        {
            var normalizedCategory = NormalizeCategory(category);
            var cleanName = NormalizeName(name);

            await EnsureUniqueNameAsync(normalizedCategory, cleanName, null);

            var row = new FertilizerItemStore
            {
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            SetName(row, normalizedCategory, cleanName);

            _context.FertilizerItemStore.Add(row);
            await _context.SaveChangesAsync();

            return ToDto(row, normalizedCategory);
        }

        public async Task<FertilizerItemConfigurationRowDto?> UpdateAsync(string category, int id, string name, bool? isActive)
        {
            var normalizedCategory = NormalizeCategory(category);
            var cleanName = NormalizeName(name);

            var row = await FindByCategoryAndIdAsync(normalizedCategory, id);
            if (row == null)
            {
                return null;
            }

            await EnsureUniqueNameAsync(normalizedCategory, cleanName, id);

            SetName(row, normalizedCategory, cleanName);

            if (isActive.HasValue)
            {
                row.IsActive = isActive.Value;
            }

            row.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return ToDto(row, normalizedCategory);
        }

        public async Task<FertilizerItemConfigurationRowDto?> SetActiveAsync(string category, int id, bool isActive)
        {
            var normalizedCategory = NormalizeCategory(category);
            var row = await FindByCategoryAndIdAsync(normalizedCategory, id);

            if (row == null)
            {
                return null;
            }

            row.IsActive = isActive;
            row.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return ToDto(row, normalizedCategory);
        }

        private async Task<FertilizerItemStore?> FindByCategoryAndIdAsync(string category, int id)
        {
            var query = _context.FertilizerItemStore.Where(x => x.Id == id);

            return category == "fertilizer"
                ? await query.FirstOrDefaultAsync(x => x.FertilizerType != null)
                : await query.FirstOrDefaultAsync(x => x.ItemType != null);
        }

        private async Task EnsureUniqueNameAsync(string category, string name, int? currentId)
        {
            var lowered = name.ToLower();
            var query = _context.FertilizerItemStore.AsNoTracking();

            if (currentId.HasValue)
            {
                query = query.Where(x => x.Id != currentId.Value);
            }

            var exists = category == "fertilizer"
                ? await query.AnyAsync(x => x.FertilizerType != null && x.FertilizerType.ToLower() == lowered)
                : await query.AnyAsync(x => x.ItemType != null && x.ItemType.ToLower() == lowered);

            if (exists)
            {
                throw new InvalidOperationException($"{name} is already registered.");
            }
        }

        private static void SetName(FertilizerItemStore row, string category, string name)
        {
            if (category == "fertilizer")
            {
                row.FertilizerType = name;
                row.ItemType = null;
                return;
            }

            row.ItemType = name;
            row.FertilizerType = null;
        }

        private static FertilizerItemConfigurationRowDto ToDto(FertilizerItemStore row, string category)
        {
            return new FertilizerItemConfigurationRowDto
            {
                Id = row.Id,
                Category = category,
                Name = category == "fertilizer" ? row.FertilizerType ?? string.Empty : row.ItemType ?? string.Empty,
                Status = row.IsActive ? "active" : "inactive",
                IsActive = row.IsActive,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            };
        }

        private static string NormalizeCategory(string category)
        {
            var normalized = category.Trim().ToLowerInvariant();
            return normalized switch
            {
                "fertilizer" or "fertilizers" => "fertilizer",
                "item" or "items" => "items",
                _ => throw new ArgumentException("Category must be fertilizer or items.")
            };
        }

        private static string NormalizeName(string name)
        {
            var cleanName = name.Trim();

            if (string.IsNullOrWhiteSpace(cleanName))
            {
                throw new ArgumentException("Name is required.");
            }

            if (cleanName.Length > 100)
            {
                throw new ArgumentException("Name cannot be longer than 100 characters.");
            }

            return cleanName;
        }
    }
}
