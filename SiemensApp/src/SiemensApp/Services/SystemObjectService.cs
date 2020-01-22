using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiemensApp.Domain;
using SiemensApp.Entities;
using Willow.Infrastructure.Exceptions;

namespace SiemensApp.Services
{
    public interface ISystemObjectService
    {
        Task CreateSystemObject(bool bMultiThread, SystemObjectEntity SystemObject);
        Task AddPropertiesSystemObject(bool bMultiThread, Guid id, string properties, string functionProperties);
    }

    public class SystemObjectService : ISystemObjectService
    {
        private readonly SiemensDbContext _dbContext;
        private readonly ILogger<ISystemObjectService> _logger;
        private readonly IServiceScopeFactory _scope;
        public SystemObjectService(IServiceScopeFactory scope, SiemensDbContext dbContext, ILogger<ISystemObjectService> logger)
        {
            _scope = scope;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task CreateSystemObject(bool bMultiThread, SystemObjectEntity SystemObject)
        {
            if (bMultiThread)
            {
                using (var context = _scope.CreateScope().ServiceProvider.GetService<SiemensDbContext>())
                {
                    await Create(context, SystemObject);
                    
                }
            }
            else
            {
                await Create(_dbContext, SystemObject);
            }
        }
        private async Task Create(SiemensDbContext context, SystemObjectEntity SystemObject)
        {
            var existing = context.SystemObjects.FirstOrDefault(x => x.ObjectId == SystemObject.ObjectId && x.Name == SystemObject.Name && x.Designation == SystemObject.Designation);
            if (existing != null)
            {
                SystemObject.Id = existing.Id;
                context.Entry(SystemObject).State = EntityState.Modified;
            }
            else
            {
                context.SystemObjects.Add(SystemObject);
            }
            await context.SaveChangesAsync();
        }
        public async Task AddPropertiesSystemObject(bool bMultiThread, Guid id, string properties, string functionProperties)
        {
            if (bMultiThread)
            {
                using (var context = _scope.CreateScope().ServiceProvider.GetService<SiemensDbContext>())
                {
                    await AddProperties(context, id, properties, functionProperties);                    
                }
            }
            else
            {
                await AddProperties(_dbContext, id, properties, functionProperties);
            }
        }
        private async Task AddProperties(SiemensDbContext context, Guid id, string properties, string functionProperties)
        {
            var entry = context.SystemObjects.Find(id);
            entry.Attributes = properties;
            entry.FunctionProperties = functionProperties;
            context.Entry(entry).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }


    }
}
