using System.Text.RegularExpressions;
using System.Xml.Linq;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     The texts of order approval: every key the code and views ask for exists in the default language file
///     and in the upgrade file (existing installations get their texts from the latter), and neither file keeps a
///     key of this feature that nothing uses. Keys are compared in lower case, like the files store them.
/// </summary>
[TestClass]
public class OrderApprovalResourceKeysTests
{
    //the keys of this feature: by prefix, as the files keep them (lower case)
    private static readonly Regex FeatureKey = new(
        @"^(checkout\.orderapproval\.|checkout\.orderplacement\.|orderapproval\.|account\.orderapprovals$" +
        @"|account\.subaccounts\.fields\.(orderapprovallimit|role)(\.|$)" +
        @"|admin\.customers\.customers\.subaccounts\.(orderapprovallimit|role)$" +
        @"|admin\.configuration\.payment\.settings\.orderapprovalpaymentmethods$" +
        @"|admin\.configuration\.stores\.fields\.ownercustomerid(\.invalid)?$" +
        @"|admin\.orders\.approval\." +
        @"|order\.approval\." +
        @"|admin\.orders\.fields\.orderstatus\.change\.(to)?awaitingapproval$" +
        @"|admin\.orders\.shipments\.orderawaitsapproval$" +
        @"|enums\.grand\.domain\.orders\.orderapprovalstatus\." +
        @"|enums\.grand\.domain\.customers\.subaccountrole\.)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    //string literals in code and views; quoted, so a liquid token inside a longer text does not match
    private static readonly Regex Literal = new(@"""([A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9]+)+)""", RegexOptions.Compiled);

    //message template names look like resource keys but are not
    private static readonly HashSet<string> TemplateNames =
        new(StringComparer.OrdinalIgnoreCase) { "OrderApproval.Requested", "OrderApproval.Decided", "OrderApproval.StaffRequested" };

    private static readonly string[] SkippedFolders = ["Tests", "bin", "obj", "node_modules", "App_Data", "wwwroot"];

    private static string Resources => Path.Combine(RepositoryPaths.Root, "src", "Web", "Grand.Web", "App_Data", "Resources");

    private static HashSet<string> Referenced()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in SourceFiles(Path.Combine(RepositoryPaths.Root, "src")))
            foreach (Match match in Literal.Matches(File.ReadAllText(file)))
            {
                var key = match.Groups[1].Value;
                if (FeatureKey.IsMatch(key) && !TemplateNames.Contains(key)) keys.Add(key);
            }

        //enum values are translated by name
        foreach (var name in Enum.GetNames<OrderApprovalStatus>())
            keys.Add($"Enums.Grand.Domain.Orders.OrderApprovalStatus.{name}");
        foreach (var name in Enum.GetNames<SubAccountRole>())
            keys.Add($"Enums.Grand.Domain.Customers.SubAccountRole.{name}");
        return keys;
    }

    private static IEnumerable<string> SourceFiles(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                     .Where(x => x.EndsWith(".cs") || x.EndsWith(".cshtml")))
            yield return file;
        foreach (var directory in Directory.EnumerateDirectories(folder))
        {
            if (SkippedFolders.Contains(Path.GetFileName(directory))) continue;
            foreach (var file in SourceFiles(directory)) yield return file;
        }
    }

    //XDocument.Load reads the UTF-16 default language file as well as the UTF-8 upgrade file
    private static HashSet<string> Defined(string file)
    {
        return XDocument.Load(file).Root!.Elements("Resource")
            .Select(x => x.Attribute("Name")!.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void TheCodeReferencesTheKeysThisTestLooksFor()
    {
        //guards the guard: a broken scan would make the two tests below pass without checking anything
        var referenced = Referenced();

        Assert.IsTrue(referenced.Contains("Checkout.OrderApproval.WillBeSentGeneric"));
        Assert.IsTrue(referenced.Contains("Checkout.OrderPlacement.RuleFailed"));
        Assert.IsTrue(referenced.Contains("OrderApproval.Order"));
        Assert.IsTrue(referenced.Contains("Admin.Configuration.Payment.Settings.OrderApprovalPaymentMethods"));
        Assert.IsTrue(referenced.Contains("Enums.Grand.Domain.Orders.OrderApprovalStatus.Withdrawn"));
        Assert.IsFalse(referenced.Contains("OrderApproval.Requested"));
    }

    [TestMethod]
    [DataRow("DefaultLanguage.xml")]
    [DataRow("Upgrade/en_250.xml")]
    public void EveryReferencedKeyIsDefined(string file)
    {
        var defined = Defined(Path.Combine(Resources, file));

        var missing = Referenced().Where(x => !defined.Contains(x)).OrderBy(x => x).ToList();

        Assert.AreEqual(0, missing.Count, $"{file} lacks: {string.Join(", ", missing)}");
    }

    [TestMethod]
    [DataRow("DefaultLanguage.xml")]
    [DataRow("Upgrade/en_250.xml")]
    public void NoKeyOfTheFeatureIsLeftUnused(string file)
    {
        var referenced = Referenced();

        var orphans = Defined(Path.Combine(Resources, file))
            .Where(x => FeatureKey.IsMatch(x) && !referenced.Contains(x)).OrderBy(x => x).ToList();

        Assert.AreEqual(0, orphans.Count, $"{file} keeps unused: {string.Join(", ", orphans)}");
    }

    [TestMethod]
    public void TheUpgradeFileHoldsTheSameTextsAsTheDefaultLanguage()
    {
        string Text(string file, string key)
        {
            return XDocument.Load(Path.Combine(Resources, file)).Root!.Elements("Resource")
                .Single(x => string.Equals(x.Attribute("Name")!.Value, key, StringComparison.OrdinalIgnoreCase))
                .Element("Value")!.Value;
        }

        foreach (var key in Referenced())
            Assert.AreEqual(Text("DefaultLanguage.xml", key), Text("Upgrade/en_250.xml", key), key);
    }
}
