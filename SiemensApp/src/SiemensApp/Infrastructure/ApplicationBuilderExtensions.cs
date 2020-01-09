using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseApiServices(this IApplicationBuilder app, IConfiguration configuration, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            if (configuration.GetValue<bool>("EnableSwagger"))
            {
                var routePrefix = string.Empty;
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Host = $"{httpReq.Host.Value}{routePrefix}");
                });
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint($"{routePrefix}/swagger/v1/swagger.json", Assembly.GetEntryAssembly().GetName().Name + " API V1");

                    if (configuration.GetValue<string>("Auth0:ClientId") != null)
                    {
                        var clientId = configuration.GetValue<string>("Auth0:ClientId");
                        options.OAuthClientId(clientId);
                        var audience = configuration.GetValue<string>("Auth0:Audience");
                        options.OAuthAdditionalQueryStringParams(new Dictionary<string, string> { { "audience", audience } });
                    }
                });
            }

            app.UseCors(builder => {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
            app.UseMvc();
        }
    }
}
