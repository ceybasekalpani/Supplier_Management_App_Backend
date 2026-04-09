using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    }

    public class SelectRegistrationRequest
    {
        public string RegNo { get; set; } = string.Empty;
    }
}