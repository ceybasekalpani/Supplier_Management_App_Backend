using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Auth;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;

        public AuthController(IAuthService authService, ISettingsService settingsService)
        {
            _authService = authService;
            _settingsService = settingsService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);

            if (result == null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            var supplier = await _authService.GetSupplierByRegNoAsync(regNoClaim);
            if (supplier == null)
                return NotFound();

            var settings = await _settingsService.GetSettingsByRegNoAsync(regNo);

            return Ok(new
            {
                regNo = supplier.RegNo,
                name = supplier.RegName,
                phone = supplier.TelNo,
                phone2 = supplier.TelNo_2,
                phone3 = supplier.TelNo_3,
                address = supplier.Address,
                idNo = supplier.IDNo,
                profileImage = settings?.ProfileImage,
                settings
            });
        }

        [Authorize]
        [HttpGet("registrations")]
        public async Task<IActionResult> GetRegistrations()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var registrations = await _authService.GetSupplierRegistrationsAsync(regNo);
            return Ok(registrations);
        }

        [Authorize]
        [HttpPost("select-registration")]
        public async Task<IActionResult> SelectRegistration([FromBody] SelectRegistrationRequest request)
        {
            var result = await _authService.SelectRegistrationAsync(request.RegNo);

            if (!result)
                return BadRequest(new { message = "Invalid registration number" });

            return Ok(new { message = "Registration selected successfully" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            var result = await _authService.ChangePasswordAsync(regNo, request.CurrentPassword, request.NewPassword);

            if (!result)
                return BadRequest(new { message = "Current password is incorrect" });

            return Ok(new { message = "Password changed successfully" });
        }
 
        [Authorize]
        [HttpPost("setup-pin")]
        public async Task<IActionResult> SetupPin([FromBody] SetupPinDto request)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length < 4)
                return BadRequest(new { message = "PIN must be at least 4 digits" });

            var result = await _authService.SetupPinAsync(regNo, request.Pin);
            if (!result)
                return BadRequest(new { message = "Could not save PIN" });

            return Ok(new { message = "PIN set successfully" });
        }

        // NEW ENDPOINT
        [Authorize]
        [HttpPost("change-pin")]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinDto request)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.NewPin) || request.NewPin.Length < 4)
                return BadRequest(new { message = "New PIN must be at least 4 digits" });

            var result = await _authService.ChangePinAsync(regNo, request.CurrentPin, request.NewPin);

            if (!result)
                return BadRequest(new { message = "Current PIN is incorrect" });

            return Ok(new { message = "PIN changed successfully" });
        }

        [HttpPost("pin-login")]
        public async Task<IActionResult> PinLogin([FromBody] PinLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RegNo) || string.IsNullOrWhiteSpace(request.Pin))
                return BadRequest(new { message = "RegNo and PIN are required" });

            var result = await _authService.PinLoginAsync(request.RegNo, request.Pin);
            if (result == null)
                return Unauthorized(new { message = "Invalid PIN" });

            return Ok(result);
        }

        [HttpPost("create-login")]
        public async Task<IActionResult> CreateSupplierLogin([FromBody] CreateSupplierLoginDto request)
        {
            var result = await _authService.CreateSupplierLoginAsync(request.RegNo, request.InitialPassword);

            if (!result)
                return BadRequest(new { message = "Supplier not found or could not create login" });

            return Ok(new { message = $"Login created for supplier {request.RegNo}. Username is their RegNo." });
        }
    }

    public class SelectRegistrationRequest
    {
        public string RegNo { get; set; } = string.Empty;
    }
}