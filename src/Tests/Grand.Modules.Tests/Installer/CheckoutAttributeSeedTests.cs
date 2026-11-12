using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     The sample "Gift message" checkout attribute is shown only when "Gift wrapping" is "Yes".
///     Read from the installer source, like the other seed guards in this folder.
/// </summary>
[TestClass]
public class CheckoutAttributeSeedTests
{
    private static string Source() =>
        File.ReadAllText(Path.Combine(RepositoryPaths.InstallerServices, "InstallDataCheckoutAttributes.cs"));

    private static string VariableOf(string source, string type, string name)
    {
        var match = Regex.Match(source,
            @"var\s+(?<var>\w+)\s*=\s*new\s+" + type + @"\s*\{[^}]*Name\s*=\s*""" + Regex.Escape(name) + @"""",
            RegexOptions.Singleline);
        Assert.IsTrue(match.Success, $"{type} \"{name}\" not seeded in a variable in InstallDataCheckoutAttributes.cs");
        return match.Groups["var"].Value;
    }

    [TestMethod]
    public void InstallCheckoutAttributes_GiftMessageDependsOnGiftWrappingYes()
    {
        var source = Source();
        var giftWrapping = VariableOf(source, "CheckoutAttribute", "Gift wrapping");
        var giftMessage = VariableOf(source, "CheckoutAttribute", "Gift message");
        var yes = VariableOf(source, "CheckoutAttributeValue", "Yes");

        StringAssert.Matches(source,
            new Regex($@"\b{giftWrapping}\.CheckoutAttributeValues\.Add\(\s*{yes}\s*\)"),
            "the \"Yes\" value is not added to \"Gift wrapping\"");
        StringAssert.Matches(source,
            new Regex($@"\b{giftMessage}\.ConditionAttribute\.Add\(\s*new\s+CustomAttribute\s*\{{\s*" +
                      $@"Key\s*=\s*{giftWrapping}\.Id\s*,\s*Value\s*=\s*{yes}\.Id\s*\}}\s*\)"),
            "\"Gift message\" has no condition on \"Gift wrapping\" = \"Yes\"");
    }

    [TestMethod]
    public void InstallCheckoutAttributes_GiftMessageConditionIsSetBeforeInsert()
    {
        var source = Source();
        var giftMessage = VariableOf(source, "CheckoutAttribute", "Gift message");

        var condition = source.IndexOf($"{giftMessage}.ConditionAttribute.Add(", StringComparison.Ordinal);
        var insert = source.IndexOf($"InsertAsync({giftMessage})", StringComparison.Ordinal);

        Assert.IsTrue(condition >= 0 && insert > condition,
            "the condition must be added before \"Gift message\" is inserted, or it is never saved");
    }
}
