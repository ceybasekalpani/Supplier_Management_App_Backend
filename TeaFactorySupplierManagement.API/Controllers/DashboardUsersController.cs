using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardUsersController : ControllerBase
    {
        private const long MaxProfileImageSizeBytes = 5 * 1024 * 1024;
        private const long MaxProfileImageRequestBytes = MaxProfileImageSizeBytes + (512 * 1024);
        private readonly IDashboardUserService _dashboardUserService;
        private readonly ISupabaseStorageService _storageService;
        private readonly IDashboardPermissionService _permissionService;
        private readonly ILogger<DashboardUsersController> _logger;

        public DashboardUsersController(
            IDashboardUserService dashboardUserService,
            ISupabaseStorageService storageService,
            IDashboardPermissionService permissionService,
            ILogger<DashboardUsersController> logger)
        {
            _dashboardUserService = dashboardUserService;
            _storageService = storageService;
            _permissionService = permissionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? status)
        {
            if (!await HasPermissionAsync(PermissionActions.View))
            {
                return Forbid();
            }

            var result = await _dashboardUserService.GetUsersAsync(search, status);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateDashboardUserDto request)
        {
            if (!await HasPermissionAsync(PermissionActions.Create))
            {
                return Forbid();
            }

            try
            {
                var result = await _dashboardUserService.CreateUserAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateDashboardUserDto request)
        {
            if (!await HasPermissionAsync(PermissionActions.Update))
            {
                return Forbid();
            }

            try
            {
                var result = await _dashboardUserService.UpdateUserAsync(id, request);
                if (result == null)
                {
                    return NotFound(new { message = "Dashboard user not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser([FromRoute] int id)
        {
            if (!await HasPermissionAsync(PermissionActions.Delete))
            {
                return Forbid();
            }

            try
            {
                var deleted = await _dashboardUserService.DeleteUserAsync(id, GetCurrentAdminId());
                if (!deleted)
                {
                    return NotFound(new { message = "Dashboard user not found." });
                }

                return Ok(new { message = "Dashboard user deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}/profile-image")]
        [RequestSizeLimit(MaxProfileImageRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxProfileImageRequestBytes)]
        public async Task<IActionResult> UpdateProfileImage([FromRoute] int id, [FromForm(Name = "image")] IFormFile? image)
        {
            if (!await HasPermissionAsync(PermissionActions.Update))
            {
                return Forbid();
            }

            var profileImage = ResolveProfileImageFile(image);
            var validationError = await ValidateProfileImageAsync(profileImage);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

            var oldAvatarUrl = await _dashboardUserService.GetUserAvatarAsync(id);

            try
            {
                await using var stream = profileImage!.OpenReadStream();
                var avatarUrl = await _storageService.UploadDashboardUserProfileImageAsync(
                    id,
                    stream,
                    profileImage.FileName,
                    profileImage.ContentType);

                var result = await _dashboardUserService.UpdateUserAvatarAsync(id, avatarUrl);
                if (result == null)
                {
                    await _storageService.DeleteProfileImageAsync(avatarUrl);
                    return NotFound(new { message = "Dashboard user not found." });
                }

                if (!string.IsNullOrWhiteSpace(oldAvatarUrl) && !SameStorageUrl(oldAvatarUrl, avatarUrl))
                {
                    await _storageService.DeleteProfileImageAsync(oldAvatarUrl);
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Dashboard user profile image upload failed. UserId={UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}/profile-image")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileImage([FromRoute] int id)
        {
            var avatarUrl = await _dashboardUserService.GetUserAvatarAsync(id);
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return NotFound(new { message = "No profile image found." });
            }

            var (stream, contentType) = await _storageService.DownloadProfileImageAsync(avatarUrl);
            if (stream == null)
            {
                return NotFound(new { message = "Profile image could not be retrieved from storage." });
            }

            Response.Headers.CacheControl = "public,max-age=3600";
            return File(stream, contentType);
        }

        [HttpDelete("{id:int}/profile-image")]
        public async Task<IActionResult> DeleteProfileImage([FromRoute] int id)
        {
            if (!await HasPermissionAsync(PermissionActions.Update))
            {
                return Forbid();
            }

            var oldAvatarUrl = await _dashboardUserService.GetUserAvatarAsync(id);
            var result = await _dashboardUserService.ClearUserAvatarAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Dashboard user not found." });
            }

            if (!string.IsNullOrWhiteSpace(oldAvatarUrl))
            {
                await _storageService.DeleteProfileImageAsync(oldAvatarUrl);
            }

            return Ok(result);
        }

        private int? GetCurrentAdminId()
        {
            var claim = User.FindFirst("DashboardAdminId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private async Task<bool> HasPermissionAsync(string action)
        {
            var adminId = GetCurrentAdminId();
            return adminId.HasValue && await _permissionService.HasPermissionAsync(adminId.Value, $"{PermissionModules.UserManagement}.{action}");
        }

        private IFormFile? ResolveProfileImageFile(IFormFile? image)
        {
            if (image != null)
            {
                return image;
            }

            if (!Request.HasFormContentType)
            {
                return null;
            }

            return Request.Form.Files.GetFile("image")
                   ?? Request.Form.Files.GetFile("profileImage")
                   ?? Request.Form.Files.GetFile("avatar")
                   ?? Request.Form.Files.FirstOrDefault();
        }

        private static async Task<string?> ValidateProfileImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return "Profile image is required.";
            }

            if (image.Length > MaxProfileImageSizeBytes)
            {
                return "Profile image must be smaller than 5 MB.";
            }

            var contentType = image.ContentType.ToLowerInvariant();
            if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                return "Profile image must be JPG, PNG, or WebP.";
            }

            await using var stream = image.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAsync(header);
            if (!HasValidImageSignature(header.AsSpan(0, read), contentType))
            {
                return "Profile image content does not match the selected file type.";
            }

            return null;
        }

        private static bool HasValidImageSignature(ReadOnlySpan<byte> header, string contentType)
        {
            return contentType switch
            {
                "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                "image/png" => header.Length >= 8 &&
                               header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                               header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
                "image/webp" => header.Length >= 12 &&
                                header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
        }

        private static bool SameStorageUrl(string left, string right)
        {
            return string.Equals(left.Split('?')[0], right.Split('?')[0], StringComparison.OrdinalIgnoreCase);
        }
    }
}
