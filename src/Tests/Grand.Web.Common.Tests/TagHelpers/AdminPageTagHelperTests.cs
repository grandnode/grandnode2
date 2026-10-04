using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Tests.TagHelpers;

/// <summary>
///     The page shell that replaces the x_panel / x_title chrome. What matters is that the header
///     carries the title and that the actions of the child element land in it - a view putting a
///     button in &lt;page-actions&gt; must not see it rendered twice or lost.
/// </summary>
[TestClass]
public class AdminPageTagHelperTests
{
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

    private static async Task<string> Run(AdminPageTagHelper helper, string body = "", string actions = null)
    {
        var context = new TagHelperContext([], new Dictionary<object, object>(), "id");

        //the page collects the actions while its child content is evaluated
        var pageOutput = new TagHelperOutput("admin-page", [], async (_, _) =>
        {
            if (actions != null)
            {
                var child = new PageActionsTagHelper();
                var childOutput = Output("page-actions", actions);
                await child.ProcessAsync(context, childOutput);
            }

            var content = new DefaultTagHelperContent();
            content.AppendHtml(body);
            return content;
        });

        await helper.ProcessAsync(context, pageOutput);
        return Render(pageOutput);
    }

    [TestMethod]
    public async Task Process_RendersTheHeaderAndTheBody()
    {
        var html = await Run(new AdminPageTagHelper { Title = "Products", Icon = "bi-boxes" }, "<p>body</p>");

        StringAssert.Contains(html, "class=\"grand-page\"");
        StringAssert.Contains(html, "grand-page__header");
        StringAssert.Contains(html, "grand-page__title");
        StringAssert.Contains(html, "bi-boxes");
        StringAssert.Contains(html, "Products");
        StringAssert.Contains(html, "<p>body</p>");
    }

    [TestMethod]
    public async Task Process_PutsTheChildActionsInTheHeader()
    {
        var html = await Run(new AdminPageTagHelper { Title = "Products" }, "<p>body</p>",
            "<a class=\"btn btn-primary\">Add new</a>");

        var actionsAt = html.IndexOf("grand-page__actions", StringComparison.Ordinal);
        var bodyAt = html.IndexOf("<p>body</p>", StringComparison.Ordinal);
        Assert.IsTrue(actionsAt > 0, "the action cluster is rendered");
        Assert.IsTrue(actionsAt < bodyAt, "the actions belong to the header, above the body");
        Assert.AreEqual(1, html.Split("Add new").Length - 1, "the action is rendered exactly once");
    }

    [TestMethod]
    public async Task Process_RendersTheBackLinkAndTheSubtitle()
    {
        var html = await Run(new AdminPageTagHelper {
            Title = "Edit", Meta = "Sample product", BackUrl = "/Admin/Product/List", BackText = "Back to list"
        });

        StringAssert.Contains(html, "grand-page__meta");
        StringAssert.Contains(html, "Sample product");
        StringAssert.Contains(html, "grand-page__back");
        StringAssert.Contains(html, "/Admin/Product/List");
        StringAssert.Contains(html, "Back to list");
    }

    [TestMethod]
    public async Task Process_WithoutABackUrl_RendersNoBackLink()
    {
        var html = await Run(new AdminPageTagHelper { Title = "Products" });

        Assert.IsFalse(html.Contains("grand-page__back", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Process_Sticky_MarksTheShell()
    {
        var html = await Run(new AdminPageTagHelper { Title = "Edit", Sticky = true });

        StringAssert.Contains(html, "grand-page--sticky");
    }

    [TestMethod]
    public async Task ChildActions_OutsideAPage_AreDropped()
    {
        //no page collected them, so nothing is rendered rather than a stray cluster
        var child = new PageActionsTagHelper();
        var output = Output("page-actions", "<button>Save</button>");
        await child.ProcessAsync(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);

        Assert.AreEqual(string.Empty, Render(output));
    }
}
