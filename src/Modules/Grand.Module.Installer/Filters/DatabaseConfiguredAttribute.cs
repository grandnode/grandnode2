using Grand.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Module.Installer.Filters;

public class DatabaseConfiguredAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
        {
            await next();
        }
        else
        {
            context.Result = new NotFoundResult();
        }
    }
}
