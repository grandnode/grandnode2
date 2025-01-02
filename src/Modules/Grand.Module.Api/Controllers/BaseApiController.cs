using Grand.Module.Api.Attributes;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Module.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[AuthorizeApiAdmin]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route($"{Configurations.RestRoutePrefix}/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    public override ForbidResult Forbid()
    {
        return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
    }
}