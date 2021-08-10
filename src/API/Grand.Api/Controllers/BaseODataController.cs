using Grand.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Grand.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ODataRouteComponent]
    [Route("odata/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [AuthorizeApiAdmin]
    public abstract partial class BaseODataController : ODataController
    {
        public override ForbidResult Forbid()
        {
            return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
        }
    }
}
