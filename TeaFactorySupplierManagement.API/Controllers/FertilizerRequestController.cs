using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FertilizerRequestController : ControllerBase
    {
        private readonly IFertilizerRequestService _fertilizerRequestService;

        public FertilizerRequestController(IFertilizerRequestService fertilizerRequestService)
        {
            _fertilizerRequestService = fertilizerRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            var requests = await _fertilizerRequestService.GetRequestsByRegNoAsync(regNo);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _fertilizerRequestService.GetRequestByIdAsync(id);
            if (request == null)
                return NotFound();

            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateFertilizerRequestDto request)
        {
            var regNo = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNo))
                return Unauthorized();

            try
            {
                var result = await _fertilizerRequestService.CreateRequestAsync(regNo, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var result = await _fertilizerRequestService.DeleteRequestAsync(id);

            if (!result)
                return NotFound();

            return Ok(new { message = "Request deleted successfully" });
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetFertilizerTypes()
        {
            var types = await _fertilizerRequestService.GetFertilizerTypesAsync();
            return Ok(types);
        }

        [HttpGet("feature-enabled")]
        public async Task<IActionResult> IsFeatureEnabled()
        {
            var isEnabled = await _fertilizerRequestService.IsFertilizerRequestEnabledAsync();
            return Ok(new { isEnabled });
        }
    }
}