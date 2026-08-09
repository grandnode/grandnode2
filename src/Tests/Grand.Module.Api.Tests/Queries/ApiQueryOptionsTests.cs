using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Dynamic.Core;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Module.Api.Tests.Queries;

[TestClass]
public class ApiQueryOptionsTests
{
    private static readonly Type ElementType = typeof(ProductDto);

    [TestMethod]
    public void Filter_AcceptsAFieldOfTheModel()
    {
        ApiQueryOptions.ValidateFilter("Name.Contains(\"shirt\") and Published == true", ElementType);
    }

    [TestMethod]
    public void Filter_KeepsALiteralOutOfTheFieldCheck()
    {
        //the text is data, not a member name - a product called "Password" must stay searchable
        ApiQueryOptions.ValidateFilter("Name == \"Password\"", ElementType);
    }

    [DataTestMethod]
    [DataRow("PasswordHash != null", DisplayName = "field the model does not expose")]
    [DataRow("it.GetType().Assembly != null", DisplayName = "reflection through the context keyword")]
    [DataRow("\"\".GetType().Assembly.GetTypes().Length > 0", DisplayName = "type walk from a literal")]
    [DataRow("System.IO.File.ReadAllText(\"appsettings.json\") != null", DisplayName = "fully qualified type")]
    [DataRow("new(Name as X).X != null", DisplayName = "object construction")]
    [DataRow("Name.Equals(Name, StringComparison.Ordinal)", DisplayName = "type reference in an argument")]
    public void Filter_RejectsAnythingOutsideTheModel(string filter)
    {
        Assert.ThrowsExactly<ApiQueryOptionException>(() => ApiQueryOptions.ValidateFilter(filter, ElementType));
    }

    [TestMethod]
    public void Filter_RejectsAnOverlongExpression()
    {
        var filter = string.Join(" or ", Enumerable.Repeat("Name == \"x\"", 100));

        var ex = Assert.ThrowsExactly<ApiQueryOptionException>(
            () => ApiQueryOptions.ValidateFilter(filter, ElementType));

        StringAssert.Contains(ex.Message, "characters");
    }

    /// <summary>
    ///     The whitelist decides what runs; this checks the parser it runs on is locked down too, so a
    ///     future change to the whitelist cannot quietly re-open type access.
    /// </summary>
    [TestMethod]
    public void FilterConfig_RefusesToResolveTypes()
    {
        var source = new[] { new ProductDto { Name = "shirt" } }.AsQueryable();

        Assert.ThrowsExactly<System.Linq.Dynamic.Core.Exceptions.ParseException>(
            () => source.Where(ApiQueryOptions.FilterConfig, "it.GetType().Name == \"ProductDto\"").ToList());
    }

    [TestMethod]
    public void OrderBy_NormalizesDirectionAndDefaultsToAscending()
    {
        Assert.AreEqual("Name asc, Sku desc", ApiQueryOptions.ParseOrderBy("Name, Sku DESC", ElementType));
    }

    [DataTestMethod]
    [DataRow("PasswordHash")]
    [DataRow("Name sideways")]
    [DataRow("Name asc extra")]
    [DataRow("")]
    public void OrderBy_RejectsWhatItCannotVerify(string orderBy)
    {
        Assert.ThrowsExactly<ApiQueryOptionException>(() => ApiQueryOptions.ParseOrderBy(orderBy, ElementType));
    }

    [TestMethod]
    public void Select_BuildsTheProjectionFromCheckedFields()
    {
        Assert.AreEqual("new(Id, Name)", ApiQueryOptions.ParseSelect("Id, Name", ElementType));
    }

    [DataTestMethod]
    [DataRow("PasswordHash", DisplayName = "field the model does not expose")]
    [DataRow("Name as Alias", DisplayName = "expression rather than a field")]
    [DataRow("it.GetType()", DisplayName = "reflection")]
    public void Select_RejectsAnythingThatIsNotAPlainField(string select)
    {
        Assert.ThrowsExactly<ApiQueryOptionException>(() => ApiQueryOptions.ParseSelect(select, ElementType));
    }

    [TestMethod]
    public void Select_RejectsMoreFieldsThanTheLimit()
    {
        var select = string.Join(",", Enumerable.Range(0, ApiQueryOptions.MaxFields + 1).Select(_ => "Name"));

        Assert.ThrowsExactly<ApiQueryOptionException>(() => ApiQueryOptions.ParseSelect(select, ElementType));
    }
}
