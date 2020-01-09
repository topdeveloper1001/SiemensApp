using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SiemensApp.Dto;
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Produces("application/json")]
    public class SiteConfigurationController : ControllerBase
    {
        private readonly ISiteConfigurationService _siteConfigurationService;

        public SiteConfigurationController(ISiteConfigurationService siteConfigurationService)
        {
            _siteConfigurationService = siteConfigurationService;
        }

        [HttpPost("siteConfiguration/create")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> CreateSiteConfiguration([FromBody] SiteConfigurationDto request)
        {
            await _siteConfigurationService.CreateSiteConfiguration(SiteConfigurationDto.MapTo(request));
            return NoContent();
        }
    }
}