using Grand.Api.Commands.Models.Customers;
using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class CustomerGroupController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public CustomerGroupController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from CustomerGroup by key", OperationId = "GetCustomerGroupById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>(key));
            if (!customerGroup.Any())
                return NotFound();

            return Ok(customerGroup.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from CustomerGroup", OperationId = "GetCustomerGroups")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>()));
        }

        [SwaggerOperation(summary: "Add new entity to CustomerGroup", OperationId = "InsertCustomerGroup")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] CustomerGroupDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddCustomerGroupCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in CustomerGroup", OperationId = "UpdateCustomerGroup")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Put([FromBody] CustomerGroupDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>(model.Id));
            if (!customerGroup.Any())
            {
                return NotFound();
            }

            if (ModelState.IsValid && !model.IsSystem)
            {
                model = await _mediator.Send(new UpdateCustomerGroupCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Partially update entity in CustomerGroup", OperationId = "PartiallyUpdateCustomerGroup")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<CustomerGroupDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>(key));
            if (!customerGroup.Any())
            {
                return NotFound();
            }
            var cr = customerGroup.FirstOrDefault();
            model.ApplyTo(cr);

            if (ModelState.IsValid && !cr.IsSystem)
            {
                await _mediator.Send(new UpdateCustomerGroupCommand() { Model = cr });
                return Ok();
            }

            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Delete entity in CustomerGroup", OperationId = "DeleteCustomerGroup")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Customers))
                return Forbid();

            var customerGroup = await _mediator.Send(new GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>(key));
            if (!customerGroup.Any())
            {
                return NotFound();
            }

            if (customerGroup.FirstOrDefault().IsSystem)
            {
                return Forbid();
            }
            await _mediator.Send(new DeleteCustomerGroupCommand() { Model = customerGroup.FirstOrDefault() });

            return Ok();
        }
    }
}
