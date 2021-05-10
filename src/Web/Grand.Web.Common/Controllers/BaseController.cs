using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Events;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Page;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Common.Controllers
{
    /// <summary>
    /// Base controller
    /// </summary>
    [PasswordExpired]
    [CustomerActivity]
    public abstract class BaseController : Controller
    {

        #region Notifications

        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void Success(string message, bool persistNextRequest = true)
        {
            Notification(NotifyType.Success, message, persistNextRequest);
        }

        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void Warning(string message, bool persistNextRequest = true)
        {
            Notification(NotifyType.Warning, message, persistNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void Error(string message, bool persistNextRequest = true)
        {
            Notification(NotifyType.Error, message, persistNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void Error(ModelStateDictionary ModelState, bool persistNextRequest = true)
        {
            var modelErrors = new List<string>();
            foreach (var modelState in ModelState.Values)
            {
                foreach (var modelError in modelState.Errors)
                {
                    modelErrors.Add(modelError.ErrorMessage);
                }
            }
            Notification(NotifyType.Error, string.Join(',', modelErrors), persistNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        protected virtual void Error(Exception exception, bool persistNextRequest = true, bool logException = true)
        {
            if (logException)
                LogException(exception);

            Notification(NotifyType.Error, exception.Message, persistNextRequest);
        }

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="exception">Exception</param>
        protected void LogException(Exception exception)
        {
            var workContext = HttpContext.RequestServices.GetRequiredService<IWorkContext>();
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger>();
            var customer = workContext.CurrentCustomer;
            logger.Error(exception.Message, exception, customer);
        }

        /// <summary>
        /// Display notification
        /// </summary>
        /// <param name="type">Notification type</param>
        /// <param name="message">Message</param>
        /// <param name="persistNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void Notification(NotifyType type, string message, bool persistNextRequest)
        {
            var dataKey = string.Format("grand.notifications.{0}", type);

            if (persistNextRequest)
            {
                if (TempData[dataKey] == null || !(TempData[dataKey] is List<string>))
                    TempData[dataKey] = new List<string>();
                ((List<string>)TempData[dataKey]).Add(message);
            }
            else
            {
                //1. Compare with null (first usage)
                //2. For some unknown reasons sometimes List<string> is converted to string[]. And it throws exceptions. That's why we reset it
                if (ViewData[dataKey] == null || !(ViewData[dataKey] is List<string>))
                    ViewData[dataKey] = new List<string>();
                ((List<string>)ViewData[dataKey]).Add(message);
            }
        }

        /// <summary>
        /// Error's json data for kendo grid
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Error's json data</returns>
        protected JsonResult ErrorForKendoGridJson(string errorMessage)
        {
            var gridModel = new DataSourceResult
            {
                Errors = errorMessage
            };

            return Json(gridModel);
        }
        /// <summary>
        /// Error's json data for kendo grid
        /// </summary>
        /// <param name="modelState">Model state</param>
        /// <returns>Error's json data</returns>
        protected JsonResult ErrorForKendoGridJson(ModelStateDictionary modelState)
        {
            var gridModel = new DataSourceResult
            {
                Errors = modelState.SerializeErrors()
            };
            return Json(gridModel);
        }
        /// <summary>
        /// Display "Edit" (manage) link (in public store)
        /// </summary>
        /// <param name="editPageUrl">Edit page URL</param>
        protected virtual void DisplayEditLink(string editPageUrl)
        {
            HttpContext.RequestServices.GetRequiredService<IPageHeadBuilder>().EditPageUrl = editPageUrl;
        }


        #endregion

        #region Localization

        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        protected virtual async Task AddLocales<TLocalizedModelLocal>(ILanguageService languageService,
            IList<TLocalizedModelLocal> locales) where TLocalizedModelLocal : ILocalizedModelLocal
        {
            await AddLocales(languageService, locales, null);
        }

        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        /// <param name="configure">Configure action</param>
        /// https://github.com/smartstore/Smartstore/blob/98494cd31f5a376bf67010264e10c37a657f6ee9/src/Smartstore.Web.Common/Controllers/ManageController.cs
        protected virtual async Task AddLocales<TLocalizedModelLocal>(ILanguageService languageService,
            IList<TLocalizedModelLocal> locales, Action<TLocalizedModelLocal, string> configure) where TLocalizedModelLocal : ILocalizedModelLocal
        {
            foreach (var language in await languageService.GetAllLanguages(true))
            {
                var locale = Activator.CreateInstance<TLocalizedModelLocal>();
                locale.LanguageId = language.Id;

                if (configure != null)
                    configure.Invoke(locale, locale.LanguageId);

                locales.Add(locale);
            }
        }


        #endregion

        #region Security

        /// <summary>
        /// Access denied view
        /// </summary>
        /// <returns>Access denied view</returns>
        protected virtual IActionResult AccessDeniedView()
        {
            return RedirectToAction("AccessDenied", "Home", new { pageUrl = HttpContext?.Request?.GetEncodedPathAndQuery() });
        }

        /// <summary>
        /// Access denied json data for kendo grid
        /// </summary>
        /// <returns>Access denied json data</returns>
        protected JsonResult AccessDeniedKendoGridJson()
        {
            var translationService = HttpContext.RequestServices.GetRequiredService<ITranslationService>();
            return ErrorForKendoGridJson(translationService.GetResource("Admin.AccessDenied.Description"));
        }

        #endregion

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // event notification before execute
            var mediator = context.HttpContext.RequestServices.GetService<IMediator>();
            await mediator.Publish(new ActionExecutingContextNotification(context, true));

            await next();

            //event notification after execute
            await mediator.Publish(new ActionExecutingContextNotification(context, false));
        }
    }
}