using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.Constants;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.DTOs.Shipping;
using Microsoft.AspNetCore.Http;
using Grand.Module.Api.DTOs.Common;

namespace Grand.Module.Api.Controllers;

public class ProductAttributeController : BaseApiController
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

    [SwaggerOperation("Get entity from ProductAttribute by key", OperationId = "GetProductAttributeById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        return Ok(productAttribute.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from ProductAttribute", OperationId = "GetProductAttributes")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductAttributeDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>()));
    }

    [SwaggerOperation("Add new entity to ProductAttribute", OperationId = "InsertProductAttribute")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] ProductAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        model = await _mediator.Send(new AddProductAttributeCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in ProductAttribute", OperationId = "UpdateProductAttribute")]
    [HttpPut("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put([FromRoute] string key, [FromBody] ProductAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        model = await _mediator.Send(new UpdateProductAttributeCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Partially update entity in ProductAttribute", OperationId = "PartiallyUpdateProductAttribute")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<ProductAttributeDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        var pa = productAttribute.FirstOrDefault();
        model.ApplyTo(pa);
        pa = await _mediator.Send(new UpdateProductAttributeCommand { Model = pa });
        return Ok(pa);
    }

    [SwaggerOperation("Delete entity from ProductAttribute", OperationId = "DeleteProductAttribute")]
    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        await _mediator.Send(new DeleteProductAttributeCommand { Model = productAttribute.FirstOrDefault() });
        return Ok();
    }
}