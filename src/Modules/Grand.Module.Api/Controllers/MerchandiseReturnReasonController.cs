using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Mediator;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Queries.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class MerchandiseReturnReasonController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public MerchandiseReturnReasonController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entities from MerchandiseReturnReason")]
    [EndpointName("GetMerchandiseReturnReasons")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MerchandiseReturnReasonDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Settings, CurrentCustomer)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<MerchandiseReturnReasonDto, MerchandiseReturnReason>()));
    }

    [EndpointDescription("Get entity from MerchandiseReturnReason by key")]
    [EndpointName("GetMerchandiseReturnReasonById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnReasonDto))]
    public async Task<IActionResult> GetById([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Settings, CurrentCustomer)) return Forbid();

        var reason = await _mediator.Send(new GetGenericQuery<MerchandiseReturnReasonDto, MerchandiseReturnReason>(key));
        if (!reason.Any()) return NotFound();

        return Ok(reason.FirstOrDefault());
    }

    [EndpointDescription("Add new entity to MerchandiseReturnReason")]
    [EndpointName("InsertMerchandiseReturnReason")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnReasonDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] MerchandiseReturnReasonDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Create, CurrentCustomer)) return Forbid();

        model = await _mediator.Send(new AddMerchandiseReturnReasonCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in MerchandiseReturnReason")]
    [EndpointName("UpdateMerchandiseReturnReason")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnReasonDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] MerchandiseReturnReasonDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Edit, CurrentCustomer)) return Forbid();

        model = await _mediator.Send(new UpdateMerchandiseReturnReasonCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Delete entity in MerchandiseReturnReason")]
    [EndpointName("DeleteMerchandiseReturnReason")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Delete, CurrentCustomer)) return Forbid();

        var reason = await _mediator.Send(new GetGenericQuery<MerchandiseReturnReasonDto, MerchandiseReturnReason>(key));
        if (!reason.Any()) return NotFound();

        await _mediator.Send(new DeleteMerchandiseReturnReasonCommand { Model = reason.FirstOrDefault() });
        return Ok();
    }
}
