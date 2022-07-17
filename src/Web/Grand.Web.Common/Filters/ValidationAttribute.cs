using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents filter attribute that valid data
    /// </summary>
    public class ValidationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public ValidationAttribute() : base(typeof(ValidationFilter))
        {
        }

        #region Filter

        /// <summary>
        /// Represents a filter that validate data by fluenvalidators
        /// </summary>
        private class ValidationFilter : IAsyncActionFilter
        {
            private readonly IServiceProvider _serviceProvider;

            #region Ctor

            public ValidationFilter(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {

                //only in POST requests
                if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
                {
                    await next();
                    return;
                }

                if (context.ActionArguments.TryGetValue("model", out var model))
                {

                    Type genericType = typeof(IValidator<>).MakeGenericType(model.GetType());
                    if (genericType is not null)
                    {
                        var validator = (IValidator)_serviceProvider.GetService(genericType);
                        if (validator is not null)
                        {
                            var contextvalidator = new ValidationContext<object>(model);
                            var result = await validator.ValidateAsync(contextvalidator);
                            if (!result.IsValid)
                            {
                                result.AddToModelState(context.ModelState, "");
                            }
                        }
                    }
                }


                await next();

            }
            #endregion
        }

        #endregion
    }
}