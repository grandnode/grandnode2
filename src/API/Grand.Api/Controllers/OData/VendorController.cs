using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    public partial class VendorController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public VendorController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Vendor by key", OperationId = "GetVendorById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Vendors))
                return Forbid();

            var vendor = await _mediator.Send(new GetGenericQuery<VendorDto, Domain.Vendors.Vendor>(key));
            if (!vendor.Any())
                return NotFound();

            return Ok(vendor.FirstOrDefault());

        }

        [SwaggerOperation(summary: "Get entities from Vendor", OperationId = "GetVendors")]
        [HttpGet]
        [EnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.Vendors))
                return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<VendorDto, Domain.Vendors.Vendor>()));
        }
    }
}
