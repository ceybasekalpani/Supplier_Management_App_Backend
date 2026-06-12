using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardAuthController : ControllerBase
    {
        private readonly IDashboardAdminAuthService _dashboardAdminAuthService;

        public DashboardAuthController(IDashboardAdminAuthService dashboardAdminAuthService)
        {
            _dashboardAdminAuthService = dashboardAdminAuthService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DashboardAdminLoginRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Login request is required" });
            }

            var result = await _dashboardAdminAuthService.LoginAsync(request.Username, request.Password);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid dashboard admin username or password" });
            }

            return Ok(result);
        }
    }
}