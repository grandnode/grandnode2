using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Microsoft.AspNetCore.Http;

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

    [SwaggerOperation("Get entity from SpecificationAttribute by key", OperationId = "GetSpecificationAttributeById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

        var specificationAttribute = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key));
        if (!specificationAttribute.Any()) return NotFound();

        return Ok(specificationAttribute.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from SpecificationAttribute", OperationId = "GetSpecificationAttributes")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SpecificationAttributeDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>()));
    }

    [SwaggerOperation("Add new entity to SpecificationAttribute", OperationId = "InsertSpecificationAttribute")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] SpecificationAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

        model = await _mediator.Send(new AddSpecificationAttributeCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in SpecificationAttribute", OperationId = "UpdateSpecificationAttribute")]
    [HttpPut("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SpecificationAttributeDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Put([FromRoute] string key, [FromBody] SpecificationAttributeDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();
        var specification = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key));
        if (!specification.Any()) return NotFound();

        model = await _mediator.Send(new UpdateSpecificationAttributeCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Partially update entity in SpecificationAttribute", OperationId = "PartiallyUpdateSpecificationAttribute")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<SpecificationAttributeDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

        var specification = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key));
        if (!specification.Any()) return NotFound();

        var spec = specification.FirstOrDefault();
        model.ApplyTo(spec);
        await _mediator.Send(new UpdateSpecificationAttributeCommand { Model = spec });
        return Ok();
    }

    [SwaggerOperation("Delete entity in SpecificationAttribute", OperationId = "DeleteSpecificationAttribute")]
    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

        var specification =
            await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, SpecificationAttribute>(key));
        if (!specification.Any()) return NotFound();

        await _mediator.Send(new DeleteSpecificationAttributeCommand { Model = specification.FirstOrDefault() });

        return Ok();
    }
}