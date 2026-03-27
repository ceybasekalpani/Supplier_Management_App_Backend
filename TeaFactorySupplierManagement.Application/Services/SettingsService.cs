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

        public async Task<UserSettingsDto?> GetSettingsByRegNoAsync(string regNo)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            return settings != null ? _mapper.Map<UserSettingsDto>(settings) : null;
        }

        public async Task<UserSettingsDto> CreateOrUpdateSettingsAsync(string regNo, UpdateSettingsDto settings)
        {
            var existingSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (existingSettings != null)
            {
                // Update existing
                _mapper.Map(settings, existingSettings);
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new
                existingSettings = _mapper.Map<UserSettings>(settings);
                existingSettings.RegNo = regNo;
                existingSettings.CreatedAt = DateTime.UtcNow;
                _context.UserSettings.Add(existingSettings);
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<UserSettingsDto>(existingSettings);
        }

        public async Task<bool> UpdateProfileImageAsync(string regNo, string imageUrl)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    ProfileImage = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.ProfileImage = imageUrl;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAccountDetailsAsync(string regNo, UpdateSettingsDto accountDetails)
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
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.BankName = accountDetails.BankName;
                settings.AccountNumber = accountDetails.AccountNumber;
                settings.AccountHolder = accountDetails.AccountHolder;
                settings.Branch = accountDetails.Branch;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateThemeAsync(string regNo, string theme)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    Theme = theme,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Theme = theme;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLanguageAsync(string regNo, string language)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    Language = language,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.Language = language;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateFontSizeAsync(string regNo, int fontSize)
        {
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == regNo);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    RegNo = regNo,
                    FontSize = fontSize,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSettings.Add(settings);
            }
            else
            {
                settings.FontSize = fontSize;
                settings.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}