using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class SpecificationAttributeController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public SpecificationAttributeController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from SpecificationAttribute by key")]
    [EndpointName("GetSpecificationAttributeById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes, CurrentCustomer)) return Forbid();

        var specificationAttribute = (await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key))).FirstOrDefault();
        if (specificationAttribute == null) return NotFound();

        return Ok(specificationAttribute);
    }

    [EndpointDescription("Get entities from SpecificationAttribute")]
    [EndpointName("GetSpecificationAttributes")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SpecificationAttributeDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes, CurrentCustomer)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>()));
    }

    [EndpointDescription("Add new entity to SpecificationAttribute")]
    [EndpointName("InsertSpecificationAttribute")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] SpecificationAttributeDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.SpecificationAttributes, PermissionActionName.Create, CurrentCustomer)) return Forbid();

        model = await _mediator.Send(new AddSpecificationAttributeCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in SpecificationAttribute")]
    [EndpointName("UpdateSpecificationAttribute")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put([FromBody] SpecificationAttributeDto model)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.SpecificationAttributes, PermissionActionName.Edit, CurrentCustomer)) return Forbid();

        if (string.IsNullOrEmpty(model.Id))
            return BadRequest("Key is null or empty");

        var updated = await _mediator.Send(new UpdateSpecificationAttributeCommand { Model = model });
        return updated == null ? NotFound() : Ok(updated);
    }

    [EndpointDescription("Partially update entity in SpecificationAttribute")]
    [EndpointName("PartiallyUpdateSpecificationAttribute")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<SpecificationAttributeDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.AuthorizeAction(PermissionSystemName.SpecificationAttributes, PermissionActionName.Edit, CurrentCustomer)) return Forbid();

        var specification = (await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key))).FirstOrDefault();
        if (specification == null) return NotFound();

        var spec = specification;
        if (!TryApplyPatch(model, spec)) return BadRequest(ModelState);
        await _mediator.Send(new UpdateSpecificationAttributeCommand { Model = spec });
        return Ok();
    }

    [EndpointDescription("Delete entity in SpecificationAttribute")]
    [EndpointName("DeleteSpecificationAttribute")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.AuthorizeAction(PermissionSystemName.SpecificationAttributes, PermissionActionName.Delete, CurrentCustomer)) return Forbid();

        var specification =
            (await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key))).FirstOrDefault();
        if (specification == null) return NotFound();

        await _mediator.Send(new DeleteSpecificationAttributeCommand { Model = specification });

        return Ok();
    }
}