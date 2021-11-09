﻿using Grand.Api.Extensions;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;

namespace Grand.Api.Infrastructure
{
    public class SwaggerStartup : IStartupApplication
    {
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            var apiConfig = application.ApplicationServices.GetService<ApiConfig>();

            if(apiConfig.Enabled)
                application.UseODataQueryRequest();

            if (apiConfig.Enabled && apiConfig.UseSwagger)
            {
                application.UseSwagger();
                application.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grandnode API V1");
                });
            }
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var apiConfig = services.BuildServiceProvider().GetService<ApiConfig>();
            if (apiConfig.Enabled && apiConfig.UseSwagger)
            {
                
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Grandnode API", Version = "v1" });
                    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, //Name the security scheme
                        new OpenApiSecurityScheme
                        {
                            Description = "JWT Authorization header using the Bearer scheme.",
                            Type = SecuritySchemeType.Http, //We set the scheme type to http since we're using bearer authentication
                            Scheme = "bearer"               //The name of the HTTP Authorization scheme to be used in the Authorization header. In this case "bearer".
                        });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        {
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference{
                                    Id = JwtBearerDefaults.AuthenticationScheme,      //The name of the previously defined security scheme.
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                    });
                    c.OperationFilter<AddParamOperationFilter>();
                    c.EnableAnnotations();
                    c.SchemaFilter<EnumSchemaFilter>();
                });
                
            }
        }

        public int Priority => 90;
        public bool BeforeConfigure => true;

    }
}
