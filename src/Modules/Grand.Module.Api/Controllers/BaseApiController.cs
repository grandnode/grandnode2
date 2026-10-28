using Grand.Domain.Customers;
using Grand.Domain.Audit;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[AuthorizeApiAdmin]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route($"{Configurations.RestRoutePrefix}/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
[Produces("application/json")]
[AuditSource(AuditSource.Api)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    ///     Customer of the current request
    /// </summary>
    protected Customer CurrentCustomer =>
        HttpContext.RequestServices.GetRequiredService<IContextAccessor>().WorkContext?.CurrentCustomer;

    public override ForbidResult Forbid()
    {
        return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
    }
}