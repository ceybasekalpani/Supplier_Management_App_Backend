using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Settings;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var settings = await _settingsService.GetSettingsByRegNoAsync(regNo);
            return Ok(settings ?? new UserSettingsDto());
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto settings)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.CreateOrUpdateSettingsAsync(regNo, settings);
            return Ok(result);
        }

        [HttpPut("profile-image")]
        public async Task<IActionResult> UpdateProfileImage([FromBody] UpdateProfileImageDto request)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.UpdateProfileImageAsync(regNo, request.ProfileImage);
            return Ok(new { success = result });
        }

        [HttpPut("account-details")]
        public async Task<IActionResult> UpdateAccountDetails([FromBody] UpdateSettingsDto accountDetails)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.UpdateAccountDetailsAsync(regNo, accountDetails);
            return Ok(new { success = result });
        }

        [HttpPut("theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest request)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.UpdateThemeAsync(regNo, request.Theme);
            return Ok(new { success = result });
        }

        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.UpdateLanguageAsync(regNo, request.Language);
            return Ok(new { success = result });
        }

        [HttpPut("font-size")]
        public async Task<IActionResult> UpdateFontSize([FromBody] UpdateFontSizeRequest request)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var result = await _settingsService.UpdateFontSizeAsync(regNo, request.FontSize);
            return Ok(new { success = result });
        }
    }

    public class UpdateThemeRequest
    {
        public string Theme { get; set; } = string.Empty;
    }

    public class UpdateLanguageRequest
    {
        public string Language { get; set; } = string.Empty;
    }

    public class UpdateFontSizeRequest
    {
        public int FontSize { get; set; }
    }
}