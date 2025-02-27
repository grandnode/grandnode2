using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class LanguageController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public LanguageController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Languages by key")]
    [EndpointName("GetLanguageById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LanguageDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Languages)) return Forbid();

        var language = await _mediator.Send(new GetGenericQuery<LanguageDto, Language>(key));
        if (!language.Any()) return NotFound();

        return Ok(language.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Languages")]
    [EndpointName("GetLanguages")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LanguageDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Languages)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<LanguageDto, Language>()));
    }
}