using Grand.Api.DTOs.Shipping;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData;

[Route("odata/ShippingMethod")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class ShippingMethodController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public ShippingMethodController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from ShippingMethod by key", OperationId = "GetShippingMethodById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        var shipping = await _mediator.Send(new GetGenericQuery<ShippingMethodDto, ShippingMethod>(key));
        if (!shipping.Any()) return NotFound();

        return Ok(shipping.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from ShippingMethod", OperationId = "GetShippingMethods")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<ShippingMethodDto, ShippingMethod>()));
    }
}