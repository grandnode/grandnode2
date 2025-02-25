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

public class CollectionController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CollectionController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Collection by key")]
    [EndpointName("GetCollectionById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CollectionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(key));
        if (!collection.Any()) return NotFound();

        return Ok(collection);
    }

    [EndpointDescription("Get entities from Collection")]
    [EndpointName("GetCollections")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CollectionDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>()));
    }

    [EndpointDescription("Add new entity to Collection")]
    [EndpointName("InsertCollection")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CollectionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CollectionDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        model = await _mediator.Send(new AddCollectionCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Collection")]
    [EndpointName("UpdateCollection")]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CollectionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] CollectionDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        model = await _mediator.Send(new UpdateCollectionCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Partially update entity in Collection")]
    [EndpointName("PartiallyUpdateCollection")]
    [HttpPatch("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<CollectionDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(key));
        if (!collection.Any()) return NotFound();

        var man = collection.FirstOrDefault();
        model.ApplyTo(man);
        await _mediator.Send(new UpdateCollectionCommand { Model = man });
        return Ok();
    }

    [EndpointDescription("Delete entity in Collection")]
    [EndpointName("DeleteCollection")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(key));
        if (!collection.Any()) return NotFound();

        await _mediator.Send(new DeleteCollectionCommand { Model = collection.FirstOrDefault() });

        return Ok();
    }
}