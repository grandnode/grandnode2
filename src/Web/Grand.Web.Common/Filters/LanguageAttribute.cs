using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents filter attribute that checks SEO friendly URLs for multiple languages and properly redirect if necessary
    /// </summary>
    public class LanguageAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public LanguageAttribute() : base(typeof(LanguageSeoCodeFilter))
        {
        }

        #region Filter

        /// <summary>
        /// Represents a filter that checks SEO friendly URLs for multiple languages and properly redirect if necessary
        /// </summary>
        private class LanguageSeoCodeFilter : IAsyncActionFilter
        {
            #region Fields

            private readonly IWorkContext _workContext;
            private readonly ILanguageService _languageService;
            private readonly AppConfig _config;

            #endregion

            #region Ctor

            public LanguageSeoCodeFilter(
                IWorkContext workContext, ILanguageService languageService,
                AppConfig config)
            {
                _workContext = workContext;
                _languageService = languageService;
                _config = config;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                {
                    await next();
                    return;
                }

                if (!DataSettingsManager.DatabaseIsInstalled())
                {
                    await next();
                    return;
                }

                //only in GET requests
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                {
                    await next();
                    return;
                }

                //whether SEO friendly URLs are enabled
                if (!_config.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    await next();
                    return;
                }

                var lang = context.RouteData.Values["language"];
                if (lang == null)
                {
                    await next();
                    return;
                }

                //check whether current page URL is already localized URL
                var pageUrl = context.HttpContext?.Request?.GetEncodedPathAndQuery();
                if (await IsLocalized(pageUrl, context.HttpContext.Request.PathBase))
                {
                    await next();
                    return;
                }

                pageUrl = AddLanguageSeo(pageUrl, context.HttpContext.Request.PathBase, _workContext.WorkingLanguage);
                context.Result = new RedirectResult(pageUrl, false);
            }

            private async Task<bool> IsLocalized(string url, PathString pathBase)
            {
                var _ = new PathString(url).StartsWithSegments(pathBase, out PathString result);
                url = WebUtility.UrlDecode(result);

                var firstSegment = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                if (string.IsNullOrEmpty(firstSegment))
                    return false;

                //suppose that the first segment is the language code and try to get language
                var language = (await _languageService.GetAllLanguages())
                    .FirstOrDefault(urlLanguage => urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

                return language != null ? language.Published : false;
            }

            private string AddLanguageSeo(string url, PathString pathBase, Language language)
            {
                if (language == null)
                    throw new ArgumentNullException(nameof(language));

                //remove application path from raw URL
                if (!string.IsNullOrEmpty(url))
                {
                    var _ = new PathString(url).StartsWithSegments(pathBase, out PathString result);
                    url = WebUtility.UrlDecode(result);
                }

                //add language code
                url = $"/{language.UniqueSeoCode}{url}";
                url = pathBase + url;

                return url;
            }
            #endregion
        }



        #endregion
    }
}