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

        private int? GetCurrentRegNo()
        {
            var claim = User.FindFirst("RegNo")?.Value;
            return int.TryParse(claim, out int regNo) ? regNo : null;
        }

        // Monthly leaf collection for logged-in supplier
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMyMonthlyCollection([FromQuery] int year, [FromQuery] int month)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var result = await _leafService.GetMonthlyLeafCollectionAsync(regNo.Value, year, month);
            return Ok(result);
        }

        // 6-month history for logged-in supplier
        [HttpGet("history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var result = await _leafService.GetSixMonthHistoryAsync(regNo.Value);
            return Ok(result);
        }

        // NEW — 12-month history for logged-in supplier
        [HttpGet("history-annual")]
        public async Task<IActionResult> GetMyAnnualHistory()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var result = await _leafService.GetTwelveMonthHistoryAsync(regNo.Value);
            return Ok(result);
        }

        // Today's collection for logged-in supplier
        [HttpGet("today")]
        public async Task<IActionResult> GetMyTodayCollection()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var result = await _leafService.GetTodayLeafCollectionAsync(regNo.Value);
            return Ok(result);
        }

        // Current month total for logged-in supplier
        [HttpGet("summary")]
        public async Task<IActionResult> GetMyCurrentMonthSummary()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var total = await _leafService.GetCurrentMonthTotalAsync(regNo.Value);
            return Ok(new { totalNet = total });
        }

        // Diagnostic — shows raw DB data to debug zero-value issues
        [HttpGet("debug")]
        public async Task<IActionResult> GetDebug()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null) return Unauthorized();

            var result = await _leafService.GetDiagnosticsAsync(regNo.Value);
            return Ok(result);
        }
    }
}