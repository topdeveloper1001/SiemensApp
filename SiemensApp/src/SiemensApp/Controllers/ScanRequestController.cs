using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiemensApp.Domain;
using SiemensApp.Dto;
using SiemensApp.Entities;
using SiemensApp.Filters;
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
    [ServiceFilter(typeof(SiemensActionFilter))]
    public class ScanRequestController : Controller
    {
        private readonly SiemensDbContext _context;
        private readonly IScanRequestService _scanRequestService;
        private readonly ILogger<ScanRequestController> _logger;
        private readonly ISiteConfigurationService _siteConfigurationService;
        public ScanRequestController(ISiteConfigurationService siteConfigurationService, SiemensDbContext context, IScanRequestService scanRequestService, ILogger<ScanRequestController> logger)
        {
            _siteConfigurationService = siteConfigurationService;
            _context = context;
            _scanRequestService = scanRequestService;
            _logger = logger;
        }

        public IActionResult Index([FromQuery(Name = "siteId")] Guid siteId)
        {
            return View();
        }

        public ActionResult<DataSourceResult> ScanRequest_Read([DataSourceRequest] DataSourceRequest request, [FromQuery] Guid siteId)
        {
            var res = _scanRequestService.GetAllBySiteIdAsync(siteId).Result;
            return res.ToDataSourceResult(request);
        }
        public IActionResult Scan([FromQuery] Guid siteId)
        {
            var sc = _siteConfigurationService.GetSiteConfiguration(siteId);

            _scanRequestService.Scan(ScanRequestDto.Create(sc.SiteId)).Wait();
            return View("Index");
        }
    }

}