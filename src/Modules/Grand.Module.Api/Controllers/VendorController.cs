﻿using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Grand.Module.Api.Attributes;
using Microsoft.AspNetCore.Http;

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

    [SwaggerOperation("Get entity from Vendor by key", OperationId = "GetVendorById")]
    [HttpGet("{key}")]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VendorDto))]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Vendors)) return Forbid();

        var vendor = await _mediator.Send(new GetGenericQuery<VendorDto, Vendor>(key));
        if (!vendor.Any()) return NotFound();

        return Ok(vendor.FirstOrDefault());
    }

    [SwaggerOperation("Get entities from Vendor", OperationId = "GetVendors")]
    [HttpGet]
    [EnableQuery]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<VendorDto>))]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(PermissionSystemName.Vendors)) return Forbid();

        return Ok(await _mediator.Send(new GetGenericQuery<VendorDto, Vendor>()));
    }
}