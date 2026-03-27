using TeaFactorySupplierManagement.Application.DTOs.Settings;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ISettingsService
    {
        Task<UserSettingsDto?> GetSettingsByRegNoAsync(string regNo);
        Task<UserSettingsDto> CreateOrUpdateSettingsAsync(string regNo, UpdateSettingsDto settings);
        Task<bool> UpdateProfileImageAsync(string regNo, string imageUrl);
        Task<bool> UpdateAccountDetailsAsync(string regNo, UpdateSettingsDto accountDetails);
        Task<bool> UpdateThemeAsync(string regNo, string theme);
        Task<bool> UpdateLanguageAsync(string regNo, string language);
        Task<bool> UpdateFontSizeAsync(string regNo, int fontSize);
    }
}