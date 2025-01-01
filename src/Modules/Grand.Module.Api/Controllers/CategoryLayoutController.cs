using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Constants;
using Grand.Module.Api.Attributes;

namespace Grand.Module.Api.Controllers;

[Route($"{Configurations.RestRoutePrefix}/CategoryLayout")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class CategoryLayoutController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public CategoryLayoutController(
        IMediator mediator,
        IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from CategoryLayout by key", OperationId = "GetCategoryLayoutById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        var layout = await _mediator.Send(new GetGenericQuery<LayoutDto, CategoryLayout>(key));
        if (!layout.Any()) return NotFound();

        return Ok(layout.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from CategoryLayout", OperationId = "GetCategoryLayout")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<LayoutDto, CategoryLayout>()));
    }
}