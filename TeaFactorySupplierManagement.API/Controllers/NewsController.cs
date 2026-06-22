using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        private int? GetCurrentRegNo()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
            {
                return null;
            }
            return regNo;
        }

        [HttpGet]
        [HttpGet("/api/mobile/news")]
        public async Task<IActionResult> GetNews()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var news = await _newsService.GetActiveNewsAsync(regNo.Value);
            return Ok(news);
        }

        [HttpGet("{id:int}")]
        [HttpGet("/api/mobile/news/{id:int}")]
        public async Task<IActionResult> GetNewsById([FromRoute] int id)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var news = await _newsService.GetNewsByIdAsync(id, regNo.Value);
            if (news == null)
                return NotFound(new { message = "News not found" });

            return Ok(news);
        }

        [HttpGet("active-popup")]
        [HttpGet("/api/mobile/news/active-popup")]
        public async Task<IActionResult> GetActivePopupNews()
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            var popupNews = await _newsService.GetActivePopupNewsAsync(regNo.Value);
            return Ok(popupNews);
        }

        [HttpPost("{id}/dismiss")]
        [HttpPost("/api/mobile/news/{id:int}/dismiss")]
        public async Task<IActionResult> DismissNews(int id)
        {
            var regNo = GetCurrentRegNo();
            if (regNo == null)
                return Unauthorized(new { message = "Invalid or missing registration number" });

            await _newsService.DismissNewsAsync(id, regNo.Value);
            return Ok(new { message = "News dismissed successfully" });
        }
    }
}
