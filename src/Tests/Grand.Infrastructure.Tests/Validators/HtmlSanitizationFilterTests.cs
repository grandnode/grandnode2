using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
using Grand.Infrastructure.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Validators;

[TestClass]
public class HtmlSanitizationFilterTests
{
    private const string Payload = "<p>keep</p><img src=x onerror=alert(1)>";

    private HtmlSanitizationFilter _filter;

    [TestInitialize]
    public void Init()
    {
        _filter = new HtmlSanitizationFilter(new HtmlSanitizationService(new SecurityConfig()));
    }

    [TestMethod]
    public async Task RewritesRichTextProperty()
    {
        var model = new SanitizationSourceTest { FullDescription = Payload };

        await Execute(model, "POST");

        Assert.IsTrue(model.FullDescription.Contains("<p>keep</p>"), "legitimate markup was dropped");
        Assert.IsFalse(model.FullDescription.Contains("onerror"), "handler survived the filter");
    }

    [TestMethod]
    public async Task StripsMarkupFromPlainTextProperty()
    {
        var model = new SanitizationSourceTest { MetaTitle = "<b>Shoes</b><script>alert(1)</script>" };

        await Execute(model, "POST");

        Assert.AreEqual("Shoesalert(1)", model.MetaTitle);
    }

    [TestMethod]
    public async Task LeavesUnmarkedPropertyAlone()
    {
        var model = new SanitizationSourceTest { Name = Payload };

        await Execute(model, "POST");

        Assert.AreEqual(Payload, model.Name);
    }

    /// <summary>
    ///     Localized values hang off a Locales collection, so the marked properties sit one level down. Missing
    ///     them would leave every non-default language unprotected.
    /// </summary>
    [TestMethod]
    public async Task RewritesNestedCollectionItems()
    {
        var model = new SanitizationSourceTest {
            Locales = [new SanitizationLocalizedSourceTest { FullDescription = Payload }]
        };

        await Execute(model, "POST");

        Assert.IsFalse(model.Locales[0].FullDescription.Contains("onerror"),
            "localized value was not sanitized");
    }

    /// <summary>
    ///     The API binds the same models from a json body; this is the path the old validation attribute missed
    ///     entirely.
    /// </summary>
    [TestMethod]
    public async Task RewritesOnPutAsWellAsPost()
    {
        var model = new SanitizationSourceTest { FullDescription = Payload };

        await Execute(model, "PUT");

        Assert.IsFalse(model.FullDescription.Contains("onerror"));
    }

    [TestMethod]
    public async Task SkipsSafeMethods()
    {
        var model = new SanitizationSourceTest { FullDescription = Payload };

        await Execute(model, "GET");

        Assert.AreEqual(Payload, model.FullDescription, "a GET has no bound body to sanitize");
    }

    [TestMethod]
    public async Task ToleratesSelfReferencingGraph()
    {
        var model = new SanitizationSourceTest { FullDescription = Payload };
        model.Child = model;

        await Execute(model, "POST");

        Assert.IsFalse(model.FullDescription.Contains("onerror"));
    }

    private async Task Execute(object model, string method)
    {
        var httpContext = new DefaultHttpContext { Request = { Method = method } };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(),
            new ModelStateDictionary());
        var executingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
            new Dictionary<string, object> { { "model", model } }, null);

        await _filter.OnActionExecutionAsync(executingContext,
            () => Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)));
    }
}

public class SanitizationSourceTest
{
    public string Name { get; set; }

    [SanitizeHtml] public string FullDescription { get; set; }

    [NoHtml] public string MetaTitle { get; set; }

    public IList<SanitizationLocalizedSourceTest> Locales { get; set; } = [];

    public SanitizationSourceTest Child { get; set; }
}

public class SanitizationLocalizedSourceTest
{
    [SanitizeHtml] public string FullDescription { get; set; }
}
