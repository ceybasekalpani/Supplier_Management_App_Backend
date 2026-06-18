using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.MobileDeviceTokens;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/mobile/device-token")]
    [Authorize]
    public class MobileDeviceTokenController : ControllerBase
    {
        private readonly IMobileDeviceTokenService _mobileDeviceTokenService;

        public MobileDeviceTokenController(IMobileDeviceTokenService mobileDeviceTokenService)
        {
            _mobileDeviceTokenService = mobileDeviceTokenService;
        }

        [HttpPost]
        public async Task<IActionResult> SaveToken([FromBody] SaveMobileDeviceTokenDto request)
        {
            var regNo = GetCurrentRegNo() ?? request.RegNo ?? request.SupplierId;
            if (!regNo.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing registration number" });
            }

            try
            {
                await _mobileDeviceTokenService.SaveTokenAsync(regNo.Value, request);
                return Ok(new { message = "Device token saved successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteToken([FromBody] SaveMobileDeviceTokenDto request)
        {
            var regNo = GetCurrentRegNo() ?? request.RegNo ?? request.SupplierId;
            if (!regNo.HasValue)
            {
                return Unauthorized(new { message = "Invalid or missing registration number" });
            }

            await _mobileDeviceTokenService.DeactivateTokenAsync(regNo.Value, request.FcmToken);
            return Ok(new { message = "Device token removed successfully" });
        }

        private int? GetCurrentRegNo()
        {
            var regNoClaim = User.FindFirst("RegNo")?.Value;
            return int.TryParse(regNoClaim, out var regNo) ? regNo : null;
        }
    }
}
