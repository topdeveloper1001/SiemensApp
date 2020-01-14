using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SiemensApp.Database;
using SiemensApp.Domain;
using SiemensApp.Entities;
using SiemensApp.Infrastructure.Queue;
using SiemensApp.Services;
using System.IO;

namespace SiemensApp
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _env = env;
            _loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApiServices(Configuration, _env);
            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));
            services.AddScoped<ISiteConfigurationService, SiteConfigurationService>();
            services.AddScoped<IScanRequestService, ScanRequestService>();
            services.AddMemoryCache();
            services.AddSingleton<IApiTokenProvider, ApiTokenProvider>();
            services.AddHttpClient();
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            services.AddJwtAuthentication(Configuration["Auth0:Domain"], Configuration["Auth0:Audience"], _env);
            var connectionString = Configuration.GetConnectionString("SiemensDb");
            AddDbContexts(services, connectionString);
            //services.AddSingleton<IDbUpgradeChecker, DbUpgradeChecker>();

            services
                .AddHealthChecks()
                .AddDbContextCheck<SiemensDbContext>()
                .AddAssemblyVersion();

            
        }

        private void AddDbContexts(IServiceCollection services, string connectionString)
        {
            void contextOptions(DbContextOptionsBuilder o)
            {
                o.UseSqlServer(connectionString);
            }

            services.AddDbContext<SiemensDbContext>(contextOptions);
        }

        //public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDbUpgradeChecker dbUpgradeChecker)
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecks("/healthcheck", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseAuthentication();
            app.UseApiServices(Configuration, env);
            //dbUpgradeChecker.EnsureDatabaseUpToDate(env);

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
         Path.Combine(Directory.GetCurrentDirectory(), @"Resources")),
                RequestPath = new PathString("/Resources")
            });
        }
    }
}
