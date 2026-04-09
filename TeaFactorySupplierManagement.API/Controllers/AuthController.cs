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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var supplier = await _authService.GetSupplierByRegNoAsync(regNo);
            if (supplier == null)
                return NotFound();

            return Ok(new
            {
                regNo = supplier.RegNo,
                name = supplier.RegName,
                phone = supplier.TelNo,
                address = supplier.Address,
                idNo = supplier.IDNo
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

        // Admin only: create initial login for a supplier using their RegNo as username
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