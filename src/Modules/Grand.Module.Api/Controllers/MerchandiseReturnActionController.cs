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

public class MerchandiseReturnActionController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public MerchandiseReturnActionController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entities from MerchandiseReturnAction")]
    [EndpointName("GetMerchandiseReturnActions")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<MerchandiseReturnActionDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Settings, CurrentCustomer)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<MerchandiseReturnActionDto, MerchandiseReturnAction>()));
    }

    [EndpointDescription("Get entity from MerchandiseReturnAction by key")]
    [EndpointName("GetMerchandiseReturnActionById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnActionDto))]
    public async Task<IActionResult> GetById([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Settings, CurrentCustomer)) return Forbid();

        var action = (await _mediator.Send(new GetGenericQuery<MerchandiseReturnActionDto, MerchandiseReturnAction>(key))).FirstOrDefault();
        if (action == null) return NotFound();

        return Ok(action);
    }

    [EndpointDescription("Add new entity to MerchandiseReturnAction")]
    [EndpointName("InsertMerchandiseReturnAction")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnActionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] MerchandiseReturnActionDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Create, CurrentCustomer)) return Forbid();

        model = await _mediator.Send(new AddMerchandiseReturnActionCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in MerchandiseReturnAction")]
    [EndpointName("UpdateMerchandiseReturnAction")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnActionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put([FromBody] MerchandiseReturnActionDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Edit, CurrentCustomer)) return Forbid();

        if (string.IsNullOrEmpty(model.Id))
            return BadRequest("Key is null or empty");

        var updated = await _mediator.Send(new UpdateMerchandiseReturnActionCommand { Model = model });
        return updated == null ? NotFound() : Ok(updated);
    }

    [EndpointDescription("Delete entity in MerchandiseReturnAction")]
    [EndpointName("DeleteMerchandiseReturnAction")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.Settings, PermissionActionName.Delete, CurrentCustomer)) return Forbid();

        var action = (await _mediator.Send(new GetGenericQuery<MerchandiseReturnActionDto, MerchandiseReturnAction>(key))).FirstOrDefault();
        if (action == null) return NotFound();

        await _mediator.Send(new DeleteMerchandiseReturnActionCommand { Model = action });
        return Ok();
    }
}
