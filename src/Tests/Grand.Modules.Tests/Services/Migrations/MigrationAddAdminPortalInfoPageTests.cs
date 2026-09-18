using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Module.Migration.Migrations._2._4;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationAddAdminPortalInfoPageTests
{
    private LiteDBRepositoryMock<Page> _pageRepository;
    private LiteDBRepositoryMock<PageLayout> _pageLayoutRepository;
    private LiteDBRepositoryMock<EntityUrl> _entityUrlRepository;
    private Mock<ICacheBase> _cacheMock;
    private IServiceProvider _serviceProvider;
    private MigrationAddAdminPortalInfoPage _migration;

    [TestInitialize]
    public void Init()
    {
        _pageRepository = new LiteDBRepositoryMock<Page>();
        _pageLayoutRepository = new LiteDBRepositoryMock<PageLayout>();
        _entityUrlRepository = new LiteDBRepositoryMock<EntityUrl>();
        _cacheMock = new Mock<ICacheBase>();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<Page>>(_pageRepository);
        services.AddSingleton<IRepository<PageLayout>>(_pageLayoutRepository);
        services.AddSingleton<IRepository<EntityUrl>>(_entityUrlRepository);
        services.AddSingleton(_cacheMock.Object);
        services.AddSingleton<ILogger<MigrationAddAdminPortalInfoPage>>(
            NullLogger<MigrationAddAdminPortalInfoPage>.Instance);
        _serviceProvider = services.BuildServiceProvider();

        _migration = new MigrationAddAdminPortalInfoPage();
    }

    [TestMethod]
    public void UpgradeProcess_NoPage_AddsUnpublishedPageWithSlug()
    {
        _pageLayoutRepository.Insert(new PageLayout { Name = "Default layout" });

        var result = _migration.UpgradeProcess(_serviceProvider);

        Assert.IsTrue(result);
        var page = _pageRepository.Table.Single();
        Assert.AreEqual("AdminPortalInfo", page.SystemName);
        Assert.IsFalse(page.Published);
        Assert.IsFalse(page.IncludeInSitemap);
        Assert.IsFalse(string.IsNullOrEmpty(page.Title));
        Assert.IsFalse(string.IsNullOrEmpty(page.Body));
        Assert.AreEqual(_pageLayoutRepository.Table.Single().Id, page.PageLayoutId);
        Assert.AreEqual("adminportalinfo", page.SeName);

        var slug = _entityUrlRepository.Table.Single();
        Assert.AreEqual(page.Id, slug.EntityId);
        Assert.AreEqual("Page", slug.EntityName);
        Assert.AreEqual("adminportalinfo", slug.Slug);
        //LiteDB reads an empty string back as null
        Assert.IsTrue(string.IsNullOrEmpty(slug.LanguageId));
        Assert.IsTrue(slug.IsActive);
        _cacheMock.Verify(x => x.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public void UpgradeProcess_RunTwice_DoesNotDuplicate()
    {
        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(1, _pageRepository.Table.Count());
        Assert.AreEqual(1, _entityUrlRepository.Table.Count());
    }

    [TestMethod]
    public void UpgradeProcess_PageAlreadyExists_KeepsOperatorContent()
    {
        var existing = new Page {
            SystemName = "adminportalinfo", Title = "Our own title", Body = "<p>Our own text</p>", Published = true
        };
        _pageRepository.Insert(existing);
        _entityUrlRepository.Insert(new EntityUrl {
            EntityId = existing.Id, EntityName = "Page", Slug = "adminportalinfo", LanguageId = "", IsActive = true
        });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        var page = _pageRepository.Table.Single();
        Assert.AreEqual("Our own title", page.Title);
        Assert.AreEqual("<p>Our own text</p>", page.Body);
        Assert.IsTrue(page.Published);
        Assert.AreEqual(1, _entityUrlRepository.Table.Count());
    }

    [TestMethod]
    public void UpgradeProcess_PageWithoutSlug_AddsOnlyTheSlug()
    {
        var existing = new Page { SystemName = "AdminPortalInfo", Title = "Kept" };
        _pageRepository.Insert(existing);

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(1, _pageRepository.Table.Count());
        Assert.AreEqual("Kept", _pageRepository.Table.Single().Title);
        Assert.AreEqual(existing.Id, _entityUrlRepository.Table.Single().EntityId);
    }

    [TestMethod]
    public void UpgradeProcess_SlugTakenByAnotherEntity_UsesAFreeSlug()
    {
        _entityUrlRepository.Insert(new EntityUrl {
            EntityId = "other", EntityName = "Product", Slug = "adminportalinfo", LanguageId = "", IsActive = true
        });

        Assert.IsTrue(_migration.UpgradeProcess(_serviceProvider));

        var page = _pageRepository.Table.Single();
        var slug = _entityUrlRepository.Table.Single(x => x.EntityId == page.Id);
        Assert.AreEqual("adminportalinfo-2", slug.Slug);
        Assert.AreEqual("adminportalinfo-2", page.SeName);
    }

    [TestMethod]
    public void UpgradeProcess_RepositoryThrows_ReturnsFalse()
    {
        var failing = new Mock<IRepository<Page>>();
        failing.Setup(x => x.Table).Throws(new InvalidOperationException("database down"));
        var services = new ServiceCollection();
        services.AddSingleton(failing.Object);
        services.AddSingleton<IRepository<PageLayout>>(_pageLayoutRepository);
        services.AddSingleton<IRepository<EntityUrl>>(_entityUrlRepository);
        services.AddSingleton<ILogger<MigrationAddAdminPortalInfoPage>>(
            NullLogger<MigrationAddAdminPortalInfoPage>.Instance);

        Assert.IsFalse(_migration.UpgradeProcess(services.BuildServiceProvider()));
    }
}
