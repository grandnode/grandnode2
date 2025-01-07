using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Queries.Models.Customers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Net;

namespace Grand.Module.Api.Controllers;

public class CustomerController : BaseApiController
{
    private readonly ICustomerManagerService _customerManagerService;

    private readonly CustomerSettings _customerSettings;
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CustomerController(
        IMediator mediator,
        IPermissionService permissionService,
        ICustomerManagerService customerManagerService,
        CustomerSettings customerSettings)
    {
        _mediator = mediator;
        _customerManagerService = customerManagerService;
        _customerSettings = customerSettings;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Customer by email")]
    [EndpointName("GetCustomerByEmail")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string email)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customer = await _mediator.Send(new GetCustomerQuery { Email = email });
        if (customer == null) return NotFound();

        return Ok(customer);
    }

    [EndpointDescription("Add new entity to Customer")]
    [EndpointName("InsertCustomer")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] CustomerDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        model = await _mediator.Send(new AddCustomerCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Customer")]
    [EndpointName("UpdateCustomer")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Put([FromBody] CustomerDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        model = await _mediator.Send(new UpdateCustomerCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Delete entity from Customer")]
    [EndpointName("DeleteCustomer")]
    [HttpDelete("{email}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string email)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customer = await _mediator.Send(new GetCustomerQuery { Email = email });
        if (customer == null) return NotFound();

        await _mediator.Send(new DeleteCustomerCommand { Email = email });

        return Ok();
    }

    //api/Customer/email/AddAddress
    [EndpointDescription("Invoke action AddAddress")]
    [EndpointName("AddAddress")]
    [HttpPost("{email}/AddAddress")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AddressDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> AddAddress([FromRoute] string email, [FromBody] AddressDto address)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customer = await _mediator.Send(new GetCustomerQuery { Email = email });
        if (customer == null) return NotFound();

        address = await _mediator.Send(new AddCustomerAddressCommand { Customer = customer, Address = address });
        return Ok(address);
    }

    //api/Customer/email/UpdateAddress
    [EndpointDescription("Invoke action UpdateAddress")]
    [EndpointName("UpdateAddress")]
    [HttpPost("{email}/UpdateAddress")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AddressDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateAddress([FromRoute] string email, [FromBody] AddressDto address)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        var customer = await _mediator.Send(new GetCustomerQuery { Email = email });
        if (customer == null) return NotFound();

        address = await _mediator.Send(new UpdateCustomerAddressCommand { Customer = customer, Address = address });

        return Ok(address);
    }

    //api/Customer/email/DeleteAddress
    //body: { "addressId": "xxx" }
    [EndpointDescription("Invoke action DeleteAddress")]
    [EndpointName("DeleteAddress")]
    [HttpPost("{email}/DeleteAddress")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteAddress([FromRoute] string email, [FromBody] DeleteAddressDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        if (model == null || string.IsNullOrEmpty(model.AddressId)) return NotFound();

        var customer = await _mediator.Send(new GetCustomerQuery { Email = email });
        if (customer == null) return NotFound();

        var address = customer.Addresses.FirstOrDefault(x => x.Id == model.AddressId);
        if (address == null) return NotFound();

        await _mediator.Send(new DeleteCustomerAddressCommand { Customer = customer, Address = address });

        return Ok(true);
    }

    //api/Customer/email/SetPassword
    //body: { "password": "123456" }
    [EndpointDescription("Invoke action SetPassword")]
    [EndpointName("SetPassword")]
    [HttpPost("{email}/SetPassword")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> SetPassword([FromRoute] string email, [FromBody] PasswordDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Customers)) return Forbid();

        if (model == null || string.IsNullOrEmpty(model.Password)) return NotFound();

        var changePassRequest = new ChangePasswordRequest(email, _customerSettings.DefaultPasswordFormat, model.Password);
        await _customerManagerService.ChangePassword(changePassRequest);

        return Ok(true);
    }
}