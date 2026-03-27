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

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var news = await _newsService.GetActiveNewsAsync(regNo);
            return Ok(news);
        }

        [HttpGet("active-popup")]
        public async Task<IActionResult> GetActivePopupNews()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var popupNews = await _newsService.GetActivePopupNewsAsync(regNo);
            return Ok(popupNews);
        }

        [HttpPost("{id}/dismiss")]
        public async Task<IActionResult> DismissNews(int id)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            await _newsService.DismissNewsAsync(id, regNo);
            return Ok(new { message = "News dismissed" });
        }
    }
}