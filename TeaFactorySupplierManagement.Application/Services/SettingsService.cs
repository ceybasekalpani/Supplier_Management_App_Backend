using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Settings;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SettingsService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<UserSettingsDto?> GetSettingsByRegNoAsync(int regNo)
        {
            var settings = await _context.UserSettings
                .AsNoTracking() // Better for read-only operations
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
                return null;

            var dto = _mapper.Map<UserSettingsDto>(settings);

            // Ensure default values for nulls
            dto.Theme ??= "light";
            dto.Language ??= "english";
            dto.FontSize ??= 50;

            return dto;
        }

        public async Task<UserSettingsDto> CreateOrUpdateSettingsAsync(int regNo, UpdateSettingsDto settings)
        {
            var existingSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (existingSettings != null)
            {
                // Update existing - only update provided fields
                if (settings.Theme != null)
                    existingSettings.Theme = settings.Theme;

                if (settings.Language != null)
                    existingSettings.Language = settings.Language;

                if (settings.FontSize.HasValue)
                    existingSettings.FontSize = settings.FontSize;

                if (settings.Address != null)
                    existingSettings.Address = settings.Address;

                if (settings.Phone != null)
                    existingSettings.Phone = settings.Phone;

                if (settings.BankName != null)
                    existingSettings.BankName = settings.BankName;

                if (settings.AccountNumber != null)
                    existingSettings.AccountNumber = settings.AccountNumber;

                if (settings.AccountHolder != null)
                    existingSettings.AccountHolder = settings.AccountHolder;

                if (settings.Branch != null)
                    existingSettings.Branch = settings.Branch;

                existingSettings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(existingSettings).State = EntityState.Modified;
            }
            else
            {
                // Create new with defaults
                existingSettings = new UserSettings
                {
                    RegNo = regNo,
                    Theme = settings.Theme ?? "light",
                    Language = settings.Language ?? "english",
                    FontSize = settings.FontSize ?? 50,
                    Address = settings.Address,
                    Phone = settings.Phone,
                    BankName = settings.BankName,
                    AccountNumber = settings.AccountNumber,
                    AccountHolder = settings.AccountHolder,
                    Branch = settings.Branch,
                    ProfileImage = null,
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(existingSettings);
            }

            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<UserSettingsDto>(existingSettings);
            return resultDto;
        }

        public async Task<bool> UpdateProfileImageAsync(int regNo, string imageUrl)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    ProfileImage = imageUrl,
                    Theme = "light",
                    Language = "english",
                    FontSize = 50,
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.ProfileImage = imageUrl;
                settings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(settings).State = EntityState.Modified;
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateAccountDetailsAsync(int regNo, UpdateSettingsDto accountDetails)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    BankName = accountDetails.BankName,
                    AccountNumber = accountDetails.AccountNumber,
                    AccountHolder = accountDetails.AccountHolder,
                    Branch = accountDetails.Branch,
                    Address = accountDetails.Address, // FIXED: Include Address
                    Phone = accountDetails.Phone,     // FIXED: Include Phone
                    Theme = "light",
                    Language = "english",
                    FontSize = 50,
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                // Update only provided fields
                if (accountDetails.BankName != null)
                    settings.BankName = accountDetails.BankName;

                if (accountDetails.AccountNumber != null)
                    settings.AccountNumber = accountDetails.AccountNumber;

                if (accountDetails.AccountHolder != null)
                    settings.AccountHolder = accountDetails.AccountHolder;

                if (accountDetails.Branch != null)
                    settings.Branch = accountDetails.Branch;

                if (accountDetails.Address != null)
                    settings.Address = accountDetails.Address;

                if (accountDetails.Phone != null)
                    settings.Phone = accountDetails.Phone;

                settings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(settings).State = EntityState.Modified;
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateThemeAsync(int regNo, string theme)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    Theme = theme,
                    Language = "english",
                    FontSize = 50,
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Theme = theme;
                settings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(settings).State = EntityState.Modified;
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateLanguageAsync(int regNo, string language)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    Language = language,
                    Theme = "light",
                    FontSize = 50,
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Language = language;
                settings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(settings).State = EntityState.Modified;
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateFontSizeAsync(int regNo, int fontSize)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    FontSize = fontSize,
                    Theme = "light",
                    Language = "english",
                    CreatedAt = DateTime.Now, // Sri Lanka time
                    UpdatedAt = null
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.FontSize = fontSize;
                settings.UpdatedAt = DateTime.Now; // Sri Lanka time
                _context.Entry(settings).State = EntityState.Modified;
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}