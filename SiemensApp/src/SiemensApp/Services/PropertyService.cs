using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SiemensApp.Domain;
using SiemensApp.Entities;
using Willow.Infrastructure.Exceptions;

namespace SiemensApp.Services
{
    public interface IPropertyService
    {
        Task CreateProperty(Property property);
        Task<Property> SaveProperty(Property property);
        Task DeleteProperty(int propertyId);
        List<Property> GetProperties(Guid siteId, bool isFunctionProperty);
    }

    public class PropertyService : IPropertyService
    {
        private readonly SiemensDbContext _dbContext;
        private readonly ILogger<IPropertyService> _logger;

        public PropertyService(SiemensDbContext dbContext, ILogger<IPropertyService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateProperty(Property property)
        {
            _dbContext.Properties.Add(PropertyEntity.MapFrom(property));
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteProperty(int propertyId)
        {
            var entity = _dbContext.Properties.Find(propertyId);
            _dbContext.Properties.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Property> SaveProperty(Property property)
        {
            var entity = PropertyEntity.MapFrom(property);
            if (_dbContext.Properties.Any(p => p.Id == property.Id))
            {
                _dbContext.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            else
            {
                throw new BadRequestException("The property to update is not exist");
            }

            
            await _dbContext.SaveChangesAsync();

            return PropertyEntity.MapTo(entity);

        }

        List<Property> IPropertyService.GetProperties(Guid siteId, bool isFunctionProperty)
        {
            return _dbContext.Properties.Where(x => x.SiteId == siteId && x.IsFunctionProperty == isFunctionProperty).Select(x => PropertyEntity.MapTo(x)).ToList();
        }
    }
}
