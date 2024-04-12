using Grand.Api.Extensions;
using Grand.Api.Infrastructure.Filters;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Grand.Api.Infrastructure;

public class SwaggerStartup : IStartupApplication
{
    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
        var backendApiConfig = application.ApplicationServices.GetService<BackendAPIConfig>();
        var frontApiConfig = application.ApplicationServices.GetService<FrontendAPIConfig>();
        var swagger = application.ApplicationServices.GetService<IConfiguration>().GetValue<bool>("UseSwagger");
        if (backendApiConfig.Enabled)
            application.UseODataQueryRequest();

        if (swagger && (backendApiConfig.Enabled || frontApiConfig.Enabled))
        {
            application.UseSwagger();
            application.UseSwaggerUI(c =>
            {
                if (backendApiConfig.Enabled)
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grandnode Backend API");

                if (frontApiConfig.Enabled)
                    c.SwaggerEndpoint("/swagger/v2/swagger.json", "Grandnode Frontend API");
            });
        }
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var backendApiConfig = services.BuildServiceProvider().GetService<BackendAPIConfig>();
        var frontApiConfig = services.BuildServiceProvider().GetService<FrontendAPIConfig>();
        var swagger = configuration.GetValue<bool>("UseSwagger");
        if (swagger && (backendApiConfig.Enabled || frontApiConfig.Enabled))
            services.AddSwaggerGen(c =>
            {
                if (backendApiConfig.Enabled)
                    c.SwaggerDoc("v1", new OpenApiInfo {
                        Title = "Grandnode Backend API",
                        Version = "v1",
                        Contact = GetOpenApiContact(),
                        License = GetOpenApiLicense()
                    });

                if (frontApiConfig.Enabled)
                    c.SwaggerDoc("v2", new OpenApiInfo {
                        Title = "Grandnode Frontend API",
                        Version = "v2",
                        Contact = GetOpenApiContact(),
                        License = GetOpenApiLicense()
                    });

                c.CustomSchemaIds(s => s.FullName?.Replace("+", "."));
                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, //Name the security scheme
                    new OpenApiSecurityScheme {
                        Description = "JWT Authorization header using the Bearer scheme.",
                        Type = SecuritySchemeType
                            .Http, //We set the scheme type to http since we're using bearer authentication
                        Scheme =
                            "bearer" //The name of the HTTP Authorization scheme to be used in the Authorization header. In this case "bearer".
                    });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Id = JwtBearerDefaults.AuthenticationScheme,
                                Type //The name of the previously defined security scheme.
                                    = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });
                c.OperationFilter<AddParamOperationFilter>();
                c.DocumentFilter<SwaggerODataControllerDocumentFilter>();
                c.EnableAnnotations();
                c.SchemaFilter<EnumSchemaFilter>();
                c.SchemaFilter<IgnoreFieldFilter>();
            });
    }

    public int Priority => 90;
    public bool BeforeConfigure => true;

    private OpenApiContact GetOpenApiContact()
    {
        return new OpenApiContact {
            Name = "Grandnode",
            Email = "support@grandnode.com",
            Url = new Uri("https://grandnode.com")
        };
    }

    private OpenApiLicense GetOpenApiLicense()
    {
        return new OpenApiLicense {
            Name = "GNU General Public License v3.0",
            Url = new Uri("https://github.com/grandnode/grandnode2/blob/main/LICENSE")
        };
    }
}