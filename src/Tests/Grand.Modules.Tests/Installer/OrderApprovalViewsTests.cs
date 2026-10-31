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

    /// <summary>
    ///     Two overlapping posts of one decision would both pass the status check and move the order twice
    /// </summary>
    [TestMethod]
    [DataRow("Web/Grand.Web.AdminShared/Views/AdminShared/Order/DecideApprovalPopup.cshtml", "DecideApproval")]
    [DataRow("Web/Grand.Web/Views/Order/Details.cshtml", "WithdrawOrderApproval")]
    public void DecisionForms_SubmitOnce(string view, string target)
    {
        var markup = File.ReadAllText(Path.Combine(RepositoryPaths.Root, "src", view));

        var form = Regex.Match(markup, $@"<form[^>]*""{target}""[^>]*>");

        Assert.IsTrue(form.Success, $"{view} has no form for {target}");
        StringAssert.Contains(form.Value, "onsubmit=\"@submitOnce\"", view);
    }
}
