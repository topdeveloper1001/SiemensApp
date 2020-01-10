using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SiemensApp.Domain;
using SiemensApp.Services;
using System;
namespace SiemensApp.Controllers
{
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Produces("application/json")]
    public class ImportController : ControllerBase
    {
        private readonly ISiteConfigurationService _siteConfigurationService;
        private readonly ImportService _importService;

        public ImportController(ISiteConfigurationService siteConfigurationService, ImportService importService)
        {
            _siteConfigurationService = siteConfigurationService;
            _importService = importService;
        }

        [HttpPost("import/{siteId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Import(Guid siteId)
        {
            var sc = _siteConfigurationService.GetSiteConfiguration(siteId);            
            
            await _importService.Import(AuthenticationOptions.Create(sc));            
            
            return NoContent();
        }
    }
}