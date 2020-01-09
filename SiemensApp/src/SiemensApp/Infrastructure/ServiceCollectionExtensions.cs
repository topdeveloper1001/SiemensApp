using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Willow.Infrastructure.Exceptions;
using Willow.Infrastructure.Services;
using Willow.Infrastructure.Swagger;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApiServices(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            JsonConvert.DefaultSettings = (() =>
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                settings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                return settings;
            });

            services.AddHttpContextAccessor();

            var httpClientConfiguration = configuration.GetSection("HttpClientFactory");
            foreach (var clientConfiguration in httpClientConfiguration.GetChildren())
            {
                var apiName = clientConfiguration.Key;
                services.AddHttpClient(apiName, (sv, client) =>
                {
                    client.BaseAddress = new Uri(clientConfiguration["BaseAddress"]);
                    var authenticationSection = clientConfiguration.GetSection("Authentication");
                    if (authenticationSection.GetChildren().Count() > 0)
                    {
                        if (authenticationSection["Scheme"] == "TokenFromCookie")
                        {
                            string token;
                            var authValue = sv.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Request?.Headers["Authorization"];
                            if (authValue.HasValue && authValue.Value.Count > 0)
                            {
                                token = authValue.Value[0].Substring("Bearer ".Length);
                            }
                            else
                            {
                                token = sv.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.User?.FindFirst(ClaimTypes.Authentication)?.Value;
                            }
                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            }
                        }
                        else if (authenticationSection["Scheme"] == "PassDownAuthorization")
                        {
                            var authValue = sv.GetRequiredService<IHttpContextAccessor>()?.HttpContext?.Request?.Headers["Authorization"];
                            if (authValue.HasValue && authValue.Value.Count > 0)
                            {
                                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authValue.Value[0]);
                            }
                        }
                        else
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                                authenticationSection["Scheme"],
                                authenticationSection["Parameter"]);
                        }
                    }
                });
            }

            services.AddCors();

            services.AddMvc(options => {
                    options.Filters.Add<GlobalExceptionFilter>();
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    opt.SerializerSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                });

            if (configuration.GetValue<bool>("EnableSwagger"))
            {
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new Info { Title = Assembly.GetEntryAssembly().GetName().Name, Version = "1" });
                    options.EnableAnnotations();
                    options.CustomSchemaIds(x => x.Name);

                    if (configuration.GetValue<string>("Auth0:ClientId") != null)
                    {
                        options.OperationFilter<SecurityRequirementsOperationFilter>();
                        var authServerUrl = $"https://{configuration.GetValue<string>("Auth0:Domain")}";
                        options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                        {
                            Type = "oauth2",
                            Flow = "implicit",
                            AuthorizationUrl = authServerUrl + "/authorize",
                            TokenUrl = authServerUrl + "/oauth/token",
                         });
                    }

                    options.OperationFilter<FileUploadOperationFilter>();
                });
            }

            services.AddSingleton<IDateTimeService, DateTimeService>();
        }

        public static void AddApplicationPart(this IServiceCollection services, Assembly assembly)
        {
            var managerService = services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationPartManager));
            if (managerService != null)
            {
                var applicationParts = ((ApplicationPartManager)managerService.ImplementationInstance).ApplicationParts;
                var exists = applicationParts.Any(p => (p is AssemblyPart) ? ((AssemblyPart)p).Assembly == assembly : false);
                if (!exists)
                {
                    var part = new AssemblyPart(assembly);
                    applicationParts.Add(part);
                }
            }
        }

        public static IServiceCollection ReplaceSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            var service = services.First(s => s.ServiceType == typeof(TService));
            services.Remove(service);
            return services.AddSingleton<TService, TImplementation>();
        }

        public static IServiceCollection ReplaceSingleton(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            var service = services.First(s => s.ServiceType == serviceType);
            services.Remove(service);
            return services.AddSingleton(serviceType, implementationType);
        }

        public static IServiceCollection ReplaceSingleton(this IServiceCollection services, Type serviceType, object implementationInstance)
        {
            var service = services.First(s => s.ServiceType == serviceType);
            services.Remove(service);
            return services.AddSingleton(serviceType, implementationInstance);
        }

        public static IServiceCollection ReplaceScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            var service = services.First(s => s.ServiceType == typeof(TService));
            services.Remove(service);
            return services.AddScoped(implementationFactory);
        }

        public static IServiceCollection ReplaceScoped<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            var service = services.First(s => s.ServiceType == typeof(TService));
            services.Remove(service);
            return services.AddScoped<TService, TImplementation>();
        }
    }
}
