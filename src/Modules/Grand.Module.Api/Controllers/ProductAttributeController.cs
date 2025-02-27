using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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

    [EndpointDescription("Get entity from ProductAttribute by key")]
    [EndpointName("GetProductAttributeById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        return Ok(productAttribute.FirstOrDefault());
    }

    [EndpointDescription("Get entities from ProductAttribute")]
    [EndpointName("GetProductAttributes")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductAttributeDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>()));
    }

    [EndpointDescription("Add new entity to ProductAttribute")]
    [EndpointName("InsertProductAttribute")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] ProductAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        model = await _mediator.Send(new AddProductAttributeCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in ProductAttribute")]
    [EndpointName("UpdateProductAttribute")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] ProductAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        model = await _mediator.Send(new UpdateProductAttributeCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Partially update entity in ProductAttribute")]
    [EndpointName("PartiallyUpdateProductAttribute")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductAttributeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [EndpointDescription("Delete entity from ProductAttribute")]
    [EndpointName("DeleteProductAttribute")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.ProductAttributes)) return Forbid();

        var productAttribute = await _mediator.Send(new GetGenericQuery<ProductAttributeDto, ProductAttribute>(key));
        if (!productAttribute.Any()) return NotFound();

        await _mediator.Send(new DeleteProductAttributeCommand { Model = productAttribute.FirstOrDefault() });
        return Ok();
    }
}