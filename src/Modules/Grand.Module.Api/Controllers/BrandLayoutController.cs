using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class BrandLayoutController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public BrandLayoutController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from BrandLayout by key")]
    [EndpointName("GetBrandLayoutById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LayoutDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        var layout = await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>(key));
        if (!layout.Any()) return NotFound();

        return Ok(layout.FirstOrDefault());
    }

    [EndpointDescription("Get entities from BrandLayout")]
    [EndpointName("GetBrandLayouts")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LayoutDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>()));
    }
}