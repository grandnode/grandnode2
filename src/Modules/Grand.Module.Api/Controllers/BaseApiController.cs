using Grand.Domain.Customers;
using Grand.Domain.Audit;
using Grand.Infrastructure;
using Grand.Infrastructure.Audit;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
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
        HttpContext.RequestServices.GetRequiredService<IContextAccessor>().WorkContext.CurrentCustomer;

    /// <summary>
    ///     Applies the patch to the record; an operation it cannot apply, such as an unknown path or a value of the
    ///     wrong type, becomes a model error and the record must not be saved
    /// </summary>
    protected bool TryApplyPatch<T>(JsonPatchDocument<T> patch, T record) where T : class
    {
        patch.ApplyTo(record, error => ModelState.AddModelError(error.Operation?.path ?? "", error.ErrorMessage));
        return ModelState.IsValid;
    }

    public override ForbidResult Forbid()
    {
        return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
    }
}