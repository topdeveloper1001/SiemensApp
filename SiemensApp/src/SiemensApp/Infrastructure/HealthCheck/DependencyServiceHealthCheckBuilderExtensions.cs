using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyServiceHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddDependencyService(this IHealthChecksBuilder builder, IConfiguration configurationRoot, string serviceName, HealthStatus? failureStatus = null)
        {
            var services = builder.Services.BuildServiceProvider();
            var environment = services.GetRequiredService<IHostingEnvironment>();
            if (environment.IsEnvironment("Test"))
            {
                return builder;
            }
            var configuration = services.GetRequiredService<IConfiguration>();
            var serviceBaseUrl = configuration.GetValue<string>($"HttpClientFactory:{serviceName}:BaseAddress");
            var healthCheckUri = new Uri(new Uri(serviceBaseUrl), "healthcheck");
            return builder.AddUrlGroup(healthCheckUri, $"Service:{serviceName}", failureStatus);
        }
    }
}