using Grand.Module.Api.DTOs.Shipping;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Attributes;

namespace Grand.Module.Api.Controllers;

[Route($"{Configurations.RestRoutePrefix}/Warehouse")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class WarehouseController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public WarehouseController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from Warehouse by key", OperationId = "GetWarehouseById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        var warehouse = await _mediator.Send(new GetGenericQuery<WarehouseDto, Warehouse>(key));
        if (!warehouse.Any()) return NotFound();

        return Ok(warehouse.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from Warehouse", OperationId = "GetWarehouses")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<WarehouseDto, Warehouse>()));
    }
}