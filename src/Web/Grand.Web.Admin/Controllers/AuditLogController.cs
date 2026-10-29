using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Audit;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Models.AuditLog;
using Grand.Web.Common.Audit;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace Grand.Web.Admin.Controllers;

/// <summary>
///     The staff audit trail: list, entry detail and CSV export. Main Admin only; retention is set in appsettings.
/// </summary>
[PermissionAuthorize(PermissionSystemName.AuditLog)]
public class AuditLogController(
    IAuditService auditService,
    IAuditEventRecorder auditEventRecorder,
    IStoreService storeService,
    IRequestTranslationService translationService) : BaseAdminController
{
    private const int DefaultDays = 7;
    private const int ChangedFieldsLength = 200;
    private const int HistoryPageSize = 20;
    private const int TypeHistorySize = 100;
    private const int HistoryValueLength = 100;

    private static readonly string[] CsvColumns = [
        "CreatedOnUtc", "Kind", "Source", "ActorEmail", "ActorCustomerId", "ImpersonatedCustomerId", "StoreId",
        "VendorId", "IpAddress", "UserAgent", "Action", "OperationId", "EntityType", "EntityId", "EntityName",
        "EntityStoreId",
        "Path", "OldValue", "NewValue", "Masked", "Truncated", "Details"
    ];

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List(string entityType, string entityId, string operationId)
    {
        var model = new AuditLogListModel {
            EntityType = entityType,
            EntityId = entityId,
            OperationId = operationId,
            //a link to one record or one operation shows its whole history
            FromUtc = string.IsNullOrEmpty(entityId) && string.IsNullOrEmpty(operationId)
                ? DateTime.UtcNow.AddDays(-DefaultDays)
                : null
        };
        await PrepareLists(model);
        await auditEventRecorder.AuditLogViewed();
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command, AuditLogListModel model)
    {
        var entries = await auditService.GetEntries(ToSearch(model), command.Page - 1, command.PageSize);
        var stores = await StoreNames();
        return Json(new DataSourceResult {
            Data = entries.Select(e => ToRow(e, stores, false)).ToList(),
            Total = entries.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Detail(string id)
    {
        var entry = await auditService.GetById(id);
        if (entry == null)
            return NotFound();

        return View(new AuditEntryModel {
            Entry = entry,
            Source = SourceName(entry.Source),
            Kind = KindName(entry.Kind),
            AppliesTo = AppliesTo(entry, await StoreNames()),
            Changes = entry.Changes.Select(ToChange).ToList()
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    public async Task<IActionResult> ExportCsv(AuditLogListModel model)
    {
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers.ContentDisposition = $"attachment; filename=auditlog-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        var count = 0;
        var complete = false;
        try
        {
            await using (var writer = new StreamWriter(Response.Body, new UTF8Encoding(true), 4096, true))
            {
                writer.NewLine = "\r\n";
                await writer.WriteLineAsync(string.Join(",", CsvColumns));
                await foreach (var entry in auditService.StreamEntries(ToSearch(model), HttpContext.RequestAborted))
                {
                    count++;
                    if (entry.Changes.Count == 0)
                        await writer.WriteLineAsync(CsvRow(entry, null));
                    foreach (var change in entry.Changes)
                        await writer.WriteLineAsync(CsvRow(entry, change));
                }
            }

            complete = true;
        }
        finally
        {
            //access to the log is logged, also when the download stops part-way
            await auditEventRecorder.AuditLogExported(
                FilterSummary(model) + (complete ? "" : "; incomplete"), count);
        }

        return new EmptyResult();
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public IActionResult HistoryPopup(string entityType, string entityId, string entityNames)
    {
        return View(new AuditHistoryPopupModel {
            EntityType = entityType, EntityId = entityId, EntityNames = entityNames
        });
    }

    /// <summary>
    ///     The history of one record, a page at a time, or the newest entries of a whole entity type
    /// </summary>
    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> HistoryList(DataSourceRequest command, string entityType, string entityId,
        string entityNames)
    {
        var search = new AuditEntrySearch {
            EntityType = entityType,
            EntityId = entityId,
            EntityNames = entityNames?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        };
        var entries = string.IsNullOrEmpty(entityId)
            ? await auditService.GetEntries(search, 0, TypeHistorySize)
            : await auditService.GetEntries(search, command.Page - 1, HistoryPageSize);
        var stores = await StoreNames();
        return Json(new DataSourceResult {
            Data = entries.Select(e => ToRow(e, stores, true)).ToList(),
            Total = string.IsNullOrEmpty(entityId) ? entries.Count : entries.TotalCount
        });
    }

    private async Task PrepareLists(AuditLogListModel model)
    {
        var all = translationService.GetResource("Admin.Common.All");
        model.AvailableEntityTypes.Add(new SelectListItem(all, ""));
        foreach (var type in await auditService.GetEntityTypes())
            model.AvailableEntityTypes.Add(new SelectListItem(type, type));

        model.AvailableSources.Add(new SelectListItem(all, ""));
        foreach (var source in Enum.GetValues<AuditSource>())
            model.AvailableSources.Add(new SelectListItem(SourceName(source), source.ToString()));

        model.AvailableKinds.Add(new SelectListItem(all, ""));
        foreach (var kind in Enum.GetValues<AuditKind>())
            model.AvailableKinds.Add(new SelectListItem(KindName(kind), kind.ToString()));

        model.AvailableStores.Add(new SelectListItem(all, ""));
        foreach (var store in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem(store.Shortcut ?? store.Name, store.Id));
    }

    private static AuditEntrySearch ToSearch(AuditLogListModel model)
    {
        return new AuditEntrySearch {
            FromUtc = AsUtc(model.FromUtc),
            ToUtc = AsUtc(model.ToUtc?.Date.AddDays(1).AddTicks(-1)),
            ActorEmail = model.ActorEmail?.Trim(),
            Source = model.Source,
            Kind = model.Kind,
            EntityType = model.EntityType,
            EntityId = model.EntityId?.Trim(),
            StoreId = model.StoreId,
            OperationId = model.OperationId?.Trim()
        };
    }

    //the filter fields are labelled UTC, but bind as Unspecified, which the database driver would read as server time
    private static DateTime? AsUtc(DateTime? value)
    {
        return value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }

    private async Task<IDictionary<string, string>> StoreNames()
    {
        return (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut ?? s.Name);
    }

    private string AppliesTo(AuditEntry entry, IDictionary<string, string> stores)
    {
        if (entry.EntityStoreId == null)
            return null;
        if (entry.EntityStoreId == "")
            return translationService.GetResource("Admin.System.AuditLog.AllStores");
        return stores.TryGetValue(entry.EntityStoreId, out var name) ? name : entry.EntityStoreId;
    }

    //the list shows only the changed field names; the values are for the change history popup
    private AuditEntryRowModel ToRow(AuditEntry entry, IDictionary<string, string> stores, bool withValues)
    {
        var fields = string.Join(", ", entry.Changes.Select(c => c.Path));
        var appliesTo = AppliesTo(entry, stores);
        return new AuditEntryRowModel {
            Id = entry.Id,
            CreatedOn = entry.CreatedOnUtc.ToString("yyyy-MM-dd HH:mm:ss"),
            ActorEmail = entry.ActorEmail,
            Source = SourceName(entry.Source),
            Kind = KindName(entry.Kind),
            What = string.Join(" ", new[] { entry.EntityType, entry.EntityName }.Where(s => !string.IsNullOrEmpty(s)))
                   + (appliesTo == null ? "" : $" ({appliesTo})"),
            ChangedFields = fields.Length > ChangedFieldsLength ? fields[..ChangedFieldsLength] + "…" : fields,
            ChangesText = withValues
                ? string.Join(" · ", entry.Changes.Select(ToChange).Select(c =>
                    $"{c.Path}: {Shorten(c.OldValue)} → {Shorten(c.NewValue)}"))
                : null
        };
    }

    private static string Shorten(string value)
    {
        if (value == null)
            return "—";
        return value.Length > HistoryValueLength ? value[..HistoryValueLength] + "…" : value;
    }

    private AuditChangeModel ToChange(AuditChange change)
    {
        if (change.Masked)
        {
            var hidden = translationService.GetResource("Admin.System.AuditLog.ChangedHidden");
            return new AuditChangeModel { Path = change.Path, OldValue = hidden, NewValue = hidden };
        }

        var suffix = change.Truncated
            ? $" … ({translationService.GetResource("Admin.System.AuditLog.Truncated")})"
            : "";
        return new AuditChangeModel {
            Path = change.Path,
            OldValue = change.OldValue == null ? null : change.OldValue + suffix,
            NewValue = change.NewValue == null ? null : change.NewValue + suffix
        };
    }

    private string SourceName(AuditSource source)
    {
        return translationService.GetResource($"Enums.Grand.Domain.Audit.AuditSource.{source}");
    }

    private string KindName(AuditKind kind)
    {
        return translationService.GetResource($"Enums.Grand.Domain.Audit.AuditKind.{kind}");
    }

    private static string FilterSummary(AuditLogListModel model)
    {
        var parts = new List<string>();
        void Add(string name, object value)
        {
            if (value != null && value.ToString() != "")
                parts.Add($"{name}={value}");
        }

        Add("from", model.FromUtc?.ToString("yyyy-MM-dd"));
        Add("to", model.ToUtc?.ToString("yyyy-MM-dd"));
        Add("actor", model.ActorEmail);
        Add("source", model.Source);
        Add("kind", model.Kind);
        Add("entity type", model.EntityType);
        Add("entity id", model.EntityId);
        Add("store", model.StoreId);
        Add("operation", model.OperationId);
        return parts.Count == 0 ? "none" : string.Join("; ", parts);
    }

    private static string CsvRow(AuditEntry entry, AuditChange change)
    {
        string[] values = [
            entry.CreatedOnUtc.ToString("o"), entry.Kind.ToString(), entry.Source.ToString(), entry.ActorEmail,
            entry.ActorCustomerId, entry.ImpersonatedCustomerId, entry.StoreId, entry.VendorId, entry.IpAddress,
            entry.UserAgent, entry.Action, entry.OperationId, entry.EntityType, entry.EntityId, entry.EntityName,
            entry.EntityStoreId,
            change?.Path, change?.OldValue, change?.NewValue, change?.Masked.ToString(), change?.Truncated.ToString(),
            entry.Details
        ];
        return string.Join(",", values.Select(CsvCell));
    }

    //RFC 4180 quoting, and a leading quote mark so a spreadsheet never runs a cell as a formula
    private static string CsvCell(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
