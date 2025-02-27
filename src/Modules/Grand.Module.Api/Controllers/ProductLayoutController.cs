using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Constants;
using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

[Route($"{Configurations.RestRoutePrefix}/ProductLayout")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class ProductLayoutController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public ProductLayoutController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from ProductLayout by key")]
    [EndpointName("GetProductLayoutById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LayoutDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        var layout = await _mediator.Send(new GetGenericQuery<LayoutDto, ProductLayout>(key));
        if (!layout.Any()) return NotFound();

        return Ok(layout.FirstOrDefault());
    }

    [EndpointDescription("Get entities from ProductTemplate")]
    [EndpointName("GetProductTemplates")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LayoutDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();
        return Ok(await _mediator.Send(new GetGenericQuery<LayoutDto, ProductLayout>()));
    }
}