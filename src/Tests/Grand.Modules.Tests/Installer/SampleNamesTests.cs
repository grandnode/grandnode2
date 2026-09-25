using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

[TestClass]
public class SampleNamesTests
{
    [TestMethod]
    [DataRow("CategoryId", "InstallDataCategories.cs")]
    [DataRow("BrandId", "InstallDataBrands.cs")]
    [DataRow("VendorId", "InstallDataVendors.cs")]
    [DataRow("TaxCategoryId", "InstallDataTaxCategories.cs")]
    [DataRow("ProductAttributeId", "InstallDataProductAttributes.cs")]
    [DataRow("ProductByName", "InstallDataProducts.*.cs")]
    public void EveryNameLookupHasASeededEntity(string helper, string seedGlob)
    {
        var code = string.Join("\n", Directory.GetFiles(RepositoryPaths.InstallerServices, "*.cs").Select(File.ReadAllText));
        var seeded = string.Join("\n", Directory.GetFiles(RepositoryPaths.InstallerServices, seedGlob).Select(File.ReadAllText));
        var names = Regex.Matches(code, helper + @"\(""([^""]+)""\)").Select(m => m.Groups[1].Value).Distinct();
        var missing = names.Where(n => !seeded.Contains($"Name = \"{n}\"")).ToList();
        Assert.AreEqual(0, missing.Count, $"{helper}: no seeded entity named {string.Join(", ", missing)}");
    }
}
