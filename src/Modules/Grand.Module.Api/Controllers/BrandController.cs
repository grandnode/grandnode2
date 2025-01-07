﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Http;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class BrandController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public BrandController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entities from Brand")]
    [EndpointName("GetBrands")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<BrandDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<BrandDto, Brand>()));
    }

    [EndpointDescription("Get entity from Brand by key")]
    [EndpointName("GetBrandById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BrandDto))]
    public async Task<IActionResult> GetById([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Brand>(key));
        if (!brand.Any()) return NotFound();

        return Ok(brand.FirstOrDefault());
    }

    [EndpointDescription("Add new entity to Brand")]
    [EndpointName("InsertBrand")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BrandDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Post([FromBody] BrandDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        model = await _mediator.Send(new AddBrandCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Update entity in Brand")]
    [EndpointName("UpdateBrand")]
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BrandDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Put([FromBody] BrandDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        model = await _mediator.Send(new UpdateBrandCommand { Model = model });
        return Ok(model);
    }

    [EndpointDescription("Partially update entity in Brand")]
    [EndpointName("PartiallyUpdateBrand")]
    [HttpPatch("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]

    ///sample
    ///{
    /// "op": "replace",
    /// "path": "/name",
    /// "value": "new name"
    ///}
    public async Task<IActionResult> Patch([FromRoute] string key, [FromBody] JsonPatchDocument<BrandDto> model)
    {
        if (string.IsNullOrEmpty(key))
            return BadRequest("Key is null or empty");

        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Brand>(key));
        if (!brand.Any()) return NotFound();

        var man = brand.FirstOrDefault();
        model.ApplyTo(man);
        await _mediator.Send(new UpdateBrandCommand { Model = man });
        return Ok();
    }

    [EndpointDescription("Delete entity in Brand")]
    [EndpointName("DeleteBrand")]
    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Brands)) return Forbid();

        var brand = await _mediator.Send(new GetGenericQuery<BrandDto, Brand>(key));
        if (!brand.Any()) return NotFound();

        await _mediator.Send(new DeleteBrandCommand { Model = brand.FirstOrDefault() });
        return Ok();
    }
}