﻿using Grand.Api.Commands.Models.Catalog;
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
    public partial class SpecificationAttributeController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        public SpecificationAttributeController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from SpecificationAttribute by key", OperationId = "GetSpecificationAttributeById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            var specificationAttribute = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>(key));
            if (!specificationAttribute.Any()) return NotFound();

            return Ok(specificationAttribute.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from SpecificationAttribute", OperationId = "GetSpecificationAttributes")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>()));
        }

        [SwaggerOperation(summary: "Add new entity to SpecificationAttribute", OperationId = "InsertSpecificationAttribute")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Post([FromBody] SpecificationAttributeDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            model = await _mediator.Send(new AddSpecificationAttributeCommand() { Model = model });
            return Ok(model);
        }

        [SwaggerOperation(summary: "Update entity in SpecificationAttribute", OperationId = "UpdateSpecificationAttribute")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Put([FromBody] SpecificationAttributeDto model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            model = await _mediator.Send(new UpdateSpecificationAttributeCommand() { Model = model });
            return Ok(model);
        }

        [SwaggerOperation(summary: "Partially update entity in SpecificationAttribute", OperationId = "PartiallyUpdateSpecificationAttribute")]
        [HttpPatch]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Patch([FromODataUri] string key, [FromBody] JsonPatchDocument<SpecificationAttributeDto> model)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            var specification = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>(key));
            if (!specification.Any()) return NotFound();

            var spec = specification.FirstOrDefault();
            model.ApplyTo(spec);
            await _mediator.Send(new UpdateSpecificationAttributeCommand() { Model = spec });
            return Ok();
        }

        [SwaggerOperation(summary: "Delete entity in SpecificationAttribute", OperationId = "DeleteSpecificationAttribute")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.SpecificationAttributes)) return Forbid();

            var specification = await _mediator.Send(new GetGenericQuery<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>(key));
            if (!specification.Any()) return NotFound();

            await _mediator.Send(new DeleteSpecificationAttributeCommand() { Model = specification.FirstOrDefault() });

            return Ok();
        }
    }
}
