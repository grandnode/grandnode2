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
using System.Net;

namespace Grand.Module.Api.Controllers;

public class DeliveryDateController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public DeliveryDateController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Delivery Date by key")]
    [EndpointName("GetDeliveryDateById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeliveryDateDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        var deliverydate = await _mediator.Send(new GetGenericQuery<DeliveryDateDto, DeliveryDate>(key));
        if (!deliverydate.Any()) return NotFound();

        return Ok(deliverydate.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Delivery Date")]
    [EndpointName("GetDeliveryDates")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DeliveryDateDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<DeliveryDateDto, DeliveryDate>()));
    }
}