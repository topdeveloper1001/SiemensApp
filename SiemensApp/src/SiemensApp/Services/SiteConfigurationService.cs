using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SiemensApp.Domain;
using SiemensApp.Entities;
using Willow.Infrastructure.Exceptions;

namespace SiemensApp.Services
{
    public interface ISiteConfigurationService
    {
        Task CreateSiteConfiguration(SiteConfiguration siteConfiguration);
        SiteConfiguration GetSiteConfiguration(Guid siteId);
        SiteConfiguration GetSiteConfiguration();
    }

    public class SiteConfigurationService : ISiteConfigurationService
    {
        private readonly SiemensDbContext _dbContext;
        private readonly ILogger<ISiteConfigurationService> _logger;

        public SiteConfigurationService(SiemensDbContext dbContext, ILogger<ISiteConfigurationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateSiteConfiguration(SiteConfiguration siteConfiguration)
        {
            if (_dbContext.SiteConfigurations.Any(sc => sc.SiteId == siteConfiguration.SiteId))
                throw new BadRequestException("SiteConfiguration already exists");

            var passwordBytes = Encoding.UTF8.GetBytes(siteConfiguration.Password);
            siteConfiguration.Password = Convert.ToBase64String(passwordBytes);
            _dbContext.SiteConfigurations.Add(SiteConfigurationEntity.MapFrom(siteConfiguration));
            await _dbContext.SaveChangesAsync();
        }

        public SiteConfiguration GetSiteConfiguration(Guid siteId)
        {
            var siteconfiguration = _dbContext.SiteConfigurations.FirstOrDefault(sc => sc.SiteId == siteId);
            if(siteconfiguration == null)
                throw new BadRequestException("SiteConfiguration does not exist");
            return SiteConfigurationEntity.MapTo(siteconfiguration);
        }
        public SiteConfiguration GetSiteConfiguration()
        {
            var siteconfiguration = _dbContext.SiteConfigurations.FirstOrDefault();
            if (siteconfiguration == null)
                throw new BadRequestException("SiteConfiguration does not exist");
            return SiteConfigurationEntity.MapTo(siteconfiguration);
        }
    }
}
