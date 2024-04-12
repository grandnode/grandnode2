using Grand.Api.Commands.Models.Customers;
using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData;

[Route("odata/CustomerGroup")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class CustomerGroupController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CustomerGroupController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from CustomerGroup by key", OperationId = "GetCustomerGroupById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(key));
        if (!customerGroup.Any()) return NotFound();

        return Ok(customerGroup.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from CustomerGroup", OperationId = "GetCustomerGroups")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>()));
    }

    [SwaggerOperation("Add new entity to CustomerGroup", OperationId = "InsertCustomerGroup")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] CustomerGroupDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        model = await _mediator.Send(new AddCustomerGroupCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in CustomerGroup", OperationId = "UpdateCustomerGroup")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put([FromBody] CustomerGroupDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(model.Id));
        if (!customerGroup.Any()) return NotFound();

        if (!model.IsSystem)
        {
            model = await _mediator.Send(new UpdateCustomerGroupCommand { Model = model });
            return Ok(model);
        }

        return BadRequest(ModelState);
    }

    [SwaggerOperation("Partially update entity in CustomerGroup", OperationId = "PartiallyUpdateCustomerGroup")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
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

    [SwaggerOperation("Delete entity in CustomerGroup", OperationId = "DeleteCustomerGroup")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, CustomerGroup>(key));
        if (!customerGroup.Any()) return NotFound();

        if (customerGroup.FirstOrDefault()!.IsSystem) return Forbid();

        await _mediator.Send(new DeleteCustomerGroupCommand { Model = customerGroup.FirstOrDefault() });

        return Ok();
    }
}