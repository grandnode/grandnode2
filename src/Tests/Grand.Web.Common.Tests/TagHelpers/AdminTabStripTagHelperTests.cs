using System.Text.Encodings.Web;
using System.Text.Json;
using Grand.Mediator;
using Grand.Web.Common.Events;
using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Wangkanai.Detection.Models;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.Tests.TagHelpers;

[TestClass]
public class AdminTabStripTagHelperTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IDetectionService> _detectionServiceMock;
    private Mock<IDeviceService> _deviceServiceMock;
    private ViewContext _viewContext;
    private Action<AdminTabStripCreated> _onPublish;

    [TestInitialize]
    public void Init()
    {
        _onPublish = null;
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Publish(It.IsAny<AdminTabStripCreated>(), It.IsAny<CancellationToken>()))
            .Returns((AdminTabStripCreated message, CancellationToken _) =>
            {
                _onPublish?.Invoke(message);
                return Task.CompletedTask;
            });
        _deviceServiceMock = new Mock<IDeviceService>();
        _deviceServiceMock.Setup(x => x.Type).Returns(Device.Desktop);
        _detectionServiceMock = new Mock<IDetectionService>();
        _detectionServiceMock.Setup(x => x.Device).Returns(_deviceServiceMock.Object);
        _viewContext = new ViewContext {
            RouteData = new RouteData(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private AdminTabStripTagHelper CreateStrip(string name = "product-edit", bool bindGrid = true,
        bool setTabPos = false)
    {
        return new AdminTabStripTagHelper(_mediatorMock.Object, _detectionServiceMock.Object) {
            Name = name,
            BindGrid = bindGrid,
            SetTabPos = setTabPos,
            ViewContext = _viewContext
        };
    }

    /// <summary>Runs the strip with the tab contents its child content tag helpers collect.</summary>
    private async Task<TagHelperOutput> Run(AdminTabStripTagHelper strip, params string[] tabContents)
    {
        var context = new TagHelperContext("admin-tabstrip", new TagHelperAttributeList(),
            new Dictionary<object, object>(), Guid.NewGuid().ToString());
        var output = new TagHelperOutput("admin-tabstrip", new TagHelperAttributeList(), async (_, _) =>
        {
            foreach (var tab in tabContents)
            {
                var content = new AdminTabContentTagHelper { ViewContext = _viewContext };
                var childContext = new TagHelperContext("content", new TagHelperAttributeList(),
                    new Dictionary<object, object>(), Guid.NewGuid().ToString());
                var childOutput = new TagHelperOutput("content", new TagHelperAttributeList(), (_, _) =>
                {
                    var inner = new DefaultTagHelperContent();
                    inner.AppendHtml(tab);
                    return Task.FromResult<TagHelperContent>(inner);
                });
                await content.ProcessAsync(childContext, childOutput);
            }

            return new DefaultTagHelperContent();
        });
        await strip.ProcessAsync(context, output);
        return output;
    }

    private static string Render(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [TestMethod]
    public async Task Process_RendersBootstrapTabMarkupWithoutAnyScript()
    {
        var output = await Run(CreateStrip(), "<div>info</div>", "<div>prices</div>");

        Assert.AreEqual("div", output.TagName);
        Assert.AreEqual("product-edit", output.Attributes["id"].Value);
        Assert.AreEqual("grand-tabstrip", output.Attributes["class"].Value);
        var panes = Render(output.PostContent);
        StringAssert.Contains(panes, "<div class=\"tab-content\">");
        StringAssert.Contains(panes, "<div>info</div>");
        StringAssert.Contains(panes, "<div>prices</div>");
        Assert.IsFalse(Render(output.PreElement).Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(panes.Contains("kendo", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Process_SerializesBindGridAndSelectedTabIndex()
    {
        _viewContext.ViewData["Grand.selected-tab-index"] = 1;
        var output = await Run(CreateStrip(), "<div>a</div>", "<div>b</div>", "<div>c</div>");

        var config = JsonDocument.Parse(output.Attributes["data-grand-tabstrip"].Value.ToString()!).RootElement;
        Assert.IsTrue(config.GetProperty("bindGrid").GetBoolean());
        Assert.AreEqual(1, config.GetProperty("selectedIndex").GetInt32());
        StringAssert.Contains(Render(output.PreElement), "name=\"selected-tab-index\"");
        StringAssert.Contains(Render(output.PreElement), "value=\"1\"");
    }

    [TestMethod]
    public async Task Process_MarksTheSelectedPaneActiveAndKeepsTheKendoStateClass()
    {
        _viewContext.TempData["Grand.selected-tab-index"] = 1;
        var output = await Run(CreateStrip(), "<div>a</div>", "<div>b</div>");

        var panes = Render(output.PostContent);
        StringAssert.Contains(panes, "class=\"tab-pane active show k-state-active\"");
        Assert.AreEqual(1, panes.Split("k-state-active").Length - 1);
    }

    [TestMethod]
    public async Task Process_SelectedIndexPastTheLastTab_ActivatesTheFirstPane()
    {
        //a nested strip numbers its tabs outside the page range (Product/ProductAttributes)
        _viewContext.ViewData["Grand.selected-tab-index"] = 8;
        var output = await Run(CreateStrip(), "<div>a</div>", "<div>b</div>");

        var config = JsonDocument.Parse(output.Attributes["data-grand-tabstrip"].Value.ToString()!).RootElement;
        Assert.AreEqual(0, config.GetProperty("selectedIndex").GetInt32());
        //the hidden input still posts what the page selected
        StringAssert.Contains(Render(output.PreElement), "value=\"8\"");
    }

    [TestMethod]
    public async Task Process_NegativeSelectedIndex_FallsBackToZero()
    {
        _viewContext.ViewData["Grand.selected-tab-index"] = -3;
        var output = await Run(CreateStrip(), "<div>a</div>");

        StringAssert.Contains(Render(output.PreElement), "value=\"0\"");
    }

    [TestMethod]
    public async Task Process_SetTabPos_AddsTheLeftLayoutClass()
    {
        var output = await Run(CreateStrip(setTabPos: true), "<div>a</div>");

        Assert.AreEqual("grand-tabstrip grand-tabstrip-left", output.Attributes["class"].Value);
    }

    [TestMethod]
    public async Task Process_OnAMobileDevice_IgnoresSetTabPos()
    {
        _deviceServiceMock.Setup(x => x.Type).Returns(Device.Mobile);
        var output = await Run(CreateStrip(setTabPos: true), "<div>a</div>");

        Assert.AreEqual("grand-tabstrip", output.Attributes["class"].Value);
    }

    [TestMethod]
    public async Task Process_PluginBlocks_AreRenderedAsMarkupNotAsAScriptLiteral()
    {
        //the title used to be pasted into a JavaScript string literal: an apostrophe broke
        //the script and a quote could close it and run whatever followed
        _onPublish = message => message.BlocksToRender.Add((
            "Vendor's tab \"x\"</script><script>alert(1)</script>",
            new HtmlString("<div id=\"plugin-tab\">body</div>")));

        var output = await Run(CreateStrip(), "<div>a</div>");

        var appended = Render(output.PostElement);
        StringAssert.Contains(appended, "<template data-grand-tab-append=\"product-edit\"");
        //the title is an attribute value and is encoded, so no tag of it survives as markup
        StringAssert.Contains(appended, "data-tab-name=\"Vendor&#x27;s tab &quot;x&quot;&lt;/script&gt;&lt;script&gt;alert(1)&lt;/script&gt;\"");
        //the body is markup, as it was meant to be
        StringAssert.Contains(appended, "<div id=\"plugin-tab\">body</div>");
        Assert.IsFalse(appended.Contains("<script>alert(1)</script>", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Process_WithoutPluginBlocks_AppendsNoTemplate()
    {
        var output = await Run(CreateStrip(), "<div>a</div>");

        Assert.AreEqual(string.Empty, Render(output.PostElement));
    }
}
