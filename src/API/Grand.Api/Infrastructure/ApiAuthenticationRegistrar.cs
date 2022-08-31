using Grand.Api.Infrastructure.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Grand.Api.Infrastructure
{
    public partial class ApiAuthenticationRegistrar : IAuthenticationBuilder
    {
        public void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration)
        {
            builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var config = new BackendAPIConfig();
                configuration.GetSection("BackendAPI").Bind(config);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = config.ValidateIssuer,
                    ValidateAudience = config.ValidateAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config.ValidIssuer,
                    ValidAudience = config.ValidAudience,
                    IssuerSigningKey = JwtSecurityKey.Create(config.SecretKey)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = async context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync(context.Exception.Message);
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                 
                        try
                        {
                            if (config.Enabled)
                            {
                                var jwtAuthentication = context.HttpContext.RequestServices.GetRequiredService<IJwtBearerAuthenticationService>();
                                if (!await jwtAuthentication.Valid(context))
                                    throw new Exception(await jwtAuthentication.ErrorMessage());
                            }
                            else
                                throw new Exception("API is disable");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    },
                };
            });


            builder.AddJwtBearer(FrontendAPIConfig.Scheme, options =>
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
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync(context.Exception.Message);
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        try
                        {
                            if (config.Enabled)
                            {
                                var jwtAuthentication = context.HttpContext.RequestServices.GetRequiredService<IJwtBearerCustomerAuthenticationService>();
                                var isValid = await jwtAuthentication.Valid(context);
                                if (!isValid)
                                    throw new Exception(await jwtAuthentication.ErrorMessage());
                            }
                            else
                                throw new Exception("API is disable");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    },
                };
            });


        }
        public int Priority => 900;

    }
}
