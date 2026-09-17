using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.Models;
using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Tests.TagHelpers;

/// <summary>
///     The window every import screen opens. What matters is that the posted field name and
///     the action reach the partial unchanged - they are the server contract - and that the
///     element it renders carries none of Bootstrap's own modal class names, which are
///     `pointer-events: none` and made the file field unclickable.
/// </summary>
[TestClass]
public class AdminImportWindowTagHelperTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<IHtmlHelper> _htmlHelperMock;
    private ImportWindowModel _model;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _model = null;
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "lang1" });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        _translationServiceMock = new Mock<ITranslationService>();
        _htmlHelperMock = new Mock<IHtmlHelper>();
        _htmlHelperMock
            .Setup(x => x.PartialAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<ViewDataDictionary>()))
            .Callback((string _, object model, ViewDataDictionary _) => _model = model as ImportWindowModel)
            .ReturnsAsync(new HtmlString("<form></form>"));
    }

    private static ViewContext ViewContext(string area, string controller)
    {
        var routeData = new RouteData();
        routeData.Values["area"] = area;
        routeData.Values["controller"] = controller;
        return new ViewContext {
            HttpContext = new DefaultHttpContext(),
            RouteData = routeData
        };
    }

    private (TagHelperOutput output, string html) Run(Action<AdminImportWindowTagHelper> configure,
        string childContent = "")
    {
        var helper = new AdminImportWindowTagHelper(_htmlHelperMock.Object, _translationServiceMock.Object,
            _contextAccessorMock.Object) {
            ViewContext = ViewContext("Admin", "Brand")
        };
        configure(helper);

        var output = new TagHelperOutput("admin-import-window", [],
            (_, _) => {
                var content = new DefaultTagHelperContent();
                content.SetHtmlContent(childContent);
                return Task.FromResult<TagHelperContent>(content);
            });
        helper.ProcessAsync(new TagHelperContext([], new Dictionary<object, object>(), "id"), output)
            .GetAwaiter().GetResult();

        using var writer = new StringWriter();
        output.PreElement.WriteTo(writer, HtmlEncoder.Default);
        output.Content.WriteTo(writer, HtmlEncoder.Default);
        output.PostContent.WriteTo(writer, HtmlEncoder.Default);
        return (output, writer.ToString());
    }

    [TestMethod]
    public void Process_RendersAHiddenDivWithoutAnyBootstrapModalClass()
    {
        var (output, _) = Run(h => {
            h.WindowId = "importexcel-window";
            h.FileName = "importexcelfile";
            h.Action = "ImportFromXlsx";
        });

        Assert.AreEqual("div", output.TagName);
        Assert.AreEqual("importexcel-window", output.Attributes["id"].Value);
        Assert.AreEqual("display:none", output.Attributes["style"].Value);
        Assert.IsNull(output.Attributes["class"],
            "a Bootstrap modal class on this element is pointer-events: none and swallows every click in it");
    }

    [TestMethod]
    public void Process_PassesThePostedFieldNameAndTheActionThrough()
    {
        Run(h => {
            h.WindowId = "importexcel-window";
            h.FileName = "importexcelfile";
            h.FileLabel = "Excel file";
            h.Action = "ImportFromXlsx";
            h.Accept = ".xlsx";
            h.SubmitText = "Import";
        });

        Assert.IsNotNull(_model);
        Assert.AreEqual("importexcelfile", _model.FileName);
        Assert.AreEqual("importexcelfile", _model.FileId, "the element id defaults to the posted name");
        Assert.AreEqual("ImportFromXlsx", _model.ActionName);
        Assert.AreEqual("Brand", _model.ControllerName, "the current controller when none is named");
        Assert.AreEqual("Admin", _model.AreaName);
        Assert.AreEqual(".xlsx", _model.Accept);
        Assert.AreEqual("Import", _model.SubmitText);
    }

    [TestMethod]
    public void Process_KeepsAFileIdThatDiffersFromThePostedName()
    {
        Run(h => {
            h.FileName = "zippedFile";
            h.FileId = "importfiledialog";
            h.Action = "UploadPlugin";
        });

        Assert.AreEqual("zippedFile", _model.FileName);
        Assert.AreEqual("importfiledialog", _model.FileId);
        Assert.AreEqual("zippedFile-window", _model.WindowId, "the window id defaults to the posted name");
    }

    [TestMethod]
    public void Process_ShowsWhateverTheViewWritesInsideTheTagAboveTheField()
    {
        Run(h => {
            h.FileName = "importexcelfile";
            h.Action = "ImportFromXlsx";
        }, "<em>Tip</em>");

        using var writer = new StringWriter();
        _model.Notes.WriteTo(writer, HtmlEncoder.Default);
        Assert.AreEqual("<em>Tip</em>", writer.ToString());
    }

    [TestMethod]
    public void Process_OpensTheWindowFromTheButtonWithAJsonEncodedTitle()
    {
        var (_, html) = Run(h => {
            h.WindowId = "importexcel-window";
            h.ButtonId = "importexcel";
            h.Title = "Sklep's import";
            h.FileName = "importexcelfile";
            h.Action = "ImportFromXlsx";
        });

        StringAssert.Contains(html, "getElementById(\"importexcel\")");
        StringAssert.Contains(html, "GrandAdmin.modal.open(\"importexcel-window\"");
        StringAssert.Contains(html, "\"Sklep\\u0027s import\"",
            "an apostrophe in a translation must not end the string literal it is pasted into");
    }

    //A resource added in this release is not in the database of an installation that has
    //already upgraded, and the raw key on a label reads as a defect.
    [TestMethod]
    public void Process_FallsBackToTheLiteralLabelWhenTheResourceIsMissing()
    {
        _translationServiceMock
            .Setup(x => x.GetResource("Admin.Common.ZipFile", "lang1", string.Empty, true))
            .Returns(string.Empty);

        Run(h => {
            h.FileName = "zippedFile";
            h.FileLabelKey = "Admin.Common.ZipFile";
            h.FileLabel = "Zip file";
            h.Action = "UploadPlugin";
        });

        Assert.AreEqual("Zip file", _model.FileLabel);
    }

    [TestMethod]
    public void Process_PrefersTheResourceOverTheLiteralLabel()
    {
        _translationServiceMock
            .Setup(x => x.GetResource("Admin.Common.ZipFile", "lang1", string.Empty, true))
            .Returns("Plik zip");

        Run(h => {
            h.FileName = "zippedFile";
            h.FileLabelKey = "Admin.Common.ZipFile";
            h.FileLabel = "Zip file";
            h.Action = "UploadPlugin";
        });

        Assert.AreEqual("Plik zip", _model.FileLabel);
    }

    [TestMethod]
    public void Process_WritesNoScriptWithoutAButton()
    {
        var (_, html) = Run(h => {
            h.WindowId = "importexcel-window";
            h.FileName = "importexcelfile";
            h.Action = "ImportFromXlsx";
        });

        Assert.IsFalse(html.Contains("<script>"));
    }
}
