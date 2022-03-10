using Grand.Business.Authentication.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Authentication.Facebook.Infrastructure
{
    /// <summary>
    /// Registration of Facebook authentication service (plugin)
    /// </summary>
    public class FacebookAuthenticationBuilder : IAuthenticationBuilder
    {
        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="builder">Authentication builder</param>
        public void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration)
        {
            builder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
            {
                var appId = configuration["FacebookSettings:AppId"];
                var appSecret = configuration["FacebookSettings:AppSecret"];

                //no empty values allowed. otherwise, an exception could be thrown on application startup
                options.AppId = !string.IsNullOrWhiteSpace(appId) ? appId : "000";
                options.AppSecret = !string.IsNullOrWhiteSpace(appSecret) ? appSecret : "000";
                options.SaveTokens = true;
                //handles exception thrown by external auth provider
                options.Events = new OAuthEvents() {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.HandleResponse();
                        var errorCode = ctx.Request.Query["error_code"].FirstOrDefault();
                        var errorMessage = ctx.Request.Query["error_message"].FirstOrDefault();
                        var state = ctx.Request.Query["state"].FirstOrDefault();
                        errorCode = WebUtility.UrlEncode(errorCode);
                        errorMessage = WebUtility.UrlEncode(errorMessage);
                        ctx.Response.Redirect($"/fb-signin-failed?error_code={errorCode}&error_message={errorMessage}");

                        return Task.FromResult(0);
                    }
                };
            });
        }

        /// <summary>
        /// Gets order of this registrar implementation
        /// </summary>
        public int Priority => 501;
    }
}