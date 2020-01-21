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
using SiemensApp.Entities;
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly SiemensDbContext _context;
        private readonly IScanRequestService _scanRequestService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SiemensDbContext context, IScanRequestService scanRequestService, ILogger<HomeController> logger)
        {
            _context = context;
            _scanRequestService = scanRequestService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Import()
        {
            try
            {
                //await _scanRequestService.Import();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on importing data");
            }
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<ActionResult<DataSourceResult>> ReadData([DataSourceRequest] DataSourceRequest request, [FromQuery] int? id, [FromQuery] Guid siteId)
        {
            var currentLevel = await _context.SystemObjects.Where(s => s.ParentId == id && s.SiteId == siteId).ToListAsync();

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

        [HttpGet("/api/propertyValues/{objectId}/{propertyId?}")]
        public async Task<ActionResult<PropertyValueResponse>> GetValue([FromRoute] string objectId, [FromRoute] string propertyId)
        {
            return await _scanRequestService.GetPropertyValueAsync(objectId, propertyId);
        }

        [HttpGet("/api/csvExport")]
        public async Task<IActionResult> GetCsvExport()
        {
            var filePath = await _scanRequestService.ExportDataCsv();

            var stream = System.IO.File.Open(filePath, FileMode.Open);

            return File(stream, "text/csv", "export.csv");
        }
    }

}