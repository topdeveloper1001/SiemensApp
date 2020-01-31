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

        public ActionResult Property_Create([DataSourceRequest]DataSourceRequest request, [Bind(Prefix = "models")]IEnumerable<Property> properties, [FromQuery] Guid siteId)
        {
            // Will keep the inserted entities here. Used to return the result later.
            var entities = new List<Property>();
            
            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    property.SiteId = siteId;
                    property.IsFunctionProperty = false;
                    _propertyService.CreateProperty(property);
                    entities.Add(property);
                }
                
            }
            
            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
        public ActionResult Property_Update([DataSourceRequest]DataSourceRequest request, [Bind(Prefix = "models")]IEnumerable<Property> properties, [FromQuery] Guid siteId)
        {
            // Will keep the inserted entities here. Used to return the result later.
            var entities = new List<Property>();

            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    _propertyService.SaveProperty(property);
                    entities.Add(property);
                }

            }

            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
        public ActionResult Property_Destroy([DataSourceRequest]DataSourceRequest request, [Bind(Prefix = "models")]IEnumerable<Property> properties, [FromQuery] Guid siteId)
        {
            // Will keep the inserted entities here. Used to return the result later.
            var entities = new List<Property>();

            if (ModelState.IsValid)
            {
                foreach (var property in properties)
                {
                    _propertyService.DeleteProperty(property.Id);
                    entities.Add(property);
                }

            }

            return Json(entities.ToDataSourceResult(request, ModelState, property => property));
        }
    }

}