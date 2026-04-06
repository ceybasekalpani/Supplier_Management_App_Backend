using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeafController : ControllerBase
    {
        private readonly ILeafService _leafService;

        public LeafController(ILeafService leafService)
        {
            _leafService = leafService;
        }                                                                                                                                                                                                                                  

        [HttpGet("monthly/{regNo}/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyCollection(int regNo, int year, int month)
        {
            var result = await _leafService.GetMonthlyLeafCollectionAsync(regNo, year, month);
            return Ok(result);
        }

        [HttpGet("history/{regNo}")]
        public async Task<IActionResult> GetHistory(int regNo)
        {
            var result = await _leafService.GetSixMonthHistoryAsync(regNo);
            return Ok(result);
        }

        [HttpGet("today/{regNo}")]
        public async Task<IActionResult> GetTodayCollection(int regNo)
        {
            var result = await _leafService.GetTodayLeafCollectionAsync(regNo);
            return Ok(result);
        }

        [HttpGet("summary/{regNo}")]
        public async Task<IActionResult> GetCurrentMonthSummary(int regNo)
        {
            var total = await _leafService.GetCurrentMonthTotalAsync(regNo);
            return Ok(new { totalNet = total });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyMonthlyCollection([FromQuery] int year, [FromQuery] int month)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return Unauthorized();
            }

            var result = await _leafService.GetMonthlyLeafCollectionAsync(regNo, year, month);
            return Ok(result);
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return Unauthorized();
            }

            var result = await _leafService.GetSixMonthHistoryAsync(regNo);
            return Ok(result);
        }

        [HttpGet("my-today")]
        public async Task<IActionResult> GetMyTodayCollection()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return Unauthorized();
            }

            var result = await _leafService.GetTodayLeafCollectionAsync(regNo);
            return Ok(result);
        }

        [HttpGet("my-summary")]
        public async Task<IActionResult> GetMyCurrentMonthSummary()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return Unauthorized();
            }

            var total = await _leafService.GetCurrentMonthTotalAsync(regNo);
            return Ok(new { totalNet = total });
        }
    }
}