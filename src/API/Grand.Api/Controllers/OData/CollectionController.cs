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
    public partial class CollectionController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        public CollectionController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Collection by key", OperationId = "GetCollectionById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Domain.Catalog.Collection>(key));
            if (!collection.Any())
                return NotFound();

            return Ok(collection);
        }

        [SwaggerOperation(summary: "Get entities from Collection", OperationId = "GetCollections")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<CollectionDto, Domain.Catalog.Collection>()));
        }

        [SwaggerOperation(summary: "Add new entity to Collection", OperationId = "InsertCollection")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] CollectionDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new AddCollectionCommand() { Model = model });
                return Ok(model);
            }
            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Update entity in Collection", OperationId = "UpdateCollection")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Put([FromBody] CollectionDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Domain.Catalog.Collection>(model.Id));
            if (!collection.Any())
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                model = await _mediator.Send(new UpdateCollectionCommand() { Model = model });
                return Ok(model);
            }

            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Partially update entity in Collection", OperationId = "PartiallyUpdateCollection")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<CollectionDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Domain.Catalog.Collection>(key));
            if (!collection.Any())
            {
                return NotFound();
            }
            var man = collection.FirstOrDefault();
            model.ApplyTo(man);

            if (ModelState.IsValid)
            {
                await _mediator.Send(new UpdateCollectionCommand() { Model = man });
                return Ok();
            }

            return BadRequest(ModelState);
        }

        [SwaggerOperation(summary: "Delete entity in Collection", OperationId = "DeleteCollection")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Collections))
                return Forbid();

            var collection = await _mediator.Send(new GetGenericQuery<CollectionDto, Domain.Catalog.Collection>(key));
            if (!collection.Any())
            {
                return NotFound();
            }

            await _mediator.Send(new DeleteCollectionCommand() { Model = collection.FirstOrDefault() });

            return Ok();
        }
    }
}
