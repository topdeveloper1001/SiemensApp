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
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
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

        public IActionResult Index()
        {
            return View();
        }

        
        
        public async Task<ActionResult<DataSourceResult>> ReadData([DataSourceRequest] DataSourceRequest request, [FromQuery] int? id)
        {
            var currentLevel = await _context.SystemObjects.Where(s => s.ParentId == id).ToListAsync();

            var ids = currentLevel.Select(o => o.Id).ToList();

            var children = await _context.SystemObjectsChildrenExists.Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Exist);

            foreach (var systemObject in currentLevel)
            {
                systemObject.HasChildren = children[systemObject.Id];
            }
            //var res = await currentLevel.ToDataSourceResultAsync(request);
            return await currentLevel.ToDataSourceResultAsync(request);
        }

        public ActionResult<DataSourceResult> ScanRequest_Read([DataSourceRequest] DataSourceRequest request)
        {
            var res = _scanRequestService.GetAllAsync().Result;
            return res.ToDataSourceResult(request);
        }
        public IActionResult Scan()
        {
            var sc = _siteConfigurationService.GetSiteConfiguration();

            _scanRequestService.Scan(ScanRequestDto.Create(sc.SiteId)).Wait();
            return View("Index");
        }
    }

}