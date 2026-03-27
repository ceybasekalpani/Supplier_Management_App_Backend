using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaFactorySupplierManagement.Application.Interfaces;

namespace TeaFactorySupplierManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FactoryController : ControllerBase
    {
        private readonly IFactorySetupService _factorySetupService;

        public FactoryController(IFactorySetupService factorySetupService)
        {
            _factorySetupService = factorySetupService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetFactorySettings()
        {
            var settings = await _factorySetupService.GetFactorySetupAsync();
            return Ok(settings);
        }

        [HttpGet("features")]
        public async Task<IActionResult> GetFactoryFeatures()
        {
            var features = await _factorySetupService.GetFactoryFeaturesAsync();
            return Ok(features);
        }
    }
}