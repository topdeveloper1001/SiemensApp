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
using SiemensApp.Filters;
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
    [ServiceFilter(typeof(SiemensActionFilter))]
    public class SystemObjectController : Controller
    {
        private readonly SiemensDbContext _context;
        private readonly IScanRequestService _scanRequestService;
        private readonly ILogger<SystemObjectController> _logger;

        public SystemObjectController(SiemensDbContext context, IScanRequestService scanRequestService, ILogger<SystemObjectController> logger)
        {
            _context = context;
            _scanRequestService = scanRequestService;
            _logger = logger;
        }

        public IActionResult Index([FromQuery(Name = "siteId")] Guid siteId)
        {
            return View();
        }

        public async Task<IActionResult> Import([FromQuery(Name = "siteId")] Guid siteId)
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
            
            return await currentLevel.ToDataSourceResultAsync(request);
        }

        [HttpGet("/api/propertyValues/{objectId}/{propertyId?}")]
        public async Task<ActionResult<PropertyValueResponse>> GetValue([FromRoute] string objectId, [FromRoute] string propertyId, [FromQuery(Name = "siteId")] Guid siteId)
        {
            return await _scanRequestService.GetPropertyValueAsync(objectId, propertyId);
        }

        [HttpGet("/api/csvExport")]
        public async Task<IActionResult> GetCsvExport([FromQuery(Name = "siteId")] Guid siteId)
        {
            var filePath = await _scanRequestService.ExportDataCsv(siteId);

            var stream = System.IO.File.Open(filePath, FileMode.Open);

            return File(stream, "text/csv", "export.csv");
        }

        [HttpGet("/api/csvExportProperties")]
        public async Task<IActionResult> GetCsvExportProperties([FromQuery(Name = "siteId")] Guid siteId)
        {
            var filePath = await _scanRequestService.ExportDataCsvProperties(siteId);

            var stream = System.IO.File.Open(filePath, FileMode.Open);

            return File(stream, "text/csv", "exportProperties.csv");
        }

        [HttpGet("/api/csvExportFunctionProperties")]
        public async Task<IActionResult> GetCsvExportFunctionProperties([FromQuery(Name = "siteId")] Guid siteId)
        {
            var filePath = await _scanRequestService.ExportDataCsvFunctionProperties(siteId);

            var stream = System.IO.File.Open(filePath, FileMode.Open);

            return File(stream, "text/csv", "exportFunctionProperties.csv");
        }
    }

}