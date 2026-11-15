using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Mediator;
using Grand.Module.Api.Attributes;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Extensions;
using Grand.Module.Api.Queries.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

public class MerchandiseReturnController : BaseApiController
{
    private const int MaxPageSize = 1000;

    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public MerchandiseReturnController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Get entities from MerchandiseReturn")]
    [EndpointName("GetMerchandiseReturns")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnListModel))]
    public async Task<IActionResult> Get(
        [FromQuery] string storeId = "", [FromQuery] string customerId = "", [FromQuery] string orderItemId = "",
        [FromQuery] string vendorId = "", [FromQuery] string ownerId = "", [FromQuery] string orderId = "",
        [FromQuery] int? status = null, [FromQuery] string returnShippingProvider = "",
        [FromQuery] string reasonId = "", [FromQuery] bool openOnly = false,
        [FromQuery] DateTime? createdFromUtc = null, [FromQuery] DateTime? createdToUtc = null,
        [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 100)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.MerchandiseReturns, CurrentCustomer))
            return Forbid();

        pageSize = Math.Min(pageSize, MaxPageSize);

        var result = await _mediator.Send(new GetMerchandiseReturnsQuery {
            StoreId = storeId,
            CustomerId = customerId,
            OrderItemId = orderItemId,
            VendorId = vendorId,
            OwnerId = ownerId,
            OrderId = orderId,
            Status = status.HasValue ? (MerchandiseReturnStatus?)status.Value : null,
            ReturnShippingProvider = returnShippingProvider,
            ReasonId = reasonId,
            OpenOnly = openOnly,
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            PageIndex = pageIndex,
            PageSize = pageSize
        });

        var items = result.Select(Map).ToList();
        return Ok(new MerchandiseReturnListModel { Items = items, TotalCount = result.TotalCount });
    }

    [EndpointDescription("Get entity from MerchandiseReturn by key")]
    [EndpointName("GetMerchandiseReturnById")]
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnDto))]
    public async Task<IActionResult> GetById([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.MerchandiseReturns, CurrentCustomer))
            return Forbid();

        var merchandiseReturn = await _mediator.Send(new GetMerchandiseReturnQuery { Id = key });
        if (merchandiseReturn == null) return NotFound();

        return Ok(Map(merchandiseReturn));
    }

    [EndpointDescription("Update entity in MerchandiseReturn")]
    [EndpointName("UpdateMerchandiseReturn")]
    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnDto))]
    public async Task<IActionResult> Update([FromRoute] string key, [FromBody] MerchandiseReturnDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.MerchandiseReturns, CurrentCustomer))
            return Forbid();

        var error = await _mediator.Send(new UpdateMerchandiseReturnCommand { Id = key, Model = model });
        if (error != null) return ErrorResult(error);

        return Ok(await Load(key));
    }

    [EndpointDescription("Delete entity in MerchandiseReturn")]
    [EndpointName("DeleteMerchandiseReturn")]
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromRoute] string key)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.MerchandiseReturns, CurrentCustomer))
            return Forbid();

        var error = await _mediator.Send(new DeleteMerchandiseReturnCommand { Id = key });
        if (error != null) return ErrorResult(error);

        return Ok();
    }

    [EndpointDescription("Change the status of a MerchandiseReturn")]
    [EndpointName("ChangeMerchandiseReturnStatus")]
    [HttpPost("{key}/status")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MerchandiseReturnDto))]
    public async Task<IActionResult> ChangeStatus([FromRoute] string key,
        [FromBody] MerchandiseReturnStatusChangeRequest model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.MerchandiseReturns, CurrentCustomer))
            return Forbid();

        if (model == null || !model.Status.HasValue ||
            !Enum.IsDefined(typeof(MerchandiseReturnStatus), model.Status.Value))
            return BadRequest();

        var error = await _mediator.Send(new ChangeMerchandiseReturnStatusCommand {
            Id = key,
            Status = model.Status.Value,
            CustomerNote = model.CustomerNote,
            ChangedBy = CurrentCustomer.StatusChangedBy()
        });
        if (error != null) return ErrorResult(error);

        return Ok(await Load(key));
    }

    private async Task<MerchandiseReturnDto> Load(string id)
    {
        var merchandiseReturn = await _mediator.Send(new GetMerchandiseReturnQuery { Id = id });
        return merchandiseReturn == null ? null : Map(merchandiseReturn);
    }

    private static MerchandiseReturnDto Map(MerchandiseReturn entity)
    {
        var model = entity.ToModel();
        model.NextStatuses = entity.MerchandiseReturnStatus.NextStatuses().Select(x => (int)x).ToList();
        return model;
    }

    private IActionResult ErrorResult(string error)
    {
        return error == MerchandiseReturnErrors.NotFound
            ? NotFound()
            : Conflict(new { error });
    }
}
