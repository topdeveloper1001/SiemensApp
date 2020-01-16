using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    context.SystemObjects.Add(SystemObject);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                _dbContext.SystemObjects.Add(SystemObject);
                await _dbContext.SaveChangesAsync();
            }
        }

        
    }
}
