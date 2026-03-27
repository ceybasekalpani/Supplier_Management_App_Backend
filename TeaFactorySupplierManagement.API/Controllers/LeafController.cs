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
        public async Task<IActionResult> GetMonthlyCollection(string regNo, int year, int month)
        {
            var result = await _leafService.GetMonthlyLeafCollectionAsync(regNo, year, month);
            return Ok(result);
        }

        [HttpGet("history/{regNo}")]
        public async Task<IActionResult> GetHistory(string regNo)
        {
            var result = await _leafService.GetSixMonthHistoryAsync(regNo);
            return Ok(result);
        }

        [HttpGet("today/{regNo}")]
        public async Task<IActionResult> GetTodayCollection(string regNo)
        {
            var result = await _leafService.GetTodayLeafCollectionAsync(regNo);
            return Ok(result);
        }

        [HttpGet("summary/{regNo}")]
        public async Task<IActionResult> GetCurrentMonthSummary(string regNo)
        {
            var total = await _leafService.GetCurrentMonthTotalAsync(regNo);
            return Ok(new { totalNet = total });
        }
    }
}