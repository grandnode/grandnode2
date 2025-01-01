using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Attributes;

namespace Grand.Module.Api.Controllers;

[Route($"{Configurations.RestRoutePrefix}/BrandLayout")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class BrandLayoutController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public BrandLayoutController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from BrandLayout by key", OperationId = "GetBrandLayoutById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get(string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        var layout = await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>(key));
        if (!layout.Any()) return NotFound();

        return Ok(layout.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from BrandLayout", OperationId = "GetBrandLayouts")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>()));
    }
}