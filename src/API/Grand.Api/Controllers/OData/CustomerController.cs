using Grand.Api.Commands.Models.Customers;
using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Customers;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class CustomerController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        private readonly ICustomerManagerService _customerManagerService;

        private readonly CustomerSettings _customerSettings;

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

        [SwaggerOperation(summary: "Get entity from Customer by key", OperationId = "GetCustomerByEmail")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customer = await _mediator.Send(new GetCustomerQuery() { Email = key });
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        [SwaggerOperation(summary: "Add new entity to Customer", OperationId = "InsertCustomer")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] CustomerDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddCustomerCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in Customer", OperationId = "UpdateCustomer")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Put([FromBody] CustomerDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new UpdateCustomerCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Delete entity from Customer", OperationId = "DeleteCustomer")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customer = await _mediator.Send(new GetCustomerQuery() { Email = key });
            if (customer == null)
            {
                return NotFound();
            }

            await _mediator.Send(new DeleteCustomerCommand() { Email = key });

            return Ok();
        }


        //odata/Customer/(email)/AddAddress
        [SwaggerOperation(summary: "Invoke action AddAddress", OperationId = "AddAddress")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AddAddress(string key, [FromBody] AddressDto address)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _mediator.Send(new GetCustomerQuery() { Email = key });
            if (customer == null)
                return NotFound();

            address = await _mediator.Send(new AddCustomerAddressCommand() { Customer = customer, Address = address });
            return Ok(address);
        }

        //odata/Customer/(email)/UpdateAddress
        [SwaggerOperation(summary: "Invoke action UpdateAddress", OperationId = "UpdateAddress")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateAddress(string key, [FromBody] AddressDto address)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _mediator.Send(new GetCustomerQuery() { Email = key });
            if (customer == null)
                return NotFound();

            address = await _mediator.Send(new UpdateCustomerAddressCommand() { Customer = customer, Address = address });

            return Ok(address);
        }

        //odata/Customer/(email)/DeleteAddress
        //body: { "addressId": "xxx" }
        [SwaggerOperation(summary: "Invoke action DeleteAddress", OperationId = "DeleteAddress")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteAddress(string key, [FromBody] DeleteAddressDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (model == null || string.IsNullOrEmpty(model.AddressId))
                return NotFound();

            var customer = await _mediator.Send(new GetCustomerQuery() { Email = key });
            if (customer == null)
                return NotFound();

            var address = customer.Addresses.FirstOrDefault(x => x.Id == model.AddressId.ToString());
            if (address == null)
                return NotFound();

            await _mediator.Send(new DeleteCustomerAddressCommand() { Customer = customer, Address = address });

            return Ok(true);
        }


        //odata/Customer/(email)/SetPassword
        //body: { "password": "123456" }
        [SwaggerOperation(summary: "Invoke action SetPassword", OperationId = "SetPassword")]
        [Route("({key})/[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetPassword(string key, [FromBody] PasswordDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (model == null || string.IsNullOrEmpty(model.Password))
                return NotFound();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var changePassRequest = new ChangePasswordRequest(key, false, _customerSettings.DefaultPasswordFormat, model.Password);
            var changePassResult = await _customerManagerService.ChangePassword(changePassRequest);
            if (!changePassResult.Success)
            {
                return BadRequest(string.Join(',', changePassResult.Errors));
            }
            return Ok(true);
        }
    }
}
