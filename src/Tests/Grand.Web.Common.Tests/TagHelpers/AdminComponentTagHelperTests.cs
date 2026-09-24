using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Tests.TagHelpers;

/// <summary>
///     The card, the filter bar, the bulk bar and the overflow menu: the components a converted
///     screen is assembled from. Their chrome texts come from the <c>Admin.Common.*</c> resources
///     the grid already reads, and - as with the grid - a key the database does not carry falls
///     back to English rather than showing a raw resource key.
/// </summary>
[TestClass]
public class AdminComponentTagHelperTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private Dictionary<string, string> _resources;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _resources = new Dictionary<string, string>();
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "lang1" });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock
            .Setup(x => x.GetResource(It.IsAny<string>(), "lang1", string.Empty, true))
            .Returns((string key, string _, string _, bool _) =>
                _resources.TryGetValue(key, out var value) ? value : string.Empty);
    }

    private static TagHelperOutput Output(string tagName, string childContent = "")
    {
        return new TagHelperOutput(tagName, [],
            (_, _) =>
            {
                var content = new DefaultTagHelperContent();
                content.AppendHtml(childContent);
                return Task.FromResult<TagHelperContent>(content);
            });
    }

    private static string Render(TagHelperOutput output)
    {
        using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private static TagHelperContext Context(IDictionary<object, object> items = null)
    {
        return new TagHelperContext([], items ?? new Dictionary<object, object>(), "id");
    }

    [TestMethod]
    public async Task Card_RendersATitledSection()
    {
        var helper = new AdminCardTagHelper { Title = "Products shipped", Icon = "bi-box-seam" };
        var output = Output("admin-card", "<table></table>");

        await helper.ProcessAsync(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "grand-card__header");
        StringAssert.Contains(html, "grand-card__title");
        StringAssert.Contains(html, "bi-box-seam");
        StringAssert.Contains(html, "Products shipped");
        StringAssert.Contains(html, "grand-card__body");
        StringAssert.Contains(html, "<table></table>");
    }

    [TestMethod]
    public async Task Card_WithoutATitle_RendersNoHeader()
    {
        var output = Output("admin-card", "<div id=\"grid\"></div>");

        await new AdminCardTagHelper { Flush = true }.ProcessAsync(Context(), output);
        var html = Render(output);

        Assert.IsFalse(html.Contains("grand-card__header", StringComparison.Ordinal));
        StringAssert.Contains(html, "grand-card__body--flush");
    }

    [TestMethod]
    public async Task Filters_RendersTheQuickRowAndTheCollapsedPanel()
    {
        _resources["Admin.Common.Search"] = "Search";
        _resources["Admin.Common.Filters"] = "Filters";
        var helper = new AdminFiltersTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object) {
            Id = "product-filters", SubmitId = "search-products"
        };
        var items = new Dictionary<object, object>();
        var context = Context(items);
        var output = new TagHelperOutput("admin-filters", [], async (_, _) =>
        {
            var search = new FiltersSearchTagHelper();
            await search.ProcessAsync(context, Output("filters-search", "<input id=\"SearchProductName\"/>"));
            var advanced = new DefaultTagHelperContent();
            advanced.AppendHtml("<div class=\"grand-field\"></div>");
            return advanced;
        });

        await helper.ProcessAsync(context, output);
        var html = Render(output);

        StringAssert.Contains(html, "grand-filters__quick");
        StringAssert.Contains(html, "SearchProductName");
        StringAssert.Contains(html, "id=\"search-products\"");
        StringAssert.Contains(html, "Search");
        StringAssert.Contains(html, "Filters");
        //the advanced criteria are there but collapsed, and laid out in two columns
        StringAssert.Contains(html, "id=\"product-filters-advanced\"");
        StringAssert.Contains(html, "collapse grand-filters__advanced");
        StringAssert.Contains(html, "grand-fields--2col");
    }

    [TestMethod]
    public async Task Filters_WithNoAdvancedFields_RendersNoToggle()
    {
        var helper = new AdminFiltersTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object);
        var output = Output("admin-filters");

        await helper.ProcessAsync(Context(), output);
        var html = Render(output);

        Assert.IsFalse(html.Contains("data-bs-toggle", StringComparison.Ordinal));
        Assert.IsFalse(html.Contains("grand-filters__advanced", StringComparison.Ordinal));
        //a missing resource shows English, never the key
        StringAssert.Contains(html, "Search");
        Assert.IsFalse(html.Contains("Admin.Common.Search", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Filters_SearchIsAnOutlineButtonUnlessAskedToBePrimary()
    {
        //Add new is a list's one primary, so Search is an outline button by default
        var secondary = Output("admin-filters");
        await new AdminFiltersTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object)
            .ProcessAsync(Context(), secondary);
        var html = Render(secondary);
        StringAssert.Contains(html, "btn btn-outline-secondary");
        Assert.IsFalse(html.Contains("btn-primary", StringComparison.Ordinal));

        var primary = Output("admin-filters");
        await new AdminFiltersTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object) {
            SecondarySubmit = false
        }.ProcessAsync(Context(), primary);
        StringAssert.Contains(Render(primary), "btn btn-primary");
    }

    [TestMethod]
    public async Task BulkBar_CarriesTheGridAndTheCountTemplate()
    {
        _resources["Admin.Common.Grid.SelectedCount"] = "{0} selected";
        var helper = new AdminBulkBarTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object) {
            GridId = "products-grid"
        };
        var output = Output("admin-bulkbar", "<button>Delete</button>");

        await helper.ProcessAsync(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "data-grand-bulkbar=\"products-grid\"");
        StringAssert.Contains(html, "grand-bulkbar__count");
        StringAssert.Contains(html, "{0} selected");
        StringAssert.Contains(html, "<button>Delete</button>");
    }

    [TestMethod]
    public void Action_WithAUrl_IsALink()
    {
        var output = Output("admin-action");

        new AdminActionTagHelper { Text = "Export", Icon = "bi-download", Url = "/Admin/Product/Export" }
            .Process(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "<a ");
        StringAssert.Contains(html, "href=\"/Admin/Product/Export\"");
        StringAssert.Contains(html, "dropdown-item");
        StringAssert.Contains(html, "bi-download");
        StringAssert.Contains(html, "Export");
    }

    [TestMethod]
    public void Action_Submitting_PostsTheFormToItsOwnUrl()
    {
        var output = Output("admin-action");

        new AdminActionTagHelper {
            Text = "Export selected", Url = "/Admin/Product/ExportSelected", Submit = true, Name = "exportexcel-selected"
        }.Process(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "<button ");
        StringAssert.Contains(html, "type=\"submit\"");
        StringAssert.Contains(html, "formaction=\"/Admin/Product/ExportSelected\"");
        StringAssert.Contains(html, "name=\"exportexcel-selected\"");
    }

    [TestMethod]
    public void Action_WithoutAUrl_IsAPlainButtonAScriptCanFind()
    {
        var output = Output("admin-action");

        new AdminActionTagHelper { Text = "Import", Id = "importexcel", Destructive = false }.Process(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "type=\"button\"");
        StringAssert.Contains(html, "id=\"importexcel\"");
        Assert.IsFalse(html.Contains("formaction", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Action_Destructive_IsMarkedAsSuch()
    {
        var output = Output("admin-action");

        new AdminActionTagHelper { Text = "Delete", Destructive = true, Id = "product-delete" }
            .Process(Context(), output);

        StringAssert.Contains(Render(output), "text-danger");
    }

    [TestMethod]
    public async Task ActionMenu_RendersADropdownOfItsEntries()
    {
        _resources["Admin.Common.Actions"] = "Actions";
        var helper = new AdminActionMenuTagHelper(_contextAccessorMock.Object, _translationServiceMock.Object);
        var output = Output("admin-action-menu", "<button class=\"dropdown-item\">Copy</button>");

        await helper.ProcessAsync(Context(), output);
        var html = Render(output);

        StringAssert.Contains(html, "dropdown");
        StringAssert.Contains(html, "data-bs-toggle=\"dropdown\"");
        StringAssert.Contains(html, "aria-label=\"Actions\"");
        StringAssert.Contains(html, "dropdown-menu");
        //the component style keys off its own class, not the editor's shared .dropdown-menu
        StringAssert.Contains(html, "grand-action-menu");
        StringAssert.Contains(html, "Copy");
    }

    [TestMethod]
    public async Task Popup_CarriesThePageScopeAndATitle()
    {
        var helper = new AdminPopupTagHelper { Title = "Picture details", Icon = "bi-image" };
        var output = Output("admin-popup", "<div class=\"grand-fields\"></div>");

        await helper.ProcessAsync(Context(), output);
        var html = Render(output);

        //grand-page is what gives a popup the buttons, fields and grids of a converted page
        StringAssert.Contains(html, "grand-page grand-popup");
        StringAssert.Contains(html, "grand-popup__title");
        StringAssert.Contains(html, "bi-image");
        StringAssert.Contains(html, "Picture details");
        StringAssert.Contains(html, "<div class=\"grand-fields\"></div>");
    }

    [TestMethod]
    public async Task Popup_WithoutATitle_RendersNoHeading()
    {
        var output = Output("admin-popup", "<p>body</p>");

        await new AdminPopupTagHelper().ProcessAsync(Context(), output);

        Assert.IsFalse(Render(output).Contains("grand-popup__title", StringComparison.Ordinal));
    }
}
