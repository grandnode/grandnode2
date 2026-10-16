using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Features.Handlers.Catalog;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Catalog;

[TestClass]
public class GetMenuHandlerTests
{
    private GetMenuHandler _handler;

    [TestInitialize]
    public void Init()
    {
        var brandServiceMock = new Mock<IBrandService>();
        brandServiceMock.Setup(s => s.GetAllBrands(It.IsAny<string>(), It.IsAny<CatalogAccess>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((string _, CatalogAccess access, int _, int _) => {
                var brands = new List<Brand> { new() { Id = "public", IncludeInMenu = true } };
                if (access.Customer.Groups.Contains("vip"))
                    brands.Add(new Brand { Id = "vip-only", IncludeInMenu = true });
                return new PagedList<Brand>(brands, 0, int.MaxValue);
            });

        var collectionServiceMock = new Mock<ICollectionService>();
        collectionServiceMock.Setup(s => s.GetAllCollections(It.IsAny<string>(), It.IsAny<CatalogAccess>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((string _, CatalogAccess access, int _, int _) => {
                var collections = new List<Collection> { new() { Id = "public", IncludeInMenu = true } };
                if (access.Customer.Groups.Contains("vip"))
                    collections.Add(new Collection { Id = "vip-only", IncludeInMenu = true });
                return new PagedList<Collection>(collections, 0, int.MaxValue);
            });

        var categoryServiceMock = new Mock<ICategoryService>();
        categoryServiceMock.Setup(s => s.GetMenuCategories(It.IsAny<Customer>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Category>());

        var pageServiceMock = new Mock<IPageService>();
        pageServiceMock.Setup(s => s.GetAllPages(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Page>());

        var cacheBase = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new GetMenuHandler(categoryServiceMock.Object, brandServiceMock.Object, pageServiceMock.Object,
            cacheBase, collectionServiceMock.Object, new Mock<IPictureService>().Object, new CatalogSettings(),
            new BlogSettings(), new MenuItemSettings(), new MediaSettings());
    }

    private Task<MenuModel> Handle(params string[] groups)
    {
        var customer = new Customer();
        foreach (var group in groups) customer.Groups.Add(group);
        return _handler.Handle(new GetMenu {
            Customer = customer,
            Store = new Domain.Stores.Store { Id = "store-1" },
            Language = new Language { Id = "lang-1" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_GuestAfterGroupMember_DoesNotSeeGroupLimitedBrandsAndCollections()
    {
        await Handle("registered", "vip");
        var guest = await Handle("guests");

        CollectionAssert.AreEquivalent(new[] { "public" }, guest.Brands.Select(x => x.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "public" }, guest.Collections.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Handle_GroupMemberAfterGuest_SeesGroupLimitedBrandsAndCollections()
    {
        await Handle("guests");
        var vip = await Handle("registered", "vip");

        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Brands.Select(x => x.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Collections.Select(x => x.Id).ToArray());
    }
}
