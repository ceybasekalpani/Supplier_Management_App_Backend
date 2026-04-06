using TeaFactorySupplierManagement.Application.DTOs.Settings;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ISettingsService
{
    Task<UserSettingsDto?> GetSettingsByRegNoAsync(int regNo);
    Task<UserSettingsDto> CreateOrUpdateSettingsAsync(int regNo, UpdateSettingsDto settings);
    Task<bool> UpdateProfileImageAsync(int regNo, string imageUrl);
    Task<bool> UpdateAccountDetailsAsync(int regNo, UpdateSettingsDto accountDetails);
    Task<bool> UpdateThemeAsync(int regNo, string theme);
    Task<bool> UpdateLanguageAsync(int regNo, string language);
    Task<bool> UpdateFontSizeAsync(int regNo, int fontSize);
}
}