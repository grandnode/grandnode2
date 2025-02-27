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

public class CurrencyController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CurrencyController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Currency by key")]
    [EndpointName("GetCurrencyById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CurrencyDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Currencies)) return Forbid();

        var currency = await _mediator.Send(new GetGenericQuery<CurrencyDto, Currency>(key));
        if (!currency.Any()) return NotFound();

        return Ok(currency.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Currency")]
    [EndpointName("GetCurrencies")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CurrencyDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Currencies)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<CurrencyDto, Currency>()));
    }
}