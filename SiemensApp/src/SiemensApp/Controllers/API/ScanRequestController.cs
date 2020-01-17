using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SiemensApp.Domain;
using SiemensApp.Services;
using System;
using SiemensApp.Dto;

namespace SiemensApp.Api.Controllers
{
    [ApiController]
    [Route("api")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Produces("application/json")]
    public class ScanRequestController : ControllerBase
    {
        private readonly ISiteConfigurationService _siteConfigurationService;
        private readonly IScanRequestService _scanRequestService;

        public ScanRequestController(ISiteConfigurationService siteConfigurationService, IScanRequestService scanRequestService)
        {
            _siteConfigurationService = siteConfigurationService;
            _scanRequestService = scanRequestService;
        }

        [HttpPost("scanRequest/scan")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Scan([FromBody] ScanRequestDto request)
        {
            
            var sc = _siteConfigurationService.GetSiteConfiguration(request.SiteId);

            await _scanRequestService.Scan(ScanRequestDto.MapTo(request));

            return NoContent();
        }
    }
}