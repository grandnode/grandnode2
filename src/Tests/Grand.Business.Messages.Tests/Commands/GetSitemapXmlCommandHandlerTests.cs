using Grand.Business.Core.Commands.System.Common;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Messages.Commands.Handlers.Common;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Xml.Linq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetSitemapXmlCommandHandlerTests
{
    private const string StoreId = "store1";
    private const string GuestsGroupId = "guests-group";
    private const string AdminsGroupId = "admins-group";
    private static readonly XNamespace Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";

    //strict mocks: a call to any overload that reads the ambient work/store context fails the test
    private Mock<IBrandService> _brandServiceMock;
    private Mock<ICategoryService> _categoryServiceMock;
    private CommonSettings _commonSettings;
    private GetSitemapXmlCommandHandler _handler;
    private Mock<IKnowledgebaseService> _knowledgebaseServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPageService> _pageServiceMock;

    [TestInitialize]
    public void Init()
    {
        _categoryServiceMock = new Mock<ICategoryService>(MockBehavior.Strict);
        _brandServiceMock = new Mock<IBrandService>(MockBehavior.Strict);
        _pageServiceMock = new Mock<IPageService>(MockBehavior.Strict);
        _knowledgebaseServiceMock = new Mock<IKnowledgebaseService>(MockBehavior.Strict);
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);

        var blogServiceMock = new Mock<IBlogService>();
        blogServiceMock.Setup(x => x.GetAllBlogPosts(StoreId, null, null, 0, int.MaxValue, false, null, "", ""))
            .ReturnsAsync(new PagedList<BlogPost>());

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(x => x.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests))
            .ReturnsAsync(new CustomerGroup { Id = GuestsGroupId, SystemName = SystemCustomerGroupNames.Guests });

        _categoryServiceMock.Setup(x => x.GetAllCategories(null, "", StoreId, 0, int.MaxValue, true))
            .ReturnsAsync(new PagedList<Category>());
        _brandServiceMock.Setup(x => x.GetAllBrands("", StoreId, 0, int.MaxValue, true))
            .ReturnsAsync(new PagedList<Brand>());
        _pageServiceMock.Setup(x => x.GetAllPages(StoreId, true)).ReturnsAsync(new List<Page>());
        _knowledgebaseServiceMock.Setup(x => x.GetPublicKnowledgebaseArticles(StoreId,
                It.Is<IList<string>>(g => g.SequenceEqual(new[] { GuestsGroupId }))))
            .ReturnsAsync(new List<KnowledgebaseArticle>());
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetSearchProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new PagedList<Product>(), (IList<string>)new List<string>()));

        _commonSettings = new CommonSettings {
            SitemapIncludeCategories = true,
            SitemapIncludeBrands = true,
            SitemapIncludeProducts = true
        };

        _handler = new GetSitemapXmlCommandHandler(_categoryServiceMock.Object, _brandServiceMock.Object,
            _pageServiceMock.Object, blogServiceMock.Object, new Mock<IPictureService>().Object,
            _knowledgebaseServiceMock.Object, groupServiceMock.Object, _mediatorMock.Object, _commonSettings,
            new BlogSettings(), new KnowledgebaseSettings(), new NewsSettings(), new AccessControlConfig(),
            new RouteNameLinkGenerator(), new AppConfig());
    }

    private Task<string> Generate()
    {
        return _handler.Handle(new GetSitemapXmlCommand {
            Store = new Store { Id = StoreId, Url = "https://shop.example.com/" },
            Language = new Language { Id = "lang1", UniqueSeoCode = "en" }
        }, CancellationToken.None);
    }

    private static List<string> Locations(string xml)
    {
        return XDocument.Parse(xml).Descendants(Sitemap + "loc").Select(x => x.Value).ToList();
    }

    [TestMethod]
    public async Task Handle_WhenPagesIncludedInSitemap_WritesNonEmptyPageLocations()
    {
        //Arrange
        _pageServiceMock.Setup(x => x.GetAllPages(StoreId, true)).ReturnsAsync(new List<Page> {
            new() { Id = "1", SystemName = "AboutUs", SeName = "about-us", Published = true, IncludeInSitemap = true },
            new() { Id = "2", SystemName = "Shipping", SeName = "shipping", Published = true, IncludeInSitemap = true }
        });

        //Act
        var xml = await Generate();

        //Assert
        var locations = Locations(xml);
        Assert.IsFalse(locations.Any(string.IsNullOrEmpty), "sitemap contains an empty <loc>");
        CollectionAssert.Contains(locations, "https://shop.example.com/about-us");
        CollectionAssert.Contains(locations, "https://shop.example.com/shipping");
    }

    [TestMethod]
    public async Task Handle_WithoutWorkContext_UsesOnlyExplicitlyScopedQueries()
    {
        //Act - the strict mocks throw if the handler calls an overload that reads the ambient context
        var xml = await Generate();

        //Assert
        Assert.IsFalse(Locations(xml).Any(string.IsNullOrEmpty), "sitemap contains an empty <loc>");
        _categoryServiceMock.Verify(x => x.GetAllCategories(null, "", StoreId, 0, int.MaxValue, true), Times.Once);
        _brandServiceMock.Verify(x => x.GetAllBrands("", StoreId, 0, int.MaxValue, true), Times.Once);
        _pageServiceMock.Verify(x => x.GetAllPages(StoreId, true), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.Is<GetSearchProductsQuery>(q =>
            q.Customer == null && q.StoreId == StoreId && q.VisibleIndividuallyOnly &&
            q.CustomerGroupIds.SequenceEqual(new[] { GuestsGroupId })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ListsOnlyWhatAGuestCanSee()
    {
        //Arrange
        _pageServiceMock.Setup(x => x.GetAllPages(StoreId, true)).ReturnsAsync(new List<Page> {
            new() { Id = "1", SystemName = "Open", SeName = "open-page", Published = true, IncludeInSitemap = true },
            new() {
                Id = "2", SystemName = "ForGuests", SeName = "guest-page", Published = true, IncludeInSitemap = true,
                LimitedToGroups = true, CustomerGroups = [GuestsGroupId]
            },
            new() {
                Id = "3", SystemName = "ForAdmins", SeName = "admin-page", Published = true, IncludeInSitemap = true,
                LimitedToGroups = true, CustomerGroups = [AdminsGroupId]
            },
            new() { Id = "4", SystemName = "Draft", SeName = "draft-page", Published = false, IncludeInSitemap = true }
        });
        _categoryServiceMock.Setup(x => x.GetAllCategories(null, "", StoreId, 0, int.MaxValue, true))
            .ReturnsAsync(new PagedList<Category>(new List<Category> {
                new() { Id = "c1", ParentCategoryId = "", SeName = "open-category", Published = true },
                new() { Id = "c2", ParentCategoryId = "c1", SeName = "child-category", Published = true },
                new() {
                    Id = "c3", ParentCategoryId = "", SeName = "admin-category", Published = true,
                    LimitedToGroups = true, CustomerGroups = [AdminsGroupId]
                },
                new() { Id = "c4", ParentCategoryId = "c3", SeName = "child-of-hidden-category", Published = true },
                new() { Id = "c5", ParentCategoryId = "", SeName = "unpublished-category", Published = false }
            }, 0, int.MaxValue));
        _brandServiceMock.Setup(x => x.GetAllBrands("", StoreId, 0, int.MaxValue, true))
            .ReturnsAsync(new PagedList<Brand>(new List<Brand> {
                new() { Id = "b1", SeName = "open-brand", Published = true },
                new() {
                    Id = "b2", SeName = "admin-brand", Published = true, LimitedToGroups = true,
                    CustomerGroups = [AdminsGroupId]
                },
                new() { Id = "b3", SeName = "unpublished-brand", Published = false }
            }, 0, int.MaxValue));

        //Act
        var locations = Locations(await Generate()).Select(x => x.Replace("https://shop.example.com/", ""))
            .ToList();

        //Assert
        CollectionAssert.IsSubsetOf(
            new[] { "open-page", "guest-page", "open-category", "child-category", "open-brand" }, locations);
        foreach (var hidden in new[] {
                     "admin-page", "draft-page", "admin-category", "child-of-hidden-category",
                     "unpublished-category", "admin-brand", "unpublished-brand"
                 })
            CollectionAssert.DoesNotContain(locations, hidden);
    }

    /// <summary>
    ///     Resolves the storefront's named routes like the real LinkGenerator: a route name that is not
    ///     registered yields null, which the sitemap writes as an empty &lt;loc /&gt;
    /// </summary>
    private sealed class RouteNameLinkGenerator : LinkGenerator
    {
        private static readonly HashSet<string> RouteNames = [
            "HomePage", "ProductSearch", "ContactUs", "NewsArchive", "Blog", "Knowledgebase",
            "Product", "Category", "Brand", "BlogPost", "Page", "KnowledgebaseArticle"
        ];

        public override string GetUriByAddress<TAddress>(TAddress address, RouteValueDictionary values,
            string scheme, HostString host, PathString pathBase = default, FragmentString fragment = default,
            LinkOptions options = null)
        {
            if (address is not RouteValuesAddress routeAddress || !RouteNames.Contains(routeAddress.RouteName))
                return null;

            return $"{scheme}://{host}/{values["SeName"]}";
        }

        public override string GetUriByAddress<TAddress>(HttpContext httpContext, TAddress address,
            RouteValueDictionary values, RouteValueDictionary ambientValues = null, string scheme = null,
            HostString? host = null, PathString? pathBase = null, FragmentString fragment = default,
            LinkOptions options = null)
        {
            throw new NotSupportedException();
        }

        public override string GetPathByAddress<TAddress>(HttpContext httpContext, TAddress address,
            RouteValueDictionary values, RouteValueDictionary ambientValues = null, PathString? pathBase = null,
            FragmentString fragment = default, LinkOptions options = null)
        {
            throw new NotSupportedException();
        }

        public override string GetPathByAddress<TAddress>(TAddress address, RouteValueDictionary values,
            PathString pathBase = default, FragmentString fragment = default, LinkOptions options = null)
        {
            throw new NotSupportedException();
        }
    }
}
