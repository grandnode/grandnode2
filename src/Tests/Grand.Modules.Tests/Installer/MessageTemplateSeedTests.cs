using System.Text.Json;
using System.Text.RegularExpressions;
using DotLiquid;
using DotLiquid.NamingConventions;
using Grand.Module.Installer.Services.MessageTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

[TestClass]
public class MessageTemplateSeedTests
{
    private static readonly Regex Token = new(@"\{\{\s*([A-Za-z_][A-Za-z0-9_\.]*)", RegexOptions.Compiled);
    private static readonly Regex TagVar = new(@"\{%-?\s*(?:if|elsif|unless|for\s+\w+\s+in)\s+([A-Za-z_][A-Za-z0-9_\.]*)", RegexOptions.Compiled);

    internal static SortedSet<string> Tokens(string text) =>
        new(Token.Matches(text).Select(m => m.Groups[1].Value)
            .Concat(TagVar.Matches(text).Select(m => m.Groups[1].Value)));

    [TestMethod]
    public void Build_Returns51UniqueTemplates()
    {
        var templates = MessageTemplateSeed.Build("ea");
        Assert.AreEqual(51, templates.Count);
        Assert.AreEqual(51, templates.Select(t => t.Name).Distinct().Count());
        Assert.IsTrue(templates.All(t => t.EmailAccountId == "ea"));
    }

    [TestMethod]
    public void EveryTemplateParsesAsLiquid()
    {
        Template.NamingConvention = new CSharpNamingConvention();
        foreach (var t in MessageTemplateSeed.Build("ea"))
        {
            Template.Parse(t.Subject);
            Template.Parse(t.Body);
        }
    }

    [TestMethod]
    public void TokensAreSupersetOfBaseline()
    {
        var baseline = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Installer", "message-template-tokens.baseline.json")))!;
        foreach (var t in MessageTemplateSeed.Build("ea"))
        {
            var now = Tokens(t.Subject + t.Body);
            var lost = baseline[t.Name].Where(x => !now.Contains(x)).ToList();
            Assert.AreEqual(0, lost.Count, $"{t.Name} lost tokens: {string.Join(", ", lost)}");
        }
    }

    [TestMethod]
    public void IsActiveFlagsUnchanged()
    {
        var inactive = MessageTemplateSeed.Build("ea").Where(t => !t.IsActive).Select(t => t.Name).OrderBy(n => n).ToList();
        CollectionAssert.AreEqual(BaselineInactive, inactive);
    }

    [TestMethod]
    public void NoDoubleSlashAfterStoreUrl()
    {
        foreach (var t in MessageTemplateSeed.Build("ea"))
            Assert.IsFalse(t.Body.Contains("{{Store.URL}}/"), $"{t.Name} joins Store.URL with '/'");
    }

    private static readonly List<string> BaselineInactive = new()
    {
        "AuctionExpired.StoreOwnerNotification",
        "OrderCancelled.VendorNotification",
        "OrderPaid.CustomerNotification",
        "OrderPaid.StoreOwnerNotification",
        "OrderPaid.VendorNotification",
        "OrderPlaced.VendorNotification",
        "OrderRefunded.CustomerNotification",
        "OrderRefunded.StoreOwnerNotification",
        "VendorInformationChange.StoreOwnerNotification"
    };
}
