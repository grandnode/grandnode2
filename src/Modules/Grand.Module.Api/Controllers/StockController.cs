using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Module.Api.Commands.Models.Inventory;
using Grand.Module.Api.DTOs.Inventory;
using Grand.Module.Api.Queries.Models.Inventory;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Module.Api.Controllers;

/// <summary>
///     Stock integration for external systems (ERP/WMS): a bulk write with idempotency keys, and a journal feed to pull
/// </summary>
public class StockController : BaseApiController
{
    private const int MaxLines = 1000;

    private readonly IMediator _mediator;
    private readonly IPermissionService _permissionService;

    public StockController(IMediator mediator, IPermissionService permissionService)
    {
        _mediator = mediator;
        _permissionService = permissionService;
    }

    [EndpointDescription("Apply stock changes: each line sets a target or changes by a difference, with an idempotency key")]
    [EndpointName("ApplyStock")]
    [HttpPost("Apply")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IList<StockApplyResultDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Apply([FromBody] StockApplyRequestDto model)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.StockIntegration, CurrentCustomer)) return Forbid();

        if (model?.Lines == null || model.Lines.Count > MaxLines || model.Lines.Any(x => x == null))
            return BadRequest($"A stock write holds at most {MaxLines} lines, none of them empty.");

        var results = await _mediator.Send(new ApplyStockCommand {
            Lines = model.Lines, CustomerId = CurrentCustomer?.Id ?? ""
        });
        return Ok(results);
    }

    [EndpointDescription("Get inventory journal entries, page by page from a cursor")]
    [EndpointName("GetStockJournal")]
    [HttpGet("Journal")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StockJournalResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Journal([FromQuery] string after, [FromQuery] int take = 100,
        [FromQuery] string warehouseId = null, [FromQuery] string warehouseCode = null,
        [FromQuery] string movementTypes = null, [FromQuery] bool excludeOwn = false)
    {
        if (!await _permissionService.Authorize(PermissionSystemName.StockIntegration, CurrentCustomer)) return Forbid();

        //an unknown type is refused: ignoring it would widen the filter to every movement
        var types = new List<InventoryMovementType>();
        foreach (var part in (movementTypes ?? "").Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<InventoryMovementType>(part, true, out var type) || !Enum.IsDefined(type))
                return BadRequest($"Unknown movement type '{part}'.");
            types.Add(type);
        }

        return Ok(await _mediator.Send(new GetStockJournalQuery {
            After = after,
            Take = take,
            WarehouseId = warehouseId,
            WarehouseCode = warehouseCode,
            MovementTypes = types,
            ExcludeOwn = excludeOwn,
            CustomerId = CurrentCustomer?.Id ?? ""
        }));
    }
}
