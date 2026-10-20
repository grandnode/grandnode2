using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data.Mongo;
using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetSearchProductsQueryHandlerTextSearchTests
{
    private CapturingRepository _repository;
    private MongoDBContext _databaseContext;

    [TestInitialize]
    public async Task Init()
    {
        _repository = new CapturingRepository();
        _databaseContext = new MongoDBContext(_repository.Collection.Database);
        await _databaseContext.CreateTextIndex(_repository, ProductTextIndex.Fields, ProductTextIndex.Name);
    }

    private GetSearchProductsQueryHandler Handler(bool textSearch = true)
    {
        return new GetSearchProductsQueryHandler(_repository, new Mock<ISpecificationAttributeService>().Object,
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = true }, new AccessControlConfig(),
            new DatabaseConfig { UseProductTextSearch = textSearch }, _databaseContext,
            NullLogger<GetSearchProductsQueryHandler>.Instance);
    }

    private Task Insert(string id, string name, string sku = null, IList<TranslationEntity> locales = null,
        string shortDescription = null)
    {
        return _repository.InsertAsync(new Product {
            Id = id, Name = name, Sku = sku, ShortDescription = shortDescription,
            Locales = locales ?? new List<TranslationEntity>(), Published = true, VisibleIndividually = true
        });
    }

    private async Task<List<string>> Search(string keywords, bool searchDescriptions = false,
        GetSearchProductsQueryHandler handler = null, bool showHidden = false)
    {
        var result = await (handler ?? Handler()).Handle(new GetSearchProductsQuery {
            Customer = new Customer(), Keywords = keywords, SearchDescriptions = searchDescriptions,
            ShowHidden = showHidden
        }, CancellationToken.None);
        return result.products.Select(x => x.Id).ToList();
    }

    [TestMethod]
    public async Task TextSearch_WholeWord()
    {
        await Insert("smartphone", "Smartphone X200");
        await Insert("case", "Phone case");

        CollectionAssert.AreEquivalent(new[] { "case" }, await Search("phone"));
    }

    [TestMethod]
    public async Task TextSearch_IgnoresCase()
    {
        await Insert("case", "Phone case");

        CollectionAssert.AreEquivalent(new[] { "case" }, await Search("PHONE"));
    }

    [TestMethod]
    public async Task TextSearch_KeepsSubstringSemanticsOnCandidates()
    {
        await Insert("case", "Phone case");
        await Insert("reversed", "Case for phone");

        CollectionAssert.AreEquivalent(new[] { "case" }, await Search("phone case"));
    }

    [TestMethod]
    public async Task TextSearch_TranslatedDescription_NotMatched()
    {
        await Insert("shoes", "Shoes",
            locales: [new TranslationEntity { LanguageId = "pl", LocaleKey = "FullDescription", LocaleValue = "waterproof" }]);

        Assert.AreEqual(0, (await Search("waterproof")).Count);
    }

    [TestMethod]
    public async Task TextSearch_TranslatedName_Matched()
    {
        await Insert("boat", "Boat",
            locales: [new TranslationEntity { LanguageId = "pl", LocaleKey = "Name", LocaleValue = "Łódka" }]);

        CollectionAssert.AreEquivalent(new[] { "boat" }, await Search("łódka"));
    }

    [TestMethod]
    public async Task TextSearch_QuotesAndMinus_ArePlainWords()
    {
        await Insert("sku", "Valve", "AB-778");
        await Insert("case", "Phone case");

        CollectionAssert.AreEquivalent(new[] { "sku" }, await Search("ab-778"));
        Assert.AreEqual(0, (await Search("\"phone case\"")).Count);
        Assert.AreEqual(0, (await Search("-case")).Count);
    }

    [TestMethod]
    public async Task TextSearch_SearchDescriptions_UsesOldPath()
    {
        await Insert("boots", "Boots", shortDescription: "waterproof");

        CollectionAssert.AreEquivalent(new[] { "boots" }, await Search("proof", searchDescriptions: true));
    }

    [TestMethod]
    public async Task TextSearch_Off_FragmentStillMatches()
    {
        await Insert("smartphone", "Smartphone");

        CollectionAssert.AreEquivalent(new[] { "smartphone" }, await Search("phone", handler: Handler(false)));
    }

    [TestMethod]
    public async Task TextSearch_NoIndex_FragmentStillMatches()
    {
        await _databaseContext.DeleteIndex(_repository, ProductTextIndex.Name);
        await Insert("smartphone", "Smartphone");

        CollectionAssert.AreEquivalent(new[] { "smartphone" }, await Search("phone"));
        Assert.IsFalse(_repository.Pipeline.Contains("$text"));
    }

    [TestMethod]
    public async Task TextSearch_IndexDroppedAfterASearch_FallsBackToScan()
    {
        var handler = Handler();
        await Insert("smartphone", "Smartphone");
        await Search("phone", handler: handler);

        await _databaseContext.DeleteIndex(_repository, ProductTextIndex.Name);

        CollectionAssert.AreEquivalent(new[] { "smartphone" }, await Search("phone", handler: handler));
        Assert.IsFalse(_repository.Pipeline.Contains("$text"));
    }

    [TestMethod]
    public async Task TextSearch_ShowHidden_FragmentStillMatches()
    {
        await Insert("smartphone", "Smartphone", "AB778X");

        CollectionAssert.AreEquivalent(new[] { "smartphone" }, await Search("phone", showHidden: true));
        CollectionAssert.AreEquivalent(new[] { "smartphone" }, await Search("778", showHidden: true));
    }

    [TestMethod]
    public async Task TextSearch_OnlyPunctuation_UsesOldPath()
    {
        await Insert("router", "Wi-Fi router");

        CollectionAssert.AreEquivalent(new[] { "router" }, await Search("-"));
    }

    [TestMethod]
    public async Task TextSearch_ScriptWithoutSpaces_UsesOldPath()
    {
        await Insert("phone", "华为手机");

        CollectionAssert.AreEquivalent(new[] { "phone" }, await Search("手机"));
    }

    [TestMethod]
    public async Task TextSearch_IsTheFirstStage()
    {
        await Insert("case", "Phone case");

        await Search("phone");

        StringAssert.Contains(_repository.Pipeline, "Aggregate([{ \"$match\" : { \"$text\" : ");
    }

    private class CapturingRepository : MongoDBRepositoryTest<Product>
    {
        public string Pipeline { get; private set; }

        public override Task<IPagedList<TResult>> PagedAsync<TResult>(IQueryable<TResult> query, int pageIndex,
            int pageSize, CancellationToken cancellationToken = default)
        {
            Pipeline = query.ToString();
            return base.PagedAsync(query, pageIndex, pageSize, cancellationToken);
        }
    }
}
