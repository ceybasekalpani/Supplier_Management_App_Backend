using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected string? GetCurrentRegNo()
        {
            return User.FindFirst("RegNo")?.Value ?? HttpContext.Items["RegNo"] as string;
        }

        protected string? GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? HttpContext.Items["Username"] as string;
        }

        protected bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetCurrentRegNo());
        }

        protected IActionResult HandleError(Exception ex)
        {
            // Log exception here
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }
}