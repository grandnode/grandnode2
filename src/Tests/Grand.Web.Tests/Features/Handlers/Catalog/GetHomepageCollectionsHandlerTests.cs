using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Features.Handlers.Catalog;
using Grand.Web.Features.Models.Catalog;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Features.Handlers.Catalog;

[TestClass]
public class GetHomepageCollectionsHandlerTests
{
    private GetHomepageCollectionsHandler _handler;
    private Mock<ICollectionService> _collectionServiceMock;

    [TestInitialize]
    public void Init()
    {
        _collectionServiceMock = new Mock<ICollectionService>();
        _collectionServiceMock.Setup(s => s.GetAllCollections(It.IsAny<string>(), It.IsAny<CatalogAccess>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((string _, CatalogAccess access, int _, int _) => {
                var collections = new List<Collection> { new() { Id = "public", ShowOnHomePage = true } };
                if (access.Customer.Groups.Contains("vip"))
                    collections.Add(new Collection { Id = "vip-only", ShowOnHomePage = true });
                return new PagedList<Collection>(collections, 0, int.MaxValue);
            });

        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(s => s.GetResource(It.IsAny<string>())).Returns("{0}");

        var cacheBase = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new GetHomepageCollectionsHandler(cacheBase, _collectionServiceMock.Object,
            new Mock<IPictureService>().Object, translationServiceMock.Object, new MediaSettings());
    }

    private Task<IList<Web.Models.Catalog.CollectionModel>> Handle(params string[] groups)
    {
        return HandleIn("store-1", "lang-1", groups);
    }

    private Task<IList<Web.Models.Catalog.CollectionModel>> HandleIn(string storeId, string languageId, params string[] groups)
    {
        var customer = new Customer();
        foreach (var group in groups) customer.Groups.Add(group);
        return _handler.Handle(new GetHomepageCollections {
            Customer = customer,
            Store = new Domain.Stores.Store { Id = storeId },
            Language = new Language { Id = languageId }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_GuestAfterGroupMember_DoesNotSeeGroupLimitedCollection()
    {
        var vip = await Handle("registered", "vip");
        var guest = await Handle("guests");

        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Select(x => x.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "public" }, guest.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Handle_GroupMemberAfterGuest_SeesGroupLimitedCollection()
    {
        await Handle("guests");
        var vip = await Handle("registered", "vip");

        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Handle_OtherStoreOrLanguage_DoesNotReuseCachedEntry()
    {
        await HandleIn("store-1", "lang-1", "guests");
        await HandleIn("store-2", "lang-1", "guests");
        await HandleIn("store-1", "lang-2", "guests");
        await HandleIn("store-1", "lang-1", "guests");

        _collectionServiceMock.Verify(s => s.GetAllCollections(It.IsAny<string>(), It.IsAny<CatalogAccess>(),
            It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
    }
}
