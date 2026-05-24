using Grand.Module.Api.Infrastructure.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Grand.Module.Api.Infrastructure;

public class ApiAuthenticationRegistrar : IAuthenticationBuilder
{
    public void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration)
    {
        builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var config = new BackendAPIConfig();
            configuration.GetSection("BackendAPI").Bind(config);
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = config.ValidateIssuer,
                ValidateAudience = config.ValidateAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.ValidIssuer,
                ValidAudience = config.ValidAudience,
                IssuerSigningKey = JwtSecurityKey.Create(config.SecretKey)
            };

            options.Events = new JwtBearerEvents {
                OnAuthenticationFailed = async context =>
                {
                    context.NoResult();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    var problemDetailsService = context.HttpContext.RequestServices.GetService<IProblemDetailsService>();
                    if (problemDetailsService != null)
                    {
                        await problemDetailsService.WriteAsync(new ProblemDetailsContext {
                            HttpContext = context.HttpContext,
                            ProblemDetails = new ProblemDetails {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = "Authentication failed"
                            }
                        });
                    }
                    else
                    {
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Authentication failed"
                        });
                    }
                },
                OnTokenValidated = async context =>
                {
                    if (config.Enabled)
                    {
                        var jwtAuthentication = context.HttpContext.RequestServices
                            .GetRequiredService<IJwtBearerAuthenticationService>();
                        if (!await jwtAuthentication.Valid(context))
                            throw new Exception(await jwtAuthentication.ErrorMessage());
                    }
                    else
                    {
                        throw new Exception("API is disabled");
                    }
                }
            };
        });


        builder.AddJwtBearer(FrontendAPIConfig.AuthenticationScheme, options =>
        {
            var config = new FrontendAPIConfig();
            configuration.GetSection("FrontendAPI").Bind(config);
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = config.ValidateIssuer,
                ValidateAudience = config.ValidateAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config.ValidIssuer,
                ValidAudience = config.ValidAudience,
                IssuerSigningKey = JwtSecurityKey.Create(config.SecretKey),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents {
                OnAuthenticationFailed = async context =>
                {
                    context.NoResult();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    var problemDetailsService = context.HttpContext.RequestServices.GetService<IProblemDetailsService>();
                    if (problemDetailsService != null)
                    {
                        await problemDetailsService.WriteAsync(new ProblemDetailsContext {
                            HttpContext = context.HttpContext,
                            ProblemDetails = new ProblemDetails {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = "Authentication failed"
                            }
                        });
                    }
                    else
                    {
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new ProblemDetails {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Authentication failed"
                        });
                    }
                },
                OnTokenValidated = async context =>
                {
                    if (config.Enabled)
                    {
                        var jwtAuthentication = context.HttpContext.RequestServices
                            .GetRequiredService<IJwtBearerCustomerAuthenticationService>();
                        var isValid = await jwtAuthentication.Valid(context);
                        if (!isValid)
                            throw new Exception(await jwtAuthentication.ErrorMessage());
                    }
                    else
                    {
                        throw new Exception("API is disabled");
                    }
                }
            };
        });
    }

    public int Priority => 900;
}
