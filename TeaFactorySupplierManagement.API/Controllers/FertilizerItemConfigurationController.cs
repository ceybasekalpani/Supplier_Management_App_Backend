using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "DashboardAdmin")]
    public class FertilizerItemConfigurationController : ControllerBase
    {
        private readonly IFertilizerItemConfigurationService _configurationService;

        public FertilizerItemConfigurationController(IFertilizerItemConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = true)
        {
            var result = await _configurationService.GetAllAsync(includeInactive);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFertilizerItemConfigurationDto request)
        {
            try
            {
                var result = await _configurationService.CreateAsync(request.Category, request.Name);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{category}/{id:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] string category,
            [FromRoute] int id,
            [FromBody] UpdateFertilizerItemConfigurationDto request)
        {
            try
            {
                var result = await _configurationService.UpdateAsync(category, id, request.Name, request.IsActive);

                if (result == null)
                {
                    return NotFound(new { message = "Configuration item not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPatch("{category}/{id:int}/active")]
        public async Task<IActionResult> SetActive(
            [FromRoute] string category,
            [FromRoute] int id,
            [FromBody] UpdateFertilizerItemConfigurationDto request)
        {
            try
            {
                var result = await _configurationService.SetActiveAsync(category, id, request.IsActive ?? true);

                if (result == null)
                {
                    return NotFound(new { message = "Configuration item not found." });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
