﻿using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Directory;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Constants;

namespace Grand.Module.Api.Controllers.OData;

[Route($"{Configurations.ODataRoutePrefix}/Country")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class CountryController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CountryController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from Country by key", OperationId = "GetCountryById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Countries)) return Forbid();

        //Domain.Directory.Country
        var country = await _mediator.Send(new GetGenericQuery<CountryDto, Country>(key));
        if (!country.Any()) return NotFound();

        return Ok(country.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from Country", OperationId = "GetCountries")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Countries)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CountryDto, Country>()));
    }
}