using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

[TestClass]
public class SampleNamesTests
{
    // Regexes that identify a quoted string as an entity's *name*, not just any quoted literal
    // (a meta title, a description, a picture query, etc. can also sit in the same seed file).
    // - Name\s*=\s*"..."                        the direct-assignment style (brands, vendors, tax
    //                                            categories, product attributes, ...).
    // - AddDepartment(\s*"..."                   the category seed helper's department name (1st
    //                                            argument).
    // - AddSubcategory(\s*\w+\s*,\s*"..."        the category seed helper's subcategory name (2nd
    //                                            argument, after the parent category identifier).
    private static readonly Regex[] SeededNamePatterns = {
        new(@"Name\s*=\s*""([^""]+)"""),
        new(@"AddDepartment\(\s*""([^""]+)"""),
        new(@"AddSubcategory\(\s*\w+\s*,\s*""([^""]+)""")
    };

    /// <summary>
    /// Returns the lookup names (from <paramref name="code"/>) that have no matching entity name
    /// in <paramref name="seeded"/>, where an entity name is only ever a direct <c>Name = "..."</c>
    /// assignment or a name argument to a category seed helper (see <see cref="SeededNamePatterns"/>)
    /// - never any other quoted literal (e.g. a meta title) that happens to equal the lookup name.
    /// </summary>
    internal static List<string> MissingNames(string helper, string code, string seeded)
    {
        // (?<![A-Za-z0-9_]) avoids e.g. "TaxCategoryId(" being matched as a "CategoryId(" call.
        var names = Regex.Matches(code, @"(?<![A-Za-z0-9_])" + helper + @"\(""([^""]+)""\)")
            .Select(m => m.Groups[1].Value).Distinct();

        var seededNames = SeededNamePatterns
            .SelectMany(p => p.Matches(seeded).Select(m => m.Groups[1].Value))
            .ToHashSet();

        return names.Where(n => !seededNames.Contains(n)).ToList();
    }

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
        var missing = MissingNames(helper, code, seeded);
        Assert.AreEqual(0, missing.Count, $"{helper}: no seeded entity named {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void MissingNames_RejectsANameThatOnlyAppearsAsAMetaTitle()
    {
        const string code = """CategoryId("Women's Clothing")""";
        const string seeded = """
            var category = new Category {
                Name = "Women",
                MetaTitle = "Women's Clothing",
            };
            """;

        var missing = MissingNames("CategoryId", code, seeded);

        CollectionAssert.Contains(missing, "Women's Clothing");
    }
}
