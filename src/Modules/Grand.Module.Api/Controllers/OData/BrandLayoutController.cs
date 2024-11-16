using Grand.Module.Api.DTOs.Common;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Module.Api.Controllers.OData;

[Route($"{Configurations.ODataRoutePrefix}/BrandLayout")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
public class BrandLayoutController : BaseODataController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public BrandLayoutController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [SwaggerOperation("Get entity from BrandLayout by key", OperationId = "GetBrandLayoutById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get(string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        var layout = await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>(key));
        if (!layout.Any()) return NotFound();

        return Ok(layout.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from BrandLayout", OperationId = "GetBrandLayouts")]
    [HttpGet]
    [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Maintenance)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<LayoutDto, BrandLayout>()));
    }
}