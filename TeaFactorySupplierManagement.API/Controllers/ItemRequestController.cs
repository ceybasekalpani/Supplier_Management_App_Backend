using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ItemRequestController : ControllerBase
    {
        private readonly IItemRequestService _itemRequestService;

        public ItemRequestController(IItemRequestService itemRequestService)
        {
            _itemRequestService = itemRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            var requests = await _itemRequestService.GetRequestsByRegNoAsync(regNo);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var request = await _itemRequestService.GetRequestByIdAsync(id);
            if (request == null)
                return NotFound();
            return Ok(request);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateItemRequestDto request)
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            if (string.IsNullOrEmpty(regNoClaim) || !int.TryParse(regNoClaim, out int regNo))
                return Unauthorized();

            try
            {
                var result = await _itemRequestService.CreateRequestAsync(regNo, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateItemRequestDto request)
        {
            var result = await _itemRequestService.UpdateRequestStatusAsync(
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
            var result = await _itemRequestService.DeleteRequestAsync(id);
            if (!result)
                return NotFound();
            return Ok(new { message = "Request deleted successfully" });
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetItemTypes()
        {
            var types = await _itemRequestService.GetItemTypesAsync();
            return Ok(types);
        }

        [HttpGet("feature-enabled")]
        public async Task<IActionResult> IsFeatureEnabled()
        {
            var isEnabled = await _itemRequestService.IsItemRequestEnabledAsync();
            return Ok(new { isEnabled });
        }
    }
}