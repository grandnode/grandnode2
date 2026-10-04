using System.ComponentModel.DataAnnotations;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Security;
using Grand.Infrastructure.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Validators;

/// <summary>
///     Exercises SanitizeHtmlAttribute/NoHtmlAttribute the way ASP.NET Core's model validation exercises any
///     ValidationAttribute: through ValidationContext.GetService, backed by a real service provider - not through
///     a live MVC pipeline (that path was covered manually against a running app earlier in this change).
/// </summary>
[TestClass]
public class SanitizeHtmlAttributeTests
{
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHtmlSanitizationService>(new HtmlSanitizationService(new SecurityConfig()));
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void SanitizeHtml_AcceptsCleanMarkup()
    {
        var model = new SanitizeHtmlSourceTest { FullDescription = "<p>Hello <strong>world</strong></p>" };

        var results = Validate(model);

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void SanitizeHtml_RejectsExecutableMarkup()
    {
        var model = new SanitizeHtmlSourceTest { FullDescription = "<img src=x onerror=alert(1)>" };

        var results = Validate(model);

        Assert.AreEqual(1, results.Count);
        StringAssert.Contains(results[0].MemberNames.FirstOrDefault() ?? "", "FullDescription");
    }

    [TestMethod]
    public void SanitizeHtml_AcceptsNullOrEmpty()
    {
        Assert.AreEqual(0, Validate(new SanitizeHtmlSourceTest { FullDescription = null }).Count);
        Assert.AreEqual(0, Validate(new SanitizeHtmlSourceTest { FullDescription = "" }).Count);
    }

    [TestMethod]
    public void NoHtml_AcceptsPlainText()
    {
        var model = new SanitizeHtmlSourceTest { MetaTitle = "Shoes & Boots" };

        Assert.AreEqual(0, Validate(model).Count);
    }

    [TestMethod]
    public void NoHtml_RejectsAnyMarkup()
    {
        var model = new SanitizeHtmlSourceTest { MetaTitle = "<b>Shoes</b>" };

        var results = Validate(model);

        Assert.AreEqual(1, results.Count);
        StringAssert.Contains(results[0].MemberNames.FirstOrDefault() ?? "", "MetaTitle");
    }

    [TestMethod]
    public void ThrowsWhenSanitizationServiceIsNotRegistered()
    {
        var emptyProvider = new ServiceCollection().BuildServiceProvider();
        var model = new SanitizeHtmlSourceTest { FullDescription = "<p>x</p>" };
        var context = new ValidationContext(model, emptyProvider, null) { MemberName = nameof(model.FullDescription) };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new SanitizeHtmlAttribute().GetValidationResult(model.FullDescription, context));
    }

    private List<ValidationResult> Validate(SanitizeHtmlSourceTest model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model,
            new ValidationContext(model, _serviceProvider, null), results, validateAllProperties: true);
        return results;
    }
}

public class SanitizeHtmlSourceTest
{
    [SanitizeHtml] public string FullDescription { get; set; }

    [NoHtml] public string MetaTitle { get; set; }
}
