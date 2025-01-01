using Grand.Module.Api.Attributes;
using Grand.Module.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Module.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[AuthorizeApiAdmin]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    public override ForbidResult Forbid()
    {
        return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
    }
}