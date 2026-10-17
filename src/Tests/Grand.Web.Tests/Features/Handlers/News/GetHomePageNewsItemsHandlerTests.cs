using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Features.Handlers.News;
using Grand.Web.Features.Models.News;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.News;

[TestClass]
public class GetHomePageNewsItemsHandlerTests
{
    private readonly AccessControlConfig _accessControlConfig = new();
    private Customer _currentCustomer;
    private GetHomePageNewsItemsHandler _handler;
    private Mock<INewsService> _newsServiceMock;

    [TestInitialize]
    public void Init()
    {
        _newsServiceMock = new Mock<INewsService>();
        _newsServiceMock.Setup(s => s.GetAllNews(It.IsAny<CatalogAccess>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync((CatalogAccess access, int _, int _, bool _, string _) => {
                var news = new List<NewsItem> { new() { Id = "public" } };
                if (access.Customer.Groups.Contains("vip"))
                    news.Add(new NewsItem { Id = "vip-only" });
                return new PagedList<NewsItem>(news, 0, int.MaxValue);
            });

        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => _currentCustomer);
        contextAccessorMock.Setup(c => c.WorkContext.WorkingLanguage).Returns(new Language { Id = "lang-1" });
        contextAccessorMock.Setup(c => c.StoreContext.CurrentStore).Returns(new Domain.Stores.Store { Id = "store-1" });

        var cacheBase = new MemoryCacheBase(new MemoryCache(new MemoryCacheOptions()), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new GetHomePageNewsItemsHandler(cacheBase, contextAccessorMock.Object, _newsServiceMock.Object,
            new Mock<IDateTimeService>().Object, new Mock<IPictureService>().Object,
            new Mock<ITranslationService>().Object, new NewsSettings(), new MediaSettings(),
            _accessControlConfig);
    }

    private Task<Web.Models.News.HomePageNewsItemsModel> HandleAs(params string[] groups)
    {
        _currentCustomer = new Customer();
        foreach (var group in groups) _currentCustomer.Groups.Add(group);
        return _handler.Handle(new GetHomePageNewsItems(), CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_GuestAfterGroupMember_DoesNotSeeGroupLimitedNews()
    {
        var vip = await HandleAs("registered", "vip");
        var guest = await HandleAs("guests");

        Assert.IsTrue(vip.NewsItems.Any(x => x.Id == "vip-only"));
        Assert.IsFalse(guest.NewsItems.Any(x => x.Id == "vip-only"));
    }

    [TestMethod]
    public async Task Handle_GroupMemberAfterGuest_SeesGroupLimitedNews()
    {
        await HandleAs("guests");
        var vip = await HandleAs("registered", "vip");

        Assert.IsTrue(vip.NewsItems.Any(x => x.Id == "vip-only"));
    }

    [TestMethod]
    public async Task Handle_IgnoreAcl_SharesOneEntryAcrossGroups()
    {
        _accessControlConfig.IgnoreAcl = true;

        await HandleAs("guests");
        await HandleAs("registered", "vip");

        _newsServiceMock.Verify(s => s.GetAllNews(It.IsAny<CatalogAccess>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }
}
