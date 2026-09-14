using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin;
using Grand.Web.Common.TagHelpers.Admin.Grid;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.TagHelpers;

[TestClass]
public class AdminGridTagHelperTests
{
    private AdminAreaSettings _adminAreaSettings;
    private LanguageSettings _languageSettings;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IUrlHelper> _urlHelperMock;
    private Mock<IUrlHelperFactory> _urlHelperFactoryMock;
    private Language _workingLanguage;
    private ViewContext _viewContext;
    private CultureInfo _originalCulture;

    [TestInitialize]
    public void Init()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        _adminAreaSettings = new AdminAreaSettings { DefaultGridPageSize = 15, GridPageSizes = "10, 15, 20, x, 50" };
        _languageSettings = new LanguageSettings();
        _workingLanguage = new Language { Id = "lang1", Rtl = false };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.WorkingLanguage).Returns(() => _workingLanguage);
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => $"[{key}]");
        _translationServiceMock
            .Setup(x => x.GetResource(It.IsAny<string>(), "lang1", string.Empty, true))
            .Returns((string key, string _, string _, bool _) => $"[{key}]");
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns((UrlActionContext c) =>
                $"/{new RouteValueDictionary(c.Values)["area"]}/{c.Controller}/{c.Action}");
        _urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        _urlHelperFactoryMock.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(_urlHelperMock.Object);
        _viewContext = new ViewContext { RouteData = new RouteData() };
        _viewContext.RouteData.Values["area"] = "Store";
    }

    [TestCleanup]
    public void Cleanup()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private AdminGridTagHelper CreateGrid(Action<AdminGridTagHelper> configure = null)
    {
        var grid = new AdminGridTagHelper(_adminAreaSettings, _languageSettings, _contextAccessorMock.Object,
            _translationServiceMock.Object, _urlHelperFactoryMock.Object) {
            Id = "weights-grid",
            ViewContext = _viewContext
        };
        configure?.Invoke(grid);
        return grid;
    }

    /// <summary>
    ///     A child element: its tag helper and, when it has children of its own, their elements.
    ///     Content is the literal markup of a cell-template.
    /// </summary>
    private sealed record Element(TagHelper Helper, string Content = null, params Element[] Children);

    /// <summary>
    ///     Runs a tag helper the way Razor does: children run inside GetChildContentAsync and see a
    ///     copy of the parent's Items.
    /// </summary>
    private static async Task<TagHelperOutput> Run(TagHelper helper, IDictionary<object, object> items, string tagName,
        string literalContent, Element[] children)
    {
        var context = new TagHelperContext(tagName, new TagHelperAttributeList(), items, Guid.NewGuid().ToString());
        var output = new TagHelperOutput(tagName, new TagHelperAttributeList(), async (_, _) =>
        {
            foreach (var child in children)
                await Run(child.Helper, new Dictionary<object, object>(context.Items), "child", child.Content,
                    child.Children);
            var content = new DefaultTagHelperContent();
            if (literalContent != null) content.AppendHtml(literalContent);
            return content;
        });
        helper.Init(context);
        await helper.ProcessAsync(context, output);
        return output;
    }

    private static Task<TagHelperOutput> RunGrid(AdminGridTagHelper grid, params Element[] children)
    {
        return Run(grid, new Dictionary<object, object>(), "admin-grid", null, children);
    }

    private static JsonElement Config(TagHelperOutput output)
    {
        var json = output.Attributes["data-grand-grid"].Value.ToString();
        return JsonDocument.Parse(json).RootElement;
    }

    private static string Render(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [TestMethod]
    public async Task Process_EmitsGridElementWithRoleAndDefaults()
    {
        var output = await RunGrid(CreateGrid(g => g.ReadUrl = "/Admin/Measure/Weights"),
            new Element(new GridColumnTagHelper { Field = "Name", Title = "Name", Width = 300 }));

        Assert.AreEqual("div", output.TagName);
        Assert.AreEqual("weights-grid", output.Attributes["id"].Value);
        Assert.AreEqual("grid", output.Attributes["data-role"].Value);
        var config = Config(output);
        Assert.AreEqual("Id", config.GetProperty("key").GetString());
        Assert.AreEqual("/Admin/Measure/Weights", config.GetProperty("transport").GetProperty("read").GetString());
        Assert.AreEqual(15, config.GetProperty("pageSize").GetInt32());
        CollectionAssert.AreEqual(new[] { 10, 15, 20, 50 },
            config.GetProperty("pageSizes").EnumerateArray().Select(x => x.GetInt32()).ToArray());
        Assert.AreEqual("Full", config.GetProperty("pager").GetString());
        Assert.AreEqual("None", config.GetProperty("editMode").GetString());
        Assert.IsFalse(config.TryGetProperty("autoBind", out _));
        Assert.IsFalse(config.TryGetProperty("rtl", out _));
        var column = config.GetProperty("columns")[0];
        Assert.AreEqual("Name", column.GetProperty("field").GetString());
        Assert.AreEqual(300, column.GetProperty("width").GetInt32());
    }

    [TestMethod]
    public async Task Process_CompactPager_HasNoPageSizeLikeKendoGridsWithoutPageSize()
    {
        var output = await RunGrid(CreateGrid(g => g.Pager = GridPager.Compact));

        var config = Config(output);
        Assert.AreEqual("Compact", config.GetProperty("pager").GetString());
        Assert.IsFalse(config.TryGetProperty("pageSize", out _));
        Assert.IsFalse(config.TryGetProperty("pageSizes", out _));
    }

    [TestMethod]
    public async Task Process_ActionAttributes_UseAreaFromRouteData()
    {
        var output = await RunGrid(CreateGrid(g =>
        {
            g.Controller = "Tax";
            g.ReadAction = "Categories";
            g.UpdateAction = "CategoryUpdate";
            g.DestroyUrl = "/explicit/wins";
            g.DestroyAction = "CategoryDelete";
        }));

        var transport = Config(output).GetProperty("transport");
        Assert.AreEqual("/Store/Tax/Categories", transport.GetProperty("read").GetString());
        Assert.AreEqual("/Store/Tax/CategoryUpdate", transport.GetProperty("update").GetString());
        Assert.AreEqual("/explicit/wins", transport.GetProperty("destroy").GetString());
        Assert.IsFalse(transport.TryGetProperty("create", out _));
    }

    [TestMethod]
    public async Task Process_InlineEditing_SerializesEditorsCommandsAndToolbar()
    {
        var grid = CreateGrid(g =>
        {
            g.EditMode = GridEditMode.Inline;
            g.ConfirmDestroy = true;
            g.ReloadAfterSave = false;
            g.AutoBind = false;
        });
        var output = await RunGrid(grid,
            new Element(new GridToolbarCreateTagHelper(_translationServiceMock.Object)),
            new Element(new GridColumnTagHelper {
                Field = "Ratio", Editor = GridEditor.Numeric, Format = "{0:n8}", Decimals = 8, Required = true,
                Align = GridAlign.Right, MinScreenWidth = 500
            }),
            new Element(new GridColumnTagHelper {
                Field = "Area", Editor = GridEditor.Select,
                Options = new[] { new SelectListItem("Admin", "10"), new SelectListItem("Front", "20") }
            }),
            new Element(new GridColumnTagHelper { Field = "Id", Editable = false, Hidden = true, Encoded = false }),
            new Element(new GridCommandsTagHelper { Edit = true, Destroy = true, Width = 200, VisibleIf = "StoreId == ''" },
                null,
                new Element(new GridCommandTagHelper { Name = "primary", Text = "Mark", Click = "markAsPrimaryWeight" })));

        var config = Config(output);
        Assert.AreEqual("Inline", config.GetProperty("editMode").GetString());
        Assert.IsTrue(config.GetProperty("confirmDestroy").GetBoolean());
        Assert.IsFalse(config.GetProperty("reloadAfterSave").GetBoolean());
        Assert.IsFalse(config.GetProperty("autoBind").GetBoolean());
        Assert.AreEqual("[Admin.Common.AddNewRecord]", config.GetProperty("toolbar").GetProperty("create").GetString());

        var columns = config.GetProperty("columns");
        Assert.AreEqual("Numeric", columns[0].GetProperty("editor").GetString());
        Assert.AreEqual(8, columns[0].GetProperty("decimals").GetInt32());
        Assert.AreEqual("Right", columns[0].GetProperty("align").GetString());
        Assert.AreEqual(500, columns[0].GetProperty("minScreenWidth").GetInt32());
        Assert.IsTrue(columns[0].GetProperty("required").GetBoolean());
        Assert.AreEqual("20", columns[1].GetProperty("options")[1].GetProperty("value").GetString());
        Assert.AreEqual("Front", columns[1].GetProperty("options")[1].GetProperty("text").GetString());
        Assert.IsFalse(columns[2].GetProperty("editable").GetBoolean());
        Assert.IsTrue(columns[2].GetProperty("hidden").GetBoolean());
        Assert.IsFalse(columns[2].GetProperty("encoded").GetBoolean());

        var commands = config.GetProperty("commands");
        Assert.IsTrue(commands.GetProperty("edit").GetBoolean());
        Assert.IsTrue(commands.GetProperty("destroy").GetBoolean());
        Assert.AreEqual("StoreId == ''", commands.GetProperty("visibleIf").GetString());
        Assert.AreEqual("markAsPrimaryWeight", commands.GetProperty("custom")[0].GetProperty("click").GetString());
    }

    [TestMethod]
    public async Task Process_LocalizedTexts_ArePassedAsJsonAndGridTextsAreAdded()
    {
        var output = await RunGrid(CreateGrid(),
            new Element(new GridTextTagHelper { Name = "markAsPrimary", Value = "{{ Id }} <b>" }));

        var texts = Config(output).GetProperty("texts");
        Assert.AreEqual("[Admin.Common.Edit]", texts.GetProperty("edit").GetString());
        Assert.AreEqual("[Admin.Common.Grid.PageInfo]", texts.GetProperty("pageInfo").GetString());
        Assert.AreEqual("[Admin.Common.DeleteConfirmation]", texts.GetProperty("deleteConfirmation").GetString());
        Assert.AreEqual("{{ Id }} <b>", texts.GetProperty("markAsPrimary").GetString());
    }

    [TestMethod]
    public async Task Process_MissingResources_AreLeftOutInsteadOfShowingTheKey()
    {
        _translationServiceMock
            .Setup(x => x.GetResource("Admin.Common.Grid.PageInfo", "lang1", string.Empty, true))
            .Returns(string.Empty);

        var texts = Config(await RunGrid(CreateGrid())).GetProperty("texts");

        Assert.IsFalse(texts.TryGetProperty("pageInfo", out _));
        Assert.AreEqual("[Admin.Common.Grid.Refresh]", texts.GetProperty("refresh").GetString());
    }

    [TestMethod]
    public async Task Process_JsonAttribute_IsHtmlEncodedWhenRendered()
    {
        var output = await RunGrid(CreateGrid(),
            new Element(new GridColumnTagHelper { Field = "Name", Title = "'\"><script>alert(1)</script>" }));

        var html = Render(output);
        Assert.IsFalse(html.Contains("<script>"), html);
        Assert.IsFalse(html.Contains("'\">"), html);
        Assert.AreEqual("'\"><script>alert(1)</script>",
            Config(output).GetProperty("columns")[0].GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task Process_CellTemplates_AreRenderedAsInertTemplateElements()
    {
        var output = await RunGrid(CreateGrid(),
            new Element(new GridColumnTagHelper { Field = "Name" }, null,
                new Element(new GridCellTemplateTagHelper(), "<a href=\"Edit/{{ Id }}\">{{ Name }}</a>")),
            new Element(new GridColumnTagHelper { Field = "Published" }, null,
                new Element(new GridCellTemplateTagHelper(), "<i data-if=\"Published\" class=\"fa fa-check\"></i>")));

        var columns = Config(output).GetProperty("columns");
        Assert.AreEqual("weights-grid-template-1", columns[0].GetProperty("template").GetString());
        Assert.AreEqual("weights-grid-template-2", columns[1].GetProperty("template").GetString());
        var post = Render(output.PostElement);
        StringAssert.Contains(post, "<template id=\"weights-grid-template-1\"><a href=\"Edit/{{ Id }}\">{{ Name }}</a></template>");
        StringAssert.Contains(post, "<template id=\"weights-grid-template-2\"><i data-if=\"Published\" class=\"fa fa-check\"></i></template>");
    }

    [TestMethod]
    public async Task Process_Detail_SerializesNestedGridWithParams()
    {
        var output = await RunGrid(CreateGrid(),
            new Element(new GridColumnTagHelper { Field = "CustomerId" }),
            new Element(new GridDetailTagHelper(_urlHelperFactoryMock.Object) {
                    ViewContext = _viewContext, Controller = "ShoppingCart", ReadAction = "GetCartDetails",
                    Param = "customerId:CustomerId, storeId"
                }, null,
                new Element(new GridColumnTagHelper { Field = "ProductName" }, null,
                    new Element(new GridCellTemplateTagHelper(), "{{{ AttributeInfo }}}")),
                new Element(new GridColumnTagHelper { Field = "UpdatedOn", Format = "{0:G}" })));

        var config = Config(output);
        Assert.AreEqual(1, config.GetProperty("columns").GetArrayLength());
        var detail = config.GetProperty("detail");
        Assert.AreEqual("/Store/ShoppingCart/GetCartDetails", detail.GetProperty("transport").GetProperty("read").GetString());
        Assert.AreEqual("Compact", detail.GetProperty("pager").GetString());
        Assert.AreEqual("customerId", detail.GetProperty("params")[0].GetProperty("name").GetString());
        Assert.AreEqual("CustomerId", detail.GetProperty("params")[0].GetProperty("field").GetString());
        Assert.AreEqual("storeId", detail.GetProperty("params")[1].GetProperty("field").GetString());
        Assert.AreEqual(2, detail.GetProperty("columns").GetArrayLength());
        Assert.AreEqual("weights-grid-template-1", detail.GetProperty("columns")[0].GetProperty("template").GetString());
        StringAssert.Contains(Render(output.PostElement), "<template id=\"weights-grid-template-1\">{{{ AttributeInfo }}}</template>");
    }

    [TestMethod]
    public async Task Process_Detail_SerializesDestroyVisibilityAndDataBound()
    {
        var output = await RunGrid(CreateGrid(),
            new Element(new GridColumnTagHelper { Field = "ProductAttribute" }),
            new Element(new GridDetailTagHelper(_urlHelperFactoryMock.Object) {
                    ViewContext = _viewContext, ReadUrl = "/Admin/Product/ProductAttributeValueList",
                    DestroyUrl = "/Admin/Product/ProductAttributeValueDelete", Param = "productAttributeMappingId:Id",
                    ConfirmDestroy = true, VisibleIf = "AttributeControlTypeId != 4", OnDataBound = "bindValuePopups"
                }, null,
                new Element(new GridColumnTagHelper { Field = "Name" }),
                new Element(new GridCommandsTagHelper { Destroy = true })));

        var detail = Config(output).GetProperty("detail");
        Assert.AreEqual("/Admin/Product/ProductAttributeValueDelete", detail.GetProperty("transport").GetProperty("destroy").GetString());
        Assert.IsTrue(detail.GetProperty("confirmDestroy").GetBoolean());
        Assert.AreEqual("AttributeControlTypeId != 4", detail.GetProperty("visibleIf").GetString());
        Assert.AreEqual("bindValuePopups", detail.GetProperty("events").GetProperty("dataBound").GetString());
        Assert.IsTrue(detail.GetProperty("commands").GetProperty("destroy").GetBoolean());
    }

    [TestMethod]
    public async Task Process_BatchEditing_SerializesPrefixAndToolbarButtons()
    {
        var grid = CreateGrid(g =>
        {
            g.EditMode = GridEditMode.Batch;
            g.BatchPrefix = "products";
        });
        var output = await RunGrid(grid,
            new Element(new GridToolbarSaveTagHelper(_translationServiceMock.Object)),
            new Element(new GridToolbarCancelTagHelper(_translationServiceMock.Object) { Text = "Cancel changes" }),
            new Element(new GridColumnTagHelper { Field = "Price", Editor = GridEditor.Numeric, Decimals = 4 }));

        var config = Config(output);
        Assert.AreEqual("Batch", config.GetProperty("editMode").GetString());
        Assert.AreEqual("products", config.GetProperty("batchPrefix").GetString());
        var toolbar = config.GetProperty("toolbar");
        Assert.AreEqual("[Admin.Common.SaveChanges]", toolbar.GetProperty("save").GetString());
        Assert.AreEqual("Cancel changes", toolbar.GetProperty("cancel").GetString());
        Assert.IsFalse(toolbar.TryGetProperty("create", out _));
    }

    [TestMethod]
    public async Task Process_RowSelectionNamedCheckboxesAndRemoteFilter_AreSerialized()
    {
        var rowGrid = Config(await RunGrid(CreateGrid(g => g.Selectable = GridSelectable.Row)));
        Assert.AreEqual("Row", rowGrid.GetProperty("selectable").GetString());

        var checkboxGrid = Config(await RunGrid(CreateGrid(g =>
            {
                g.Selectable = GridSelectable.Checkbox;
                g.SelectionName = "SelectedProductIds";
            }),
            new Element(new GridColumnTagHelper {
                Field = "CategoryId", Editor = GridEditor.Select, OptionsUrl = "/Admin/Search/Category",
                OptionsFilter = "startswith", OptionLabel = "Select category...", TextField = "Category"
            })));
        Assert.AreEqual("SelectedProductIds", checkboxGrid.GetProperty("selectionName").GetString());
        var column = checkboxGrid.GetProperty("columns")[0];
        Assert.AreEqual("startswith", column.GetProperty("optionsFilter").GetString());
        Assert.AreEqual("/Admin/Search/Category", column.GetProperty("optionsUrl").GetString());
    }

    [TestMethod]
    public async Task Process_LocalData_KeepsRowPropertyNames()
    {
        var rows = new[] { new { AuthMethodName = "Google", Email = "a@b.c", ExternalIdentifier = "<x>" } };
        var config = Config(await RunGrid(CreateGrid(g =>
        {
            g.ReadUrl = null;
            g.Data = rows;
        })));

        var data = config.GetProperty("data");
        Assert.AreEqual(JsonValueKind.Array, data.ValueKind);
        Assert.AreEqual("Google", data[0].GetProperty("AuthMethodName").GetString());
        Assert.AreEqual("<x>", data[0].GetProperty("ExternalIdentifier").GetString());
        Assert.IsFalse(config.GetProperty("transport").TryGetProperty("read", out _));
    }

    [TestMethod]
    public async Task Process_Rtl_FollowsWorkingLanguageUnlessIgnoredForAdminArea()
    {
        _workingLanguage.Rtl = true;
        Assert.IsTrue(Config(await RunGrid(CreateGrid())).GetProperty("rtl").GetBoolean());

        _languageSettings.IgnoreRtlPropertyForAdminArea = true;
        Assert.IsFalse(Config(await RunGrid(CreateGrid())).TryGetProperty("rtl", out _));
    }

    [TestMethod]
    public async Task Process_Events_AreHandlerNames()
    {
        var output = await RunGrid(CreateGrid(g =>
        {
            g.OnDataBound = "onDataBound";
            g.AdditionalData = "additionalData";
            g.Selectable = GridSelectable.Checkbox;
        }));

        var config = Config(output);
        Assert.AreEqual("onDataBound", config.GetProperty("events").GetProperty("dataBound").GetString());
        Assert.AreEqual("additionalData", config.GetProperty("additionalData").GetString());
        Assert.AreEqual("Checkbox", config.GetProperty("selectable").GetString());
    }

    [TestMethod]
    public async Task Process_Culture_IsTheCurrentCulture()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
        var culture = Config(await RunGrid(CreateGrid())).GetProperty("culture");

        Assert.AreEqual("pl-PL", culture.GetProperty("name").GetString());
        Assert.AreEqual(",", culture.GetProperty("numberFormat").GetProperty("decimal").GetString());
        Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
            culture.GetProperty("calendar").GetProperty("shortDate").GetString());
    }

    [TestMethod]
    public void GridCulture_UsesGregorianPatternsForCulturesWithAnotherDefaultCalendar()
    {
        var culture = GridCulture.From(new CultureInfo("ar-SA"));

        var gregorian = (CultureInfo)new CultureInfo("ar-SA").Clone();
        var optional = gregorian.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (optional != null) gregorian.DateTimeFormat.Calendar = optional;
        Assert.AreEqual(gregorian.DateTimeFormat.ShortDatePattern, culture.Calendar.ShortDate);
        Assert.AreEqual(12, culture.Calendar.Months.Length);
    }

    [TestMethod]
    public void ParsePageSizes_IgnoresInvalidEntries()
    {
        CollectionAssert.AreEqual(new[] { 10, 20 }, AdminGridTagHelper.ParsePageSizes(" 10, abc, 20, -5, 10 ").ToArray());
        Assert.IsNull(AdminGridTagHelper.ParsePageSizes(""));
        Assert.IsNull(AdminGridTagHelper.ParsePageSizes("x"));
    }

    [TestMethod]
    public void ParseParams_SplitsNameAndField()
    {
        var result = GridDetailTagHelper.ParseParams("shipmentId:Id");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("shipmentId", result[0].Name);
        Assert.AreEqual("Id", result[0].Field);
        Assert.IsNull(GridDetailTagHelper.ParseParams(" "));
    }
}
