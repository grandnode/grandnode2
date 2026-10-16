using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
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

namespace Grand.Web.Tests.Features.Handlers.Catalog;

[TestClass]
public class GetHomepageBrandsHandlerTests
{
    private GetHomepageBrandsHandler _handler;

    [TestInitialize]
    public void Init()
    {
        var brandServiceMock = new Mock<IBrandService>();
        brandServiceMock.Setup(s => s.GetAllBrands(It.IsAny<string>(), It.IsAny<CatalogAccess>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((string _, CatalogAccess access, int _, int _) => {
                var brands = new List<Brand> { new() { Id = "public", ShowOnHomePage = true } };
                if (access.Customer.Groups.Contains("vip"))
                    brands.Add(new Brand { Id = "vip-only", ShowOnHomePage = true });
                return new PagedList<Brand>(brands, 0, int.MaxValue);
            });

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(s => s.GetResource(It.IsAny<string>())).Returns("{0}");

        var cacheBase = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new GetHomepageBrandsHandler(cacheBase, brandServiceMock.Object,
            new Mock<IPictureService>().Object, translationServiceMock.Object, new MediaSettings());
    }

    private Task<IList<Web.Models.Catalog.BrandModel>> Handle(params string[] groups)
    {
        var customer = new Customer();
        foreach (var group in groups) customer.Groups.Add(group);
        return _handler.Handle(new GetHomepageBrands {
            Customer = customer,
            Store = new Domain.Stores.Store { Id = "store-1" },
            Language = new Language { Id = "lang-1" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_GuestAfterGroupMember_DoesNotSeeGroupLimitedBrand()
    {
        var vip = await Handle("registered", "vip");
        var guest = await Handle("guests");

        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Select(x => x.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "public" }, guest.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Handle_GroupMemberAfterGuest_SeesGroupLimitedBrand()
    {
        await Handle("guests");
        var vip = await Handle("registered", "vip");

        CollectionAssert.AreEquivalent(new[] { "public", "vip-only" }, vip.Select(x => x.Id).ToArray());
    }
}
