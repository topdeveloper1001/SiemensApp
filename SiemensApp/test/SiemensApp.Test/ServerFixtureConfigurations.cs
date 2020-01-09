using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Willow.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using SiemensApp;
using SiemensApp.Database;
using SiemensApp.Entities;

namespace Workflow.Tests
{
    public class ServerFixtureConfigurations
    {
        public static readonly ServerFixtureConfiguration SqlServer = new ServerFixtureConfiguration
        {
            EnableTestAuthentication = true,
            StartupType = typeof(Startup),
            MainServicePostAppConfiguration = (configuration, testContext) =>
            {
                var databaseName = $"Test_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                var configurationValues = new Dictionary<string, string>
                {
                    ["ConnectionStrings:SiemensDb"] = testContext.DatabaseFixture.GetConnectionString(databaseName),
                };
                configuration.AddInMemoryCollection(configurationValues);
            },
            MainServicePostConfigureServices = (services) =>
            {
            }
        };

        public static readonly ServerFixtureConfiguration InMemoryDb = new ServerFixtureConfiguration
        {
            EnableTestAuthentication = true,
            StartupType = typeof(Startup),
            MainServicePostAppConfiguration = (configuration, testContext) =>
            {
                if (TestEnvironment.UseInMemoryDatabase == false)
                {
                    testContext.Output.WriteLine("Force to use database instead of in-memory database");
                    var databaseName = $"Test_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    var configurationValues = new Dictionary<string, string>
                    {
                        ["ConnectionStrings:SiemensDb"] = testContext.DatabaseFixture.GetConnectionString(databaseName),
                    };
                    configuration.AddInMemoryCollection(configurationValues);
                }
            },
            MainServicePostConfigureServices = (services) =>
            {
                if (TestEnvironment.UseInMemoryDatabase)
                {
                    var databaseName = $"Test_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    services.ReplaceScoped(GetInMemoryOptions<SiemensDbContext>(databaseName));
                    services.ReplaceScoped<IDbUpgradeChecker>(_ => new InMemoryDbUpgradeChecker());
                }
            },
            DependencyServices = new List<DependencyServiceConfiguration>
            {
                new DependencyServiceConfiguration
                {
                    Name = "TripPin"
                }
            }
        };

        public static Func<IServiceProvider, DbContextOptions<T>> GetInMemoryOptions<T>(string dbName) where T : DbContext
        {
            return (_) =>
            {
                var builder = new DbContextOptionsBuilder<T>();
                builder.UseInMemoryDatabase(databaseName: dbName);
                return builder.Options;
            };
        }

        public class InMemoryDbUpgradeChecker : IDbUpgradeChecker
        {
            public void EnsureDatabaseUpToDate(IHostingEnvironment env)
            {
                // Do nothing
            }
        }

    }
}
