using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class ProductAttributeController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public ProductAttributeController(
            IMediator mediator,
            IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from ProductAttribute by key", OperationId = "GetProductAttributeById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, Domain.Catalog.ProductAttribute>(key));
            if (!productAttribute.Any())
                return NotFound();

            return Ok(productAttribute.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from ProductAttribute", OperationId = "GetProductAttributes")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<ProductAttributeDto, Domain.Catalog.ProductAttribute>()));
        }

        [SwaggerOperation(summary: "Add new entity to ProductAttribute", OperationId = "InsertProductAttribute")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] ProductAttributeDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddProductAttributeCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in ProductAttribute", OperationId = "UpdateProductAttribute")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Put([FromBody] ProductAttributeDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new UpdateProductAttributeCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Partially update entity in ProductAttribute", OperationId = "PartiallyUpdateProductAttribute")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<ProductAttributeDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, Domain.Catalog.ProductAttribute>(key));
            if (!productAttribute.Any())
                return NotFound();

            var pa = productAttribute.FirstOrDefault();
            model.ApplyTo(pa);

            if (ModelState.IsValid)
            {
                pa = await _mediator.Send(new UpdateProductAttributeCommand() { Model = pa });
                return Ok(pa);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Delete entity from ProductAttribute", OperationId = "DeleteProductAttribute")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes))
                return Forbid();

            var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, Domain.Catalog.ProductAttribute>(key));
            if (!productAttribute.Any())
                return NotFound();

            await _mediator.Send(new DeleteProductAttributeCommand() { Model = productAttribute.FirstOrDefault() });
            return Ok();
        }
    }
}
