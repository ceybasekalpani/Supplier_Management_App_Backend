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
        private readonly ISupabaseStorageService _storageService;
        private readonly ILogger<SettingsController> _logger;

        private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp", "application/octet-stream"];
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB

        public SettingsController(ISettingsService settingsService, ISupabaseStorageService storageService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _storageService = storageService;
            _logger = logger;
        }

        private int? GetCurrentRegNo()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return null;
            }
            return regNo;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var settings = await _settingsService.GetSettingsByRegNoAsync(regNo.Value);
            return Ok(settings ?? new UserSettingsDto());
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto settings)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var result = await _settingsService.CreateOrUpdateSettingsAsync(regNo.Value, settings);
            return Ok(result);
        }

        // Used by Swagger / browser uploads
        [HttpPut("profile-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfileImage(IFormFile image)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            _logger.LogInformation("profile-image received: file={File}, contentType={CT}, length={Len}",
                image?.FileName, image?.ContentType, image?.Length);

            if (image == null || image.Length == 0)
                return BadRequest(new { message = "No image file provided." });

            var ext = Path.GetExtension(image.FileName)?.ToLowerInvariant() ?? "";
            var validType = AllowedMimeTypes.Contains(image.ContentType) || AllowedExtensions.Contains(ext);
            if (!validType)
                return BadRequest(new { message = "Only JPEG, PNG, and WebP images are allowed." });

            if (image.Length > MaxImageSizeBytes)
                return BadRequest(new { message = "Image must be smaller than 10 MB." });

            var currentSettings = await _settingsService.GetSettingsByRegNoAsync(regNo.Value);
            var oldImageUrl = currentSettings?.ProfileImage;

            await using var stream = image.OpenReadStream();
            var publicUrl = await _storageService.UploadProfileImageAsync(
                regNo.Value, stream, image.FileName, image.ContentType);

            var saved = await _settingsService.UpdateProfileImageAsync(regNo.Value, publicUrl);

            // Compare paths without ?v= cache-bust param — same path means same file, don't delete
            if (saved && oldImageUrl != null &&
                oldImageUrl.Split('?')[0] != publicUrl.Split('?')[0])
                await _storageService.DeleteProfileImageAsync(oldImageUrl);

            return Ok(new { success = saved, imageUrl = publicUrl });
        }

        // Used by mobile app — receives base64 to avoid React Native FormData/multipart issues
        [HttpPut("profile-image-base64")]
        public async Task<IActionResult> UpdateProfileImageBase64([FromBody] UpdateProfileImageBase64Dto request)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            if (string.IsNullOrEmpty(request.ImageBase64))
                return BadRequest(new { message = "No image data provided." });

            byte[] bytes;
            try { bytes = Convert.FromBase64String(request.ImageBase64); }
            catch { return BadRequest(new { message = "Invalid image data." }); }

            if (bytes.Length == 0)
                return BadRequest(new { message = "Image is empty." });

            if (bytes.Length > MaxImageSizeBytes)
                return BadRequest(new { message = "Image must be smaller than 10 MB." });

            var mimeType = request.MimeType ?? "image/jpeg";
            var fileName = request.FileName ?? "profile.jpg";

            var currentSettings = await _settingsService.GetSettingsByRegNoAsync(regNo.Value);
            var oldImageUrl = currentSettings?.ProfileImage;

            using var stream = new MemoryStream(bytes);
            var publicUrl = await _storageService.UploadProfileImageAsync(
                regNo.Value, stream, fileName, mimeType);

            var saved = await _settingsService.UpdateProfileImageAsync(regNo.Value, publicUrl);

            // Compare paths without ?v= cache-bust param — same path means same file, don't delete
            if (saved && oldImageUrl != null &&
                oldImageUrl.Split('?')[0] != publicUrl.Split('?')[0])
                await _storageService.DeleteProfileImageAsync(oldImageUrl);

            return Ok(new { success = saved, imageUrl = publicUrl });
        }

        [HttpGet("profile-image")]
        public async Task<IActionResult> GetProfileImage()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var settings = await _settingsService.GetSettingsByRegNoAsync(regNo.Value);
            if (string.IsNullOrEmpty(settings?.ProfileImage))
                return NotFound(new { message = "No profile image found." });

            var (stream, contentType) = await _storageService.DownloadProfileImageAsync(settings.ProfileImage);
            if (stream == null)
                return NotFound(new { message = "Image could not be retrieved from storage." });

            return File(stream, contentType);
        }

        // Diagnostic — tests Supabase Storage connection, lists buckets/objects, tries test upload
        [HttpGet("storage-check")]
        public async Task<IActionResult> CheckStorage()
        {
            var result = await _storageService.CheckStorageAsync();
            return Ok(result);
        }

        [HttpPut("account-details")]
        public async Task<IActionResult> UpdateAccountDetails([FromBody] UpdateSettingsDto accountDetails)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var result = await _settingsService.UpdateAccountDetailsAsync(regNo.Value, accountDetails);
            return Ok(new { success = result });
        }

        [HttpPut("theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest request)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var result = await _settingsService.UpdateThemeAsync(regNo.Value, request.Theme);
            return Ok(new { success = result });
        }

        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var result = await _settingsService.UpdateLanguageAsync(regNo.Value, request.Language);
            return Ok(new { success = result });
        }

        [HttpPut("font-size")]
        public async Task<IActionResult> UpdateFontSize([FromBody] UpdateFontSizeRequest request)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized();

            var result = await _settingsService.UpdateFontSizeAsync(regNo.Value, request.FontSize);
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