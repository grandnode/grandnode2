using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Configuration;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Module.Installer.Services.MessageTemplates;
using Grand.Module.Migration.Migrations._2._5;
using Grand.Modules.Tests.Installer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.RegularExpressions;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationMerchandiseReturnNoteStoreOwnerTemplateTests
{
    private const string TemplateName = "NewMerchandiseReturnNote.StoreOwnerNotification";

    private Mock<ICacheBase> _cache;
    private LiteDBRepositoryMock<EmailAccount> _emailAccounts;
    private MigrationMerchandiseReturnNoteStoreOwnerTemplate _migration;
    private IServiceProvider _serviceProvider;
    private LiteDBRepositoryMock<MessageTemplate> _templates;

    [TestInitialize]
    public void Init()
    {
        _templates = new LiteDBRepositoryMock<MessageTemplate>();
        _emailAccounts = new LiteDBRepositoryMock<EmailAccount>();
        _cache = new Mock<ICacheBase>();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton<IRepository<EmailAccount>>(_emailAccounts);
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnNoteStoreOwnerTemplate>>(
            NullLogger<MigrationMerchandiseReturnNoteStoreOwnerTemplate>.Instance);
        _serviceProvider = services.BuildServiceProvider();
        _migration = new MigrationMerchandiseReturnNoteStoreOwnerTemplate();
    }

    [TestMethod]
    public async Task Upgrade_InsertsMissing_LeavesExisting()
    {
        var account = new EmailAccount { Email = "first@example.com" };
        _emailAccounts.Insert(account);

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var inserted = _templates.Table.Single(x => x.Name == TemplateName);
        Assert.IsTrue(inserted.IsActive);
        Assert.AreEqual(account.Id, inserted.EmailAccountId);
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()), Times.Once);

        //the operator's own edit survives a second run
        inserted.Subject = "Our subject";
        _templates.Update(inserted);

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual("Our subject", _templates.Table.Single(x => x.Name == TemplateName).Subject);
    }

    [TestMethod]
    public async Task Upgrade_RepositoryFails_ReturnsFalseWithoutThrowing()
    {
        var failing = new Mock<IRepository<MessageTemplate>>();
        failing.SetupGet(x => x.Table).Throws(new InvalidOperationException("db"));
        var services = new ServiceCollection();
        services.AddSingleton(failing.Object);
        services.AddSingleton<IRepository<EmailAccount>>(_emailAccounts);
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton<ILogger<MigrationMerchandiseReturnNoteStoreOwnerTemplate>>(
            NullLogger<MigrationMerchandiseReturnNoteStoreOwnerTemplate>.Instance);

        Assert.IsFalse(await _migration.UpgradeProcess(services.BuildServiceProvider()));
    }

    [TestMethod]
    public void Template_EqualsInstallerSeed()
    {
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == TemplateName);
        var migrated = MigrationMerchandiseReturnNoteStoreOwnerTemplate.Template("ea");

        Assert.AreEqual(seeded.Subject, migrated.Subject);
        CollectionAssert.AreEqual(MessageTemplateSeedTests.Tokens(seeded.Body).ToList(),
            MessageTemplateSeedTests.Tokens(migrated.Body).ToList());
        Assert.AreEqual(Normalize(seeded.Body), Normalize(migrated.Body));
    }

    private static string Normalize(string body)
    {
        return Regex.Replace(body, @"\s+", " ");
    }
}
