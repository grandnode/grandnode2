using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData;

[Route("odata/Collection")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class CollectionController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CollectionController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from Collection by key", OperationId = "GetCollectionById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(key));
        if (!collection.Any()) return NotFound();

        return Ok(collection);
    }

    [SwaggerOperation("Get entities from Collection", OperationId = "GetCollections")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>()));
    }

    [SwaggerOperation("Add new entity to Collection", OperationId = "InsertCollection")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] CollectionDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        model = await _mediator.Send(new AddCollectionCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Update entity in Collection", OperationId = "UpdateCollection")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put([FromBody] CollectionDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(model.Id));
        if (!collection.Any()) return NotFound();

        model = await _mediator.Send(new UpdateCollectionCommand { Model = model });
        return Ok(model);
    }

    [SwaggerOperation("Partially update entity in Collection", OperationId = "PartiallyUpdateCollection")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
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

    [SwaggerOperation("Delete entity in Collection", OperationId = "DeleteCollection")]
    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Collections)) return Forbid();

        var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Collection>(key));
        if (!collection.Any()) return NotFound();

        await _mediator.Send(new DeleteCollectionCommand { Model = collection.FirstOrDefault() });

        return Ok();
    }
}