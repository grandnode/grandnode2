using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     The texts of the customer's return pages: new installations get them from the default language file, existing
///     ones from the upgrade file
/// </summary>
[TestClass]
public class MerchandiseReturnStorefrontResourceKeysTests
{
    private static readonly string[] Added = [
        "Admin.Settings.Order.MerchandiseReturnReasons.PhotoRequired",
        "Admin.Settings.Order.MerchandiseReturnReasons.PhotoRequired.Hint",
        "MerchandiseReturns.Wizard.Items", "MerchandiseReturns.Wizard.Method", "MerchandiseReturns.Wizard.Summary",
        "MerchandiseReturns.Wizard.Next", "MerchandiseReturns.Wizard.Back",
        "MerchandiseReturns.Photos", "MerchandiseReturns.Photos.Hint", "MerchandiseReturns.Photos.Required",
        "MerchandiseReturns.Photos.Invalid",
        "MerchandiseReturns.Estimate.Title", "MerchandiseReturns.Estimate.ItemsAmount",
        "MerchandiseReturns.Estimate.ReturnShippingCost", "MerchandiseReturns.Estimate.Note",
        "MerchandiseReturns.Estimate.Destination",
        "MerchandiseReturns.Cancel", "MerchandiseReturns.Cancel.Confirm", "MerchandiseReturns.Cancel.Done",
        "MerchandiseReturns.Cancel.NotAllowed",
        "MerchandiseReturns.Notes.Add", "MerchandiseReturns.Notes.Message", "MerchandiseReturns.Notes.Attachment",
        "MerchandiseReturns.Notes.Send", "MerchandiseReturns.Notes.Added", "MerchandiseReturns.Notes.Closed",
        "MerchandiseReturns.Notes.Required", "MerchandiseReturns.Notes.TooLong", "MerchandiseReturns.Notes.From.You",
        "MerchandiseReturns.Notes.From.Store", "MerchandiseReturns.Notes.TooSoon", "MerchandiseReturns.Notes.TooMany",
        "MerchandiseReturns.Timeline", "MerchandiseReturns.Timeline.Submitted", "MerchandiseReturns.Timeline.Refunded",
        "MerchandiseReturns.Pdf",
        "PDFMerchandiseReturn.Title", "PDFMerchandiseReturn.Order", "PDFMerchandiseReturn.Date",
        "PDFMerchandiseReturn.ProductName", "PDFMerchandiseReturn.Quantity", "PDFMerchandiseReturn.Reason",
        "PDFMerchandiseReturn.Method", "PDFMerchandiseReturn.SendTo", "PDFMerchandiseReturn.DropOffPoint",
        "PDFMerchandiseReturn.ReturnCode", "PDFMerchandiseReturn.Company", "PDFMerchandiseReturn.Name",
        "PDFMerchandiseReturn.Phone", "PDFMerchandiseReturn.Address"
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

    [TestMethod]
    [DataRow("DefaultLanguage.xml")]
    [DataRow("Upgrade/en_250.xml")]
    public void EveryAddedKeyIsDefined(string file)
    {
        var defined = Defined(file);

        var missing = Added.Where(x => !defined.Contains(x)).ToList();

        Assert.AreEqual(0, missing.Count, $"{file} lacks: {string.Join(", ", missing)}");
    }
}
