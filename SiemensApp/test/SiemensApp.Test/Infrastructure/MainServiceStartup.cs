using MaxKagamine.Moq.HttpClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Willow.Infrastructure.Services;
using Willow.Tests.Infrastructure.MockServices;

namespace Willow.Tests.Infrastructure
{
    public class MainServiceStartup<TOriginalStartup> where TOriginalStartup : class
    {
        private readonly TOriginalStartup _startup;

        public MainServiceStartup(IServiceProvider serviceProvider)
        {
            _startup = serviceProvider.CreateInstance<TOriginalStartup>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.InvokeMethod(
                _startup,
                "ConfigureServices",
                new Dictionary<Type, object>() { [typeof(IServiceCollection)] = services });

            var assembly = typeof(TOriginalStartup).Assembly;
            services.AddApplicationPart(assembly);

            var serverFixtureConfiguration = serviceProvider.GetRequiredService<ServerFixtureConfiguration>();

            if (serverFixtureConfiguration.EnableTestAuthentication)
            {
                services.AddTestAuthentication();
            }

            var httpHandler = new Mock<HttpMessageHandler>();
            services.AddSingleton(httpHandler);
            var httpClientFactory = httpHandler.CreateClientFactory();
            services.AddSingleton(typeof(IHttpClientFactory), httpClientFactory);
            var mockHttpClientFactory = Mock.Get(httpClientFactory);
            foreach (var dependencyService in serverFixtureConfiguration.DependencyServices)
            {
                var baseUrl = $"https://{dependencyService.Name}.com";
                mockHttpClientFactory
                    .Setup(x => x.CreateClient(dependencyService.Name))
                    .Returns(() =>
                    {
                        var client = httpHandler.CreateClient();
                        client.BaseAddress = new Uri(baseUrl);
                        return client;
                    });
                if (dependencyService.Setup != null)
                {
                    var handler = new DependencyServiceHttpHandler(httpHandler, baseUrl);
                    dependencyService.Setup(handler);
                }
            }

            services.ReplaceSingleton<IDateTimeService, MockDateTimeService>();

            serverFixtureConfiguration.MainServicePostConfigureServices?.Invoke(services);
        }

        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            // TestServer does not return 500 when an internal exception pops up, it passes the exception to the caller.
            // Add this middleware to simulate a real server behavior: returns status code 500.
            app.UseMiddleware<ExceptionMiddleware>();

            serviceProvider.InvokeMethod(
                _startup,
                "Configure",
                new Dictionary<Type, object>() { [typeof(IApplicationBuilder)] = app });
        }

        public class ExceptionMiddleware
        {
            private readonly RequestDelegate _next;
            private readonly ILogger<ExceptionMiddleware> _logger;

            public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
            {
                _next = next;
                _logger = logger;
            }

            public async Task Invoke(HttpContext httpContext)
            {
                try
                {
                    await _next(httpContext);
                }
                catch (Exception ex)
                {
                    httpContext.Response.ContentType = MediaTypeNames.Text.Plain;
                    httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    _logger.LogError(ex, "Internal server error");
                    await httpContext.Response.WriteAsync("Internal server error: " + ex.ToString());
                }
            }
        }
    }

}
