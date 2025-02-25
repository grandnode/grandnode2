using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Directory;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class CountryController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CountryController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Country by key")]
    [EndpointName("GetCountryById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Countries)) return Forbid();

        //Domain.Directory.Country
        var country = await _mediator.Send(new GetGenericQuery<CountryDto, Country>(key));
        if (!country.Any()) return NotFound();

        return Ok(country.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Country")]
    [EndpointName("GetCountries")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CountryDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Countries)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CountryDto, Country>()));
    }
}