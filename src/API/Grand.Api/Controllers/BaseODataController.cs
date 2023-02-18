﻿using Grand.Api.Filters;
using Grand.Api.Infrastructure;
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
    [ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
    [AuthorizeApiAdmin]
    [ServiceFilter(typeof(ModelValidationAttribute))]
    public abstract partial class BaseODataController : ODataController
    {
        public override ForbidResult Forbid()
        {
            return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
        }
    }
}
