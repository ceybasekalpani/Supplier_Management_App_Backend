using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class DashboardUsersController : ControllerBase
    {
        private readonly IDashboardUserService _dashboardUserService;

        public DashboardUsersController(IDashboardUserService dashboardUserService)
        {
            _dashboardUserService = dashboardUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? status)
        {
            var result = await _dashboardUserService.GetUsersAsync(search, status);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateDashboardUserDto request)
        {
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

        private int? GetCurrentAdminId()
        {
            var claim = User.FindFirst("DashboardAdminId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
