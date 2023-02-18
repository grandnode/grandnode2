﻿using Grand.Business.Core.Interfaces.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Authentication.Google.Infrastructure
{
    /// <summary>
    /// Registration of google authentication service (plugin)
    /// </summary>
    public class GoogleAuthenticationBuilder : IAuthenticationBuilder
    {
        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="builder">Authentication builder</param>
        public void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration)
        {
            builder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                var clientId = configuration["GoogleSettings:ClientId"];
                var clientSecret = configuration["GoogleSettings:ClientSecret"];

                options.ClientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : "000";
                options.ClientSecret = !string.IsNullOrWhiteSpace(clientSecret) ? clientSecret : "000";
                options.SaveTokens = true;

                //handles exception thrown by external auth provider
                options.Events = new OAuthEvents() {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.HandleResponse();
                        var errorMessage = ctx.Failure.Message;
                        var state = ctx.Request.Query["state"].FirstOrDefault();
                        errorMessage = WebUtility.UrlEncode(errorMessage);
                        ctx.Response.Redirect($"/google-signin-failed?error_message={errorMessage}");

                        return Task.FromResult(0);
                    }
                };
            });

        }
        public int Priority => 502;

    }
}
