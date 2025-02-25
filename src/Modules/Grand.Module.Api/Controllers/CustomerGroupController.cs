using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class CustomerGroupController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CustomerGroupController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from CustomerGroup by key")]
    [EndpointName("GetCustomerGroupById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerGroupDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(key));
        if (!customerGroup.Any()) return NotFound();

        return Ok(customerGroup.FirstOrDefault());
    }

    [EndpointDescription("Get entities from CustomerGroup")]
    [EndpointName("GetCustomerGroups")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CustomerGroupDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>()));
    }

    [EndpointDescription("Add new entity to CustomerGroup")]
    [EndpointName("InsertCustomerGroup")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerGroupDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CustomerGroupDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        model = await _mediator.Send(new AddCustomerGroupCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in CustomerGroup")]
    [EndpointName("UpdateCustomerGroup")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerGroupDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] CustomerGroupDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        if (!model.IsSystem)
        {
            model = await _mediator.Send(new UpdateCustomerGroupCommand { Model = model });
            return Ok(model);
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Partially update entity in CustomerGroup")]
    [EndpointName("PartiallyUpdateCustomerGroup")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<CustomerGroupDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(key));
        if (!customerGroup.Any()) return NotFound();

        var cr = customerGroup.FirstOrDefault();
        model.ApplyTo(cr);
        if (cr is { IsSystem: false })
        {
            await _mediator.Send(new UpdateCustomerGroupCommand { Model = cr });
            return Ok();
        }

        return BadRequest(ModelState);
    }

    [EndpointDescription("Delete entity in CustomerGroup")]
    [EndpointName("DeleteCustomerGroup")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(key));
        if (!customerGroup.Any()) return NotFound();

        if (customerGroup.FirstOrDefault()!.IsSystem) return Forbid();

        await _mediator.Send(new DeleteCustomerGroupCommand { Model = customerGroup.FirstOrDefault() });

        return Ok();
    }
}