using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.TagHelpers;

/// <summary>
///     The classes the admin editors render. Bootstrap 5 styles a select with form-select -
///     form-control resets its appearance and takes the native arrow with it - and the Kendo
///     k-input marker that sat next to form-control is gone with Kendo.
/// </summary>
[TestClass]
public class AdminControlClassTests
{
    private static TagHelperContext Context(params TagHelperAttribute[] attributes)
    {
        return new TagHelperContext(new TagHelperAttributeList(attributes),
            new Dictionary<object, object>(), "test");
    }

    private static TagHelperOutput Output(string tagName, params TagHelperAttribute[] attributes)
    {
        return new TagHelperOutput(tagName, new TagHelperAttributeList(attributes),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    [TestMethod]
    public async Task AdminSelect_RendersFormSelect()
    {
        var helper = new AdminSelectTagHelper(Mock.Of<IHtmlGenerator>()) {
            ViewContext = new ViewContext()
        };
        var output = Output("admin-select");

        await helper.ProcessAsync(Context(), output);

        var classes = output.Attributes["class"].Value.ToString();
        Assert.IsTrue(classes!.Contains("form-select"), classes);
        Assert.IsFalse(classes.Contains("form-control"), "a Bootstrap 5 select is not a form-control");
        Assert.IsFalse(classes.Contains("k-input"), "Kendo is gone");
        Assert.AreEqual("select", output.TagName);
    }

    [TestMethod]
    public async Task AdminSelect_KeepsTheClassTheViewAsksFor()
    {
        var helper = new AdminSelectTagHelper(Mock.Of<IHtmlGenerator>()) {
            ViewContext = new ViewContext()
        };
        var output = Output("admin-select");

        await helper.ProcessAsync(Context(new TagHelperAttribute("class", "store-select")), output);

        var classes = output.Attributes["class"].Value.ToString();
        Assert.IsTrue(classes!.Contains("form-select"), classes);
        Assert.IsTrue(classes.Contains("store-select"), classes);
    }

    private static AdminTextAreaTagHelper TextArea()
    {
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForType(typeof(string));
        var viewData = new ViewDataDictionary(provider, new ModelStateDictionary());
        return new AdminTextAreaTagHelper(Mock.Of<IHtmlGenerator>()) {
            For = new ModelExpression("Description", new ModelExplorer(provider, metadata, null)),
            ViewContext = new ViewContext { ViewData = viewData }
        };
    }

    [TestMethod]
    public void AdminTextArea_RendersFormControlWithoutTheKendoMarker()
    {
        var output = Output("admin-textarea");

        TextArea().Process(Context(), output);

        Assert.AreEqual("form-control", output.Attributes["class"].Value.ToString());
        Assert.AreEqual("textarea", output.TagName);
    }

    [TestMethod]
    public void AdminTextArea_KeepsTheClassTheViewAsksFor()
    {
        var output = Output("admin-textarea", new TagHelperAttribute("class", "html-editor"));

        TextArea().Process(Context(), output);

        Assert.AreEqual("html-editor", output.Attributes["class"].Value.ToString());
    }
}
