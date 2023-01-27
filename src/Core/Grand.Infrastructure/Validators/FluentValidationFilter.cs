using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        /// <param name="next"></param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //only in POST requests
            if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
            {
                await next();
                return;
            }
            
            foreach (var argument in context.ActionArguments.Where(x => !IsSimpleType(x.Value?.GetType())))
            {
                Type genericType = typeof(IValidator<>).MakeGenericType(argument.Value!.GetType());
                var validator = (IValidator)_serviceProvider.GetService(genericType);
                if (validator is null) continue;
                var contextValidator = new ValidationContext<object>(argument.Value);
                var result = await validator.ValidateAsync(contextValidator);
                if (result.IsValid) continue;
                
                result.AddToModelState(context.ModelState, "");
                var hasJsonData = context.HttpContext.Request.ContentType?.Contains("application/json") ?? false;
                if (!hasJsonData) continue;
                context.Result = new BadRequestObjectResult(context.ModelState);
                return;
            }

            await next();
        }
        
        private static bool IsSimpleType(Type? type)
        {
            if (type == null)
                return true;
            
            return
                type.IsPrimitive ||
                new Type[] {
                    typeof(string),
                    typeof(decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                type.IsEnum ||
                Convert.GetTypeCode(type) != TypeCode.Object ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]))
                ;
        }

        #endregion
    }
}