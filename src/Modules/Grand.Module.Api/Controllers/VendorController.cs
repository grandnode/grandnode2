using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Queries.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class VendorController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public VendorController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entity from Vendor by key")]
    [EndpointName("GetVendorById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VendorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Vendors)) return Forbid();

        var vendor = await _mediator.Send(new GetGenericQuery<VendorDto, Vendor>(key));
        if (!vendor.Any()) return NotFound();

        return Ok(vendor.FirstOrDefault());
    }

    [EndpointDescription("Get entities from Vendor")]
    [EndpointName("GetVendors")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VendorDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Vendors)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<VendorDto, Vendor>()));
    }
}