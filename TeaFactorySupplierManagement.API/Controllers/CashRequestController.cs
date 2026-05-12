using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CashRequestController : ControllerBase
    {
        private readonly ICashRequestService _cashRequestService;

        public CashRequestController(ICashRequestService cashRequestService)
        {
            _cashRequestService = cashRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            var requests = await _cashRequestService.GetRequestsByRegNoAsync(regNo);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _cashRequestService.GetRequestByIdAsync(id);
            if (request == null)
                return NotFound();
            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateCashRequestDto request)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            try
            {
                var result = await _cashRequestService.CreateRequestAsync(regNo, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateCashRequestDto request)
        {
            var result = await _cashRequestService.UpdateRequestStatusAsync(
                id,
                request.Status ?? "pending",
                request.ApprovedBy,
                request.Remarks);

            if (!result)
                return NotFound();

            return Ok(new { message = "Request updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var result = await _cashRequestService.DeleteRequestAsync(id);
            if (!result)
                return NotFound();
            return Ok(new { message = "Request deleted successfully" });
        }

        [HttpGet("feature-enabled")]
        public async Task<IActionResult> IsFeatureEnabled()
        {
            var isEnabled = await _cashRequestService.IsCashRequestEnabledAsync();
            return Ok(new { isEnabled });
        }

        [HttpGet("advance-limit")]
        public async Task<IActionResult> GetAdvanceLimit()
        {
            var limit = await _cashRequestService.GetAdvanceLimitAsync();
            return Ok(new { limit });
        }
    }
}