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
    public class SiteConfigurationController : Controller
    {
        private readonly SiemensDbContext _context;
        private readonly ISiteConfigurationService _siteConfigurationService;
        private readonly ILogger<SiteConfigurationController> _logger;

        public SiteConfigurationController(SiemensDbContext context, ISiteConfigurationService siteConfigurationService, ILogger<SiteConfigurationController> logger)
        {
            _context = context;
            _siteConfigurationService = siteConfigurationService;
            _logger = logger;
        }

        public IActionResult Index([FromQuery(Name = "siteId")] Guid siteId)
        {
            return View(SiteConfigurationDto.Create(siteId));
        }

        [HttpPost]
        public IActionResult Save(SiteConfigurationDto dto)
        {
            return View("Index", dto);
        }

        
    }

}