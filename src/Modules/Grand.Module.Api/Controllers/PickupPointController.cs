using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.DTOs.Shipping;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class PickupPointController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public PickupPointController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from PickupPoint by key")]
    [EndpointName("GetPickupPointById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PickupPointDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        var points = await _mediator.Send(new GetGenericQuery<PickupPointDto, PickupPoint>(key));
        if (!points.Any()) return NotFound();

        return Ok(points.FirstOrDefault());
    }

    [EndpointDescription("Get entities from PickupPoint")]
    [EndpointName("GetPickupPoints")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PickupPointDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<PickupPointDto, PickupPoint>()));
    }
}