using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     A reason comes from a plugin and a note from a person, and storefront content inside a Vue root is compiled as
///     a template: without v-pre "{{ }}" in such a text would run as an expression. Rendering a Razor view needs the
///     whole application, so the markup itself is checked.
/// </summary>
[TestClass]
public class OrderApprovalViewsTests
{
    [TestMethod]
    [DataRow("Order/Details.cshtml", "reason")]
    [DataRow("Order/Details.cshtml", "Model.OrderApprovalComment")]
    [DataRow("OrderApproval/Details.cshtml", "reason")]
    public void Views_RenderReasonsWithVPre(string view, string value)
    {
        var path = Path.Combine(RepositoryPaths.Root, "src", "Web", "Grand.Web", "Views", view);
        var markup = File.ReadAllText(path);

        var bound = Regex.Matches(markup, $@"<(\w+)([^>]*)>\s*@{Regex.Escape(value)}\s*</\1>");

        Assert.IsTrue(bound.Count > 0, $"{view} does not show {value}");
        foreach (Match match in bound)
            StringAssert.Contains(match.Groups[2].Value, "v-pre", $"{view}: {match.Value}");
    }
}
