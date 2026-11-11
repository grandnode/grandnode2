using System.Xml.Linq;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     The texts of the merchandise return workflow: new installations get them from the default language file,
///     existing ones from the upgrade file, and the texts it replaced are gone from both
/// </summary>
[TestClass]
public class MerchandiseReturnWorkflowResourceKeysTests
{
    private static readonly string[] PanelPrefixes = ["Admin", "Vendor"];

    private static readonly string[] Removed = [
        "Admin.Orders.MerchandiseReturns.NotifyCustomer", "Admin.Orders.MerchandiseReturns.NotifyCustomer.Hint",
        "Vendor.Orders.MerchandiseReturns.NotifyCustomer", "Vendor.Orders.MerchandiseReturns.NotifyCustomer.Hint",
        "MerchandiseReturns.MultiVendorsItems"
    ];

    private static string Resources =>
        Path.Combine(RepositoryPaths.Root, "src", "Web", "Grand.Web", "App_Data", "Resources");

    //XDocument.Load reads the UTF-16 default language file as well as the UTF-8 upgrade file
    private static HashSet<string> Defined(string file)
    {
        return XDocument.Load(Path.Combine(Resources, file)).Root!.Elements("Resource")
            .Select(x => x.Attribute("Name")!.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    //the keys this workflow added: every installation needs them
    private static IEnumerable<string> Added()
    {
        foreach (var prefix in PanelPrefixes)
        {
            //the status buttons are named after the status they change to
            foreach (var status in Enum.GetNames<MerchandiseReturnStatus>())
                yield return $"{prefix}.Orders.MerchandiseReturns.ChangeStatus.{status}";
            yield return $"{prefix}.Orders.MerchandiseReturns.ChangeStatusNote";
            yield return $"{prefix}.Orders.MerchandiseReturns.StatusChanged";
            yield return $"{prefix}.Orders.MerchandiseReturns.ConfirmCancel";
            yield return $"{prefix}.{MerchandiseReturnErrors.StatusChangeNotAllowed}";
            yield return $"{prefix}.{MerchandiseReturnErrors.Busy}";
            yield return $"{prefix}.{MerchandiseReturnErrors.NotFound}";
        }

        yield return MerchandiseReturnErrors.SubmitBusy;
        //Received is not added: every installation has had its name since the first release of the old statuses
        yield return "Enums.Grand.Domain.Orders.MerchandiseReturnStatus.Accepted";
        yield return "Enums.Grand.Domain.Orders.MerchandiseReturnStatus.Rejected";
        yield return "Enums.Grand.Domain.Orders.MerchandiseReturnStatus.Completed";
    }

    [TestMethod]
    [DataRow("DefaultLanguage.xml")]
    [DataRow("Upgrade/en_250.xml")]
    public void EveryAddedKeyIsDefined(string file)
    {
        var defined = Defined(file);

        var missing = Added().Where(x => !defined.Contains(x)).ToList();

        Assert.AreEqual(0, missing.Count, $"{file} lacks: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void EveryStatusHasAName()
    {
        //the panels and the storefront show the status by its translated name
        var defined = Defined("DefaultLanguage.xml");

        var missing = Enum.GetNames<MerchandiseReturnStatus>()
            .Select(x => $"Enums.Grand.Domain.Orders.MerchandiseReturnStatus.{x}")
            .Where(x => !defined.Contains(x)).ToList();

        Assert.AreEqual(0, missing.Count, $"lacks: {string.Join(", ", missing)}");
    }

    [TestMethod]
    [DataRow("DefaultLanguage.xml")]
    [DataRow("Upgrade/en_250.xml")]
    public void NoRemovedKeyIsLeft(string file)
    {
        var defined = Defined(file);

        var left = Removed.Where(defined.Contains).ToList();

        Assert.AreEqual(0, left.Count, $"{file} keeps: {string.Join(", ", left)}");
    }
}
