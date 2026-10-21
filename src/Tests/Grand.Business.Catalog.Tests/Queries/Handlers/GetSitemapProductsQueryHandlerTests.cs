using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetSitemapProductsQueryHandlerTests
{
    private const string StoreId = "store1";
    private const string GuestsGroupId = "guests-group";
    private GetSitemapProductsQueryHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _handler = new GetSitemapProductsQueryHandler(_repository, new CatalogSettings(), new AccessControlConfig());
    }

    private Task<IList<SitemapProduct>> Handle(string afterId = null, int pageSize = 1000)
    {
        return _handler.Handle(new GetSitemapProductsQuery {
            StoreId = StoreId,
            CustomerGroupIds = [GuestsGroupId],
            LanguageId = "lang1",
            AfterId = afterId,
            PageSize = pageSize
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_ReturnsOnlyProductsAGuestCanSeeInTheStore()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Id = "open", Published = true, VisibleIndividually = true });
        await _repository.InsertAsync(new Product {
            Id = "guests", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = [GuestsGroupId]
        });
        await _repository.InsertAsync(new Product {
            Id = "admins", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["admins-group"]
        });
        await _repository.InsertAsync(new Product {
            Id = "this-store", Published = true, VisibleIndividually = true, LimitedToStores = true,
            Stores = [StoreId]
        });
        await _repository.InsertAsync(new Product {
            Id = "other-store", Published = true, VisibleIndividually = true, LimitedToStores = true,
            Stores = ["store2"]
        });
        await _repository.InsertAsync(new Product { Id = "draft", Published = false, VisibleIndividually = true });
        await _repository.InsertAsync(new Product { Id = "variant", Published = true, VisibleIndividually = false });
        await _repository.InsertAsync(new Product {
            Id = "expired", Published = true, VisibleIndividually = true,
            AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await _repository.InsertAsync(new Product {
            Id = "upcoming", Published = true, VisibleIndividually = true,
            AvailableStartDateTimeUtc = DateTime.UtcNow.AddDays(1)
        });

        //Act
        var result = await Handle();

        //Assert
        CollectionAssert.AreEquivalent(new[] { "open", "guests", "this-store" }, result.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_ProjectsTheTranslatedSeNameFirstPictureAndLastChange()
    {
        //Arrange
        var updatedOn = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var product = new Product {
            Id = "translated", SeName = "default-name", Published = true, VisibleIndividually = true,
            Locales = [
                new TranslationEntity { LanguageId = "lang2", LocaleKey = "SeName", LocaleValue = "other-name" },
                new TranslationEntity { LanguageId = "lang1", LocaleKey = "Name", LocaleValue = "Name" },
                new TranslationEntity { LanguageId = "lang1", LocaleKey = "SeName", LocaleValue = "translated-name" }
            ]
        };
        product.ProductPictures.Add(new ProductPicture { PictureId = "picture1" });
        product.ProductPictures.Add(new ProductPicture { PictureId = "picture2" });
        await _repository.InsertAsync(product);
        await _repository.UpdateField(product.Id, p => p.UpdatedOnUtc, updatedOn);
        await _repository.InsertAsync(new Product {
            Id = "untranslated", SeName = "default-only", Published = true, VisibleIndividually = true
        });

        //Act
        var result = (await Handle()).ToDictionary(p => p.Id);

        //Assert
        Assert.AreEqual("translated-name", result["translated"].SeName);
        Assert.AreEqual("picture1", result["translated"].PictureId);
        Assert.AreEqual(updatedOn, result["translated"].UpdatedOnUtc.ToUniversalTime());
        Assert.AreEqual("default-only", result["untranslated"].SeName);
        Assert.IsNull(result["untranslated"].PictureId);
    }

    [TestMethod]
    public async Task Handle_PagesByIdAfterTheLastProductOfTheBatch()
    {
        //Arrange
        foreach (var id in new[] { "c", "a", "e", "b", "d" })
            await _repository.InsertAsync(new Product { Id = id, Published = true, VisibleIndividually = true });

        //Act
        var first = await Handle(pageSize: 2);
        var second = await Handle(first[^1].Id, 2);
        var last = await Handle(second[^1].Id, 2);

        //Assert
        CollectionAssert.AreEqual(new[] { "a", "b" }, first.Select(p => p.Id).ToList());
        CollectionAssert.AreEqual(new[] { "c", "d" }, second.Select(p => p.Id).ToList());
        CollectionAssert.AreEqual(new[] { "e" }, last.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_NoCustomerGroups_ReturnsOnlyProductsNotLimitedToGroups()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Id = "open", Published = true, VisibleIndividually = true });
        await _repository.InsertAsync(new Product {
            Id = "guests", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = [GuestsGroupId]
        });

        //Act
        var result = await _handler.Handle(new GetSitemapProductsQuery { StoreId = StoreId, CustomerGroupIds = null },
            CancellationToken.None);

        //Assert
        CollectionAssert.AreEqual(new[] { "open" }, result.Select(p => p.Id).ToList());
    }

    [TestMethod]
    public async Task Handle_LiteDB_ReturnsTheRestOfTheCatalogInOneBatch()
    {
        //Arrange
        var repository = new LiteDBRepositoryMock<Product>();
        var handler = new GetSitemapProductsQueryHandler(repository, new CatalogSettings(), new AccessControlConfig());
        foreach (var id in new[] { "c", "a", "e", "b", "d" })
            await repository.InsertAsync(new Product { Id = id, Published = true, VisibleIndividually = true });

        //Act
        var result = await handler.Handle(new GetSitemapProductsQuery {
            StoreId = StoreId, CustomerGroupIds = [GuestsGroupId], AfterId = "a", PageSize = 2
        }, CancellationToken.None);

        //Assert
        CollectionAssert.AreEqual(new[] { "b", "c", "d", "e" }, result.Select(p => p.Id).ToList());
    }
}
