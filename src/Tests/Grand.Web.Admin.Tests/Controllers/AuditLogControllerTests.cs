using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain;
using Grand.Domain.Audit;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Controllers;
using Grand.Web.Admin.Models.AuditLog;
using Grand.Web.Common.Audit;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class AuditLogControllerTests
{
    private Mock<IAuditService> _auditService;
    private Mock<IAuditEventRecorder> _recorder;
    private AuditLogController _controller;
    private DefaultHttpContext _httpContext;

    [TestInitialize]
    public void Setup()
    {
        _auditService = new Mock<IAuditService>();
        _auditService.Setup(s => s.GetEntityTypes()).ReturnsAsync(new List<string> { "Product" });
        _recorder = new Mock<IAuditEventRecorder>();
        var storeService = new Mock<IStoreService>();
        storeService.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> { new() { Id = "s2", Shortcut = "Store B" } });
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        _controller = new AuditLogController(_auditService.Object, _recorder.Object,
            storeService.Object, translation.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };
    }

    private static async IAsyncEnumerable<AuditEntry> Stream(params AuditEntry[] entries)
    {
        foreach (var entry in entries)
            yield return entry;
        await Task.CompletedTask;
    }

    private string ResponseText()
    {
        return Encoding.UTF8.GetString(((MemoryStream)_httpContext.Response.Body).ToArray()).TrimStart('﻿');
    }

    [TestMethod]
    public async Task List_Get_RecordsAuditLogViewed_DefaultsToLastSevenDays()
    {
        var result = (ViewResult)await _controller.List(null, null, null);

        var model = (AuditLogListModel)result.Model!;
        Assert.IsTrue((DateTime.UtcNow.AddDays(-7) - model.FromUtc!.Value).Duration() < TimeSpan.FromMinutes(1));
        _recorder.Verify(r => r.AuditLogViewed(), Times.Once);
    }

    [TestMethod]
    public async Task List_Get_ForOneRecord_ShowsWholeHistory()
    {
        var result = (ViewResult)await _controller.List("Product", "p1", null);

        var model = (AuditLogListModel)result.Model!;
        Assert.AreEqual("Product", model.EntityType);
        Assert.AreEqual("p1", model.EntityId);
        Assert.IsNull(model.FromUtc);
    }

    [TestMethod]
    public async Task List_Post_MapsFiltersToSearch()
    {
        AuditEntrySearch search = null;
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 1, 20))
            .Callback<AuditEntrySearch, int, int>((s, _, _) => search = s)
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry> {
                new() { EntityType = "Product", EntityName = "Lamp", Changes = [new AuditChange { Path = "Price" }] }
            }, 0, 20));

        var result = (JsonResult)await _controller.List(new DataSourceRequest { Page = 2, PageSize = 20 },
            new AuditLogListModel { EntityType = "Product", EntityId = "p1", SearchText = "anna", Kind = AuditKind.Update });

        Assert.AreEqual("Product", search.EntityType);
        Assert.AreEqual("p1", search.EntityId);
        Assert.AreEqual("anna", search.SearchText);
        Assert.AreEqual(AuditKind.Update, search.Kind);
        var data = ((DataSourceResult)result.Value!).Data.Cast<AuditEntryRowModel>().Single();
        Assert.AreEqual("Price", data.ChangedFields);
        Assert.IsNull(data.ChangesText);
    }

    [TestMethod]
    public async Task List_Post_DatesAreUtc()
    {
        AuditEntrySearch search = null;
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 0, 20))
            .Callback<AuditEntrySearch, int, int>((s, _, _) => search = s)
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry>(), 0, 20));

        //as model binding produces them from the date inputs
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 20 }, new AuditLogListModel {
            FromUtc = new DateTime(2026, 10, 21), ToUtc = new DateTime(2026, 10, 22)
        });

        Assert.AreEqual(DateTimeKind.Utc, search.FromUtc!.Value.Kind);
        Assert.AreEqual(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), search.FromUtc);
        Assert.AreEqual(DateTimeKind.Utc, search.ToUtc!.Value.Kind);
        Assert.AreEqual(new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1), search.ToUtc);
    }

    [TestMethod]
    public async Task Detail_NotFound_Returns404()
    {
        Assert.IsInstanceOfType<NotFoundResult>(await _controller.Detail("missing"));
    }

    [TestMethod]
    public async Task ExportCsv_WritesHeaderAndOneRowPerChange()
    {
        _auditService.Setup(s => s.StreamEntries(It.IsAny<AuditEntrySearch>(), It.IsAny<CancellationToken>()))
            .Returns(Stream(new AuditEntry {
                Kind = AuditKind.Update, EntityType = "Product",
                Changes = [new AuditChange { Path = "Price", OldValue = "1", NewValue = "2" }, new AuditChange { Path = "Name" }]
            }, new AuditEntry { Kind = AuditKind.SignIn }));

        await _controller.ExportCsv(new AuditLogListModel());

        Assert.AreEqual("text/csv; charset=utf-8", _httpContext.Response.ContentType);
        var lines = ResponseText().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(4, lines.Length);
        StringAssert.StartsWith(lines[0], "CreatedOnUtc,Kind,Source,ActorEmail");
        StringAssert.Contains(lines[1], ",Price,1,2,");
    }

    [TestMethod]
    public async Task ExportCsv_EscapesQuotesAndFormulaPrefix()
    {
        _auditService.Setup(s => s.StreamEntries(It.IsAny<AuditEntrySearch>(), It.IsAny<CancellationToken>()))
            .Returns(Stream(new AuditEntry {
                Kind = AuditKind.Update,
                Changes = [new AuditChange { Path = "Name", OldValue = "say \"hi\", ok", NewValue = "=HYPERLINK(1)" }]
            }));

        await _controller.ExportCsv(new AuditLogListModel());

        var text = ResponseText();
        StringAssert.Contains(text, "\"say \"\"hi\"\", ok\"");
        StringAssert.Contains(text, "'=HYPERLINK(1)");
    }

    private static async IAsyncEnumerable<AuditEntry> FailingStream()
    {
        yield return new AuditEntry();
        await Task.CompletedTask;
        throw new OperationCanceledException("client went away");
    }

    [TestMethod]
    public async Task ExportCsv_StreamFailsMidway_StillRecordsAuditLogExported()
    {
        _auditService.Setup(s => s.StreamEntries(It.IsAny<AuditEntrySearch>(), It.IsAny<CancellationToken>()))
            .Returns(FailingStream());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => _controller.ExportCsv(new AuditLogListModel()));

        _recorder.Verify(r => r.AuditLogExported(It.Is<string>(s => s.Contains("incomplete")), 1), Times.Once);
    }

    [TestMethod]
    public async Task ExportCsv_HasEntityStoreIdColumn()
    {
        _auditService.Setup(s => s.StreamEntries(It.IsAny<AuditEntrySearch>(), It.IsAny<CancellationToken>()))
            .Returns(Stream(new AuditEntry { EntityType = "Setting", EntityStoreId = "s2" }));

        await _controller.ExportCsv(new AuditLogListModel());

        var lines = ResponseText().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var columns = lines[0].Split(',');
        var cells = lines[1].Split(',');
        Assert.AreEqual("s2", cells[Array.IndexOf(columns, "EntityStoreId")]);
    }

    [TestMethod]
    public async Task ExportCsv_RecordsAuditLogExported()
    {
        _auditService.Setup(s => s.StreamEntries(It.IsAny<AuditEntrySearch>(), It.IsAny<CancellationToken>()))
            .Returns(Stream(new AuditEntry(), new AuditEntry()));

        await _controller.ExportCsv(new AuditLogListModel { EntityType = "Product" });

        _recorder.Verify(r => r.AuditLogExported(It.Is<string>(s => s.Contains("Product")), 2), Times.Once);
    }

    [TestMethod]
    public void Controller_HasPermissionAuthorizeAuditLog()
    {
        var attribute = typeof(AuditLogController).GetCustomAttributes(typeof(PermissionAuthorizeAttribute), true)
            .Cast<PermissionAuthorizeAttribute>().Single();

        Assert.AreEqual(PermissionSystemName.AuditLog, attribute.Permission);
    }

    [TestMethod]
    public void HistoryPopup_PassesTypeAndId()
    {
        var result = (ViewResult)_controller.HistoryPopup("Product", "p1", null);

        var model = (AuditHistoryPopupModel)result.Model!;
        Assert.AreEqual("Product", model.EntityType);
        Assert.AreEqual("p1", model.EntityId);
    }

    [TestMethod]
    public async Task HistoryList_WithId_PageSize20()
    {
        AuditEntrySearch search = null;
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 2, 20))
            .Callback<AuditEntrySearch, int, int>((s, _, _) => search = s)
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry> {
                new() { Changes = [new AuditChange { Path = "Price", OldValue = "129", NewValue = "99" }] }
            }, 0, 20));

        var result = (JsonResult)await _controller.HistoryList(new DataSourceRequest { Page = 3, PageSize = 50 },
            "Product", "p1", null);

        Assert.AreEqual("Product", search.EntityType);
        Assert.AreEqual("p1", search.EntityId);
        var row = ((DataSourceResult)result.Value!).Data.Cast<AuditEntryRowModel>().Single();
        Assert.AreEqual("Price: 129 → 99", row.ChangesText);
    }

    [TestMethod]
    public async Task HistoryList_EntityNames_FilterTheSettingClasses()
    {
        AuditEntrySearch search = null;
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 0, 100))
            .Callback<AuditEntrySearch, int, int>((s, _, _) => search = s)
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry>(), 0, 100));

        await _controller.HistoryList(new DataSourceRequest(), "Setting", null, "catalogsettings, ordersettings");

        CollectionAssert.AreEqual(new[] { "catalogsettings", "ordersettings" }, search.EntityNames.ToArray());
    }

    [TestMethod]
    public async Task Rows_ShowWhichStoreTheRecordAppliesTo()
    {
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 0, 100))
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry> {
                new() { EntityType = "Setting", EntityName = "catalogsettings", EntityStoreId = "" },
                new() { EntityType = "Setting", EntityName = "catalogsettings", EntityStoreId = "s2" },
                new() { EntityType = "Product", EntityName = "Lamp" }
            }, 0, 100));

        var result = (JsonResult)await _controller.HistoryList(new DataSourceRequest(), "Setting", null, null);

        var what = ((DataSourceResult)result.Value!).Data.Cast<AuditEntryRowModel>().Select(r => r.What).ToArray();
        CollectionAssert.AreEqual(new[] {
            "Setting catalogsettings (Admin.System.AuditLog.AllStores)",
            "Setting catalogsettings (Store B)",
            "Product Lamp"
        }, what);
    }

    [TestMethod]
    public async Task HistoryList_TypeOnly_Newest100()
    {
        AuditEntrySearch search = null;
        _auditService.Setup(s => s.GetEntries(It.IsAny<AuditEntrySearch>(), 0, 100))
            .Callback<AuditEntrySearch, int, int>((s, _, _) => search = s)
            .ReturnsAsync(new PagedList<AuditEntry>(new List<AuditEntry>(), 0, 100));

        await _controller.HistoryList(new DataSourceRequest { Page = 4, PageSize = 20 }, "SalesEmployee", null, null);

        Assert.AreEqual("SalesEmployee", search.EntityType);
        Assert.IsNull(search.EntityId);
    }

    [TestMethod]
    public void AuditLogViews_NeverRenderStoredValuesRaw()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Web")))
            dir = dir.Parent;
        Assert.IsNotNull(dir, "repository root not found");
        var views = Directory.GetFiles(
            Path.Combine(dir.FullName, "src", "Web", "Grand.Web.Admin", "Areas", "Admin", "Views", "AuditLog"),
            "*.cshtml");

        Assert.IsTrue(views.Length >= 3);
        foreach (var view in views)
        {
            var source = File.ReadAllText(view);
            Assert.IsFalse(source.Contains("Html.Raw"), view);
            Assert.IsFalse(source.Contains("{{{"), view);
        }
    }
}
