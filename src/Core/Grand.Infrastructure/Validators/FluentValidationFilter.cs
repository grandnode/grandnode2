using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Infrastructure.Validators
{
    public class FluentValidationFilter : IAsyncActionFilter
    {
        #region Fields

        private readonly IServiceProvider _serviceProvider;
        
        #endregion

        #region Ctor

        public FluentValidationFilter(IServiceProvider serviceProvider)
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
                        if (!result.IsValid) result.AddToModelState(context.ModelState, "");
                    }
                }
            }
            await next();
        }
        #endregion
    }
}
