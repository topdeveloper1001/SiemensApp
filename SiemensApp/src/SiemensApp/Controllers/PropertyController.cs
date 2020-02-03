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
using Newtonsoft.Json;
using SiemensApp.Domain;
using SiemensApp.Dto;
using SiemensApp.Entities;
using SiemensApp.Filters;
using SiemensApp.Services;

namespace SiemensApp.Controllers
{
    [ServiceFilter(typeof(SiemensActionFilter))]
    public class PropertyController : Controller
    {
        private readonly SiemensDbContext _context;
        private readonly IPropertyService _propertyService;
        private readonly ILogger<PropertyController> _logger;
        private readonly ISiteConfigurationService _siteConfigurationService;
        public PropertyController(ISiteConfigurationService siteConfigurationService, SiemensDbContext context, IPropertyService propertyService, ILogger<PropertyController> logger)
        {
            _siteConfigurationService = siteConfigurationService;
            _context = context;
            _propertyService = propertyService;
            _logger = logger;
        }

        public IActionResult Index([FromQuery(Name = "siteId")] Guid siteId)
        {
            return View();
        }

        public ActionResult<DataSourceResult> Property_Read([DataSourceRequest] DataSourceRequest request, [FromQuery] Guid siteId)
        {
            var res = _propertyService.GetProperties(siteId, false);
            return res.ToDataSourceResult(request);
        }
        public ActionResult<DataSourceResult> FunctionProperty_Read([DataSourceRequest] DataSourceRequest request, [FromQuery] Guid siteId)
        {
            var res = _propertyService.GetProperties(siteId, true);
            return res.ToDataSourceResult(request);
        }

        public async Task<ActionResult> Property_Create([DataSourceRequest]DataSourceRequest request, [FromForm] string models, [FromQuery] Guid siteId)
        {
            // Will keep the inserted entities here. Used to return the result later.
            var properties = string.IsNullOrEmpty(models) ? new List<Property>() : JsonConvert.DeserializeObject<List<Property>>(models);
            var entities = new List<Property>();
            
            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    await _propertyService.CreatePropertyAsync(property);
                    entities.Add(property);
                }
                
            }
            
            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
        public async Task<ActionResult> Property_Update([DataSourceRequest]DataSourceRequest request, [FromForm] string models, [FromQuery] Guid siteId)
        {
            var properties = string.IsNullOrEmpty(models) ? new List<Property>() : JsonConvert.DeserializeObject<List<Property>>(models);
            // Will keep the inserted entities here. Used to return the result later.
            var entities = new List<Property>();

            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    await _propertyService.SavePropertyAsync(property);
                    entities.Add(property);
                }

            }

            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
        public async Task<ActionResult> Property_Destroy([DataSourceRequest]DataSourceRequest request, [FromForm] string models, [FromQuery] Guid siteId)
        {
            var properties = string.IsNullOrEmpty(models) ? new List<Property>() : JsonConvert.DeserializeObject<List<Property>>(models);
            // Will keep the inserted entities here. Used to return the result later.
            var entities = new List<Property>();

            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    await _propertyService.DeletePropertyAsync(property.Id);
                    entities.Add(property);
                }

            }

            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
    }

}