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
    // - NewDigitalProduct|NewComputer(\s*"...",\s*"..."
    //                                            the Digital and Computers product factories (name
    //                                            is the 2nd argument, after the SKU).
    private static readonly Regex[] SeededNamePatterns = {
        new(@"Name\s*=\s*""([^""]+)"""),
        new(@"AddDepartment\(\s*""([^""]+)"""),
        new(@"AddSubcategory\(\s*\w+\s*,\s*""([^""]+)"""),
        new(@"(?:NewDigitalProduct|NewComputer)\(\s*""[^""]*""\s*,\s*""([^""]+)""")
    };

    private static string ReadServices(string glob) =>
        string.Join("\n", Directory.GetFiles(RepositoryPaths.InstallerServices, glob).Select(File.ReadAllText));

    private static HashSet<string> SeededProductNames() =>
        SeededNamePatterns.SelectMany(p => p.Matches(ReadServices("InstallDataProducts.*.cs")).Select(m => m.Groups[1].Value))
            .ToHashSet();

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
    public void EverySpecPairNamesASeededOption()
    {
        // var saN = new SpecificationAttribute { Name = "..." ... } and var saNOptions = new[] { ... }
        var seed = ReadServices("InstallDataSpecificationAttributes.cs");
        var attributes = Regex.Matches(seed, @"var (sa\d+) = new SpecificationAttribute \{\s*Name = ""([^""]+)""")
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        var options = Regex.Matches(seed, @"var (sa\d+)Options = new\[\] \{([^}]*)\}")
            .ToDictionary(
                m => attributes[m.Groups[1].Value],
                m => Regex.Matches(m.Groups[2].Value, @"""([^""]+)""").Select(o => o.Groups[1].Value).ToHashSet());

        var productSeed = ReadServices("InstallDataProducts.*.cs");
        var pairs = Regex.Matches(productSeed, @"Spec\(""([^""]+)"",\s*""([^""]+)""");
        // every Spec( call must be the two-literal form, or a call written differently would go unchecked
        Assert.AreEqual(Regex.Matches(productSeed, @"(?<![A-Za-z0-9_])Spec\(").Count, pairs.Count,
            "a Spec(...) call does not use two string literals, so this test cannot check it");
        var missing = pairs
            .Select(m => (Attribute: m.Groups[1].Value, Option: m.Groups[2].Value))
            .Where(p => !options.TryGetValue(p.Attribute, out var set) || !set.Contains(p.Option))
            .Select(p => $"{p.Attribute}: {p.Option}")
            .Distinct()
            .ToList();
        Assert.AreEqual(0, missing.Count, "Spec pairs with no seeded option: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void EveryCollectionProductIsSeeded()
    {
        var seed = ReadServices("InstallDataCollections.cs");
        var lists = Regex.Matches(seed, @"AddProductsToCollection\(\w+,\s*\[(.*?)\]\)", RegexOptions.Singleline);
        // every call (the helper's own declaration excluded) must be the inline-array form this test reads
        Assert.AreEqual(Regex.Matches(seed, @"(?<![A-Za-z0-9_])AddProductsToCollection\(").Count - 1, lists.Count,
            "an AddProductsToCollection(...) call does not use an inline array, so this test cannot check it");
        var names = lists
            .SelectMany(m => Regex.Matches(m.Groups[1].Value, @"""([^""]+)""").Select(n => n.Groups[1].Value))
            .ToList();
        var seeded = SeededProductNames();
        var missing = names.Where(n => !seeded.Contains(n)).Distinct().ToList();
        Assert.AreEqual(0, missing.Count, "Collection products that are not seeded: " + string.Join(", ", missing));
    }

    [TestMethod]
    [DataRow("SampleProductTags")]
    [DataRow("SampleProductRelations")]
    [DataRow("SampleProductReviews")]
    public void EveryRelationTableNameIsSeeded(string fieldName)
    {
        var field = typeof(Grand.Module.Installer.Services.InstallationService)
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, $"{fieldName} not found on InstallationService");

        // every tuple's first item is a product name; string[] items after it (Related, CrossSells) are product
        // names too, while Tags' string[] and the review strings are not
        var names = new List<string>();
        foreach (System.Runtime.CompilerServices.ITuple row in (System.Collections.IEnumerable)field.GetValue(null)!)
        {
            names.Add((string)row[0]!);
            if (fieldName == "SampleProductRelations")
                for (var i = 1; i < row.Length; i++)
                    names.AddRange((string[])row[i]!);
        }

        Assert.IsTrue(names.Count > 0, $"{fieldName} is empty");
        var seeded = SeededProductNames();
        var missing = names.Where(n => !seeded.Contains(n)).Distinct().ToList();
        Assert.AreEqual(0, missing.Count, $"{fieldName}: products that are not seeded: {string.Join(", ", missing)}");
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
