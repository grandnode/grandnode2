using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tax.CountryStateZip;
using Tax.CountryStateZip.Infrastructure.Cache;
using Tax.CountryStateZip.Services;

namespace Grand.Modules.Tests.Plugins.Tax;

[TestClass]
public class CountryStateZipTaxProviderTests
{
    private readonly CountryStateZipTaxProvider _provider;

    public CountryStateZipTaxProviderTests()
    {
        var cache = new Mock<ICacheBase>();
        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<TaxRateForCaching>>>>()))
            .ReturnsAsync([new TaxRateForCaching { CountryId = "pl", TaxCategoryId = "books", Percentage = 5 }]);
        _provider = new CountryStateZipTaxProvider(new Mock<IRequestTranslationService>().Object, cache.Object,
            new Mock<ITaxRateService>().Object, new CountryStateZipTaxSettings());
    }

    [TestMethod]
    public async Task NoAddress_NoTaxAndNoError()
    {
        //a guest who has not given an address, with no default tax address: nothing to report on every price
        var result = await _provider.GetTaxRate(new TaxRequest { TaxCategoryId = "books" });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.TaxRate);
    }

    [TestMethod]
    public async Task AddressOfARateCountry_TheRate()
    {
        var result = await _provider.GetTaxRate(new TaxRequest {
            TaxCategoryId = "books", Address = new Address { CountryId = "pl" }
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.TaxRate);
    }
}
