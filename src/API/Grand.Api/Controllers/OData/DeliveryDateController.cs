using Grand.Api.DTOs.Shipping;
using Grand.Api.Queries.Models.Common;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using MongoDB.AspNetCore.OData;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Grand.Api.Controllers.OData
{
    [Route("odata/DeliveryDate")]
    [ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
    public class DeliveryDateController : BaseODataController
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;

        public DeliveryDateController(IMediator mediator, IPermissionService permissionService)
        {
            _mediator = mediator;
            _permissionService = permissionService;
        }

        [SwaggerOperation(summary: "Get entity from Delivery Date by key", OperationId = "GetDeliveryDateById")]
        [HttpGet("{key}")]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get([FromRoute] string key)
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

            var deliverydate = await _mediator.Send(new GetGenericQuery<DeliveryDateDto, Domain.Shipping.DeliveryDate>(key));
            if (!deliverydate.Any()) return NotFound();

            return Ok(deliverydate.FirstOrDefault());
        }

        [SwaggerOperation(summary: "Get entities from Delivery Date", OperationId = "GetDeliveryDates")]
        [HttpGet]
        [MongoEnableQuery(HandleNullPropagation = HandleNullPropagationOption.False)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            if (!await _permissionService.Authorize(PermissionSystemName.ShippingSettings)) return Forbid();

            return Ok(await _mediator.Send(new GetGenericQuery<DeliveryDateDto, Domain.Shipping.DeliveryDate>()));
        }
    }
}
