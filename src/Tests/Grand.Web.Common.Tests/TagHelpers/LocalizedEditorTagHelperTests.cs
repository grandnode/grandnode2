using System.Text.Encodings.Web;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Web.Common.TagHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.TagHelpers;

[TestClass]
public class LocalizedEditorTagHelperTests
{
    private ViewContext _viewContext;

    [TestInitialize]
    public void Init()
    {
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById("en"))
            .ReturnsAsync(new Language { Id = "en", Name = "English", FlagImageFileName = "us.png" });
        languageService.Setup(x => x.GetLanguageById("pl"))
            .ReturnsAsync(new Language { Id = "pl", Name = "Polski", FlagImageFileName = "pl.png" });

        var services = new ServiceCollection();
        services.AddSingleton(languageService.Object);
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        _viewContext = new ViewContext {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
    }

    private async Task<string> Run(params string[] languageIds)
    {
        var helper = new LocalizedEditorTagHelper {
            Name = "brand-info-localized",
            LanguagesIds = languageIds.ToList(),
            ViewContext = _viewContext,
            LocalizedTemplate = i => new HelperResult(writer => writer.WriteAsync($"<div>locale {i}</div>"))
        };
        var context = new TagHelperContext("localized-editor", new TagHelperAttributeList(),
            new Dictionary<object, object>(), Guid.NewGuid().ToString());
        var output = new TagHelperOutput("localized-editor", new TagHelperAttributeList(), (_, _) =>
        {
            var inner = new DefaultTagHelperContent();
            inner.AppendHtml("<div>standard</div>");
            return Task.FromResult<TagHelperContent>(inner);
        });
        await helper.ProcessAsync(context, output);
        return Render(output.Content);
    }

    private static string Render(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [TestMethod]
    public async Task Process_SeveralLanguages_MarksTheStripAsALanguageStrip()
    {
        var html = await Run("en", "pl");

        //ui.css draws a language strip one step down from a page's tab strip by this class
        StringAssert.Contains(html, "class=\"grand-tabstrip grand-tabstrip-localized\"");
        StringAssert.Contains(html, "id=\"brand-info-localized\"");
        StringAssert.Contains(html, "data-grand-tabstrip=");
    }

    [TestMethod]
    public async Task Process_SeveralLanguages_RendersOneTabAndPanePerLanguageAfterStandard()
    {
        var html = await Run("en", "pl");

        StringAssert.Contains(html, "Standard");
        StringAssert.Contains(html, "English");
        StringAssert.Contains(html, "Polski");
        StringAssert.Contains(html, "<div>standard</div>");
        StringAssert.Contains(html, "<div>locale 0</div>");
        StringAssert.Contains(html, "<div>locale 1</div>");
        StringAssert.Contains(html, "class=\"k-image\"");
    }

    [TestMethod]
    public async Task Process_OneLanguage_RendersTheStandardFieldsWithoutAStrip()
    {
        var html = await Run("en");

        Assert.AreEqual("<div>standard</div>", html);
    }
}
