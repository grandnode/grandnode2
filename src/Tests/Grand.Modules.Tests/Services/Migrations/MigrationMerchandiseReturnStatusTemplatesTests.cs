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
public class MigrationMerchandiseReturnStatusTemplatesTests
{
    private static readonly string[] Names = [
        "MerchandiseReturnAccepted.CustomerNotification", "MerchandiseReturnReceived.CustomerNotification",
        "MerchandiseReturnRejected.CustomerNotification", "MerchandiseReturnCompleted.CustomerNotification"
    ];

    private Mock<ICacheBase> _cache;
    private MigrationMerchandiseReturnStatusTemplates _migration;
    private IServiceProvider _serviceProvider;
    private LiteDBRepositoryMock<MessageTemplate> _templates;

    public MigrationMerchandiseReturnStatusTemplatesTests()
    {
        _templates = new LiteDBRepositoryMock<MessageTemplate>();
        var emailAccounts = new LiteDBRepositoryMock<EmailAccount>();
        emailAccounts.Insert(new EmailAccount { Email = "shop@example.com" });
        _cache = new Mock<ICacheBase>();

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton<IRepository<EmailAccount>>(emailAccounts);
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnStatusTemplates>>(
            NullLogger<MigrationMerchandiseReturnStatusTemplates>.Instance);
        _serviceProvider = services.BuildServiceProvider();
        _migration = new MigrationMerchandiseReturnStatusTemplates();
    }

    [TestMethod]
    public async Task Adds_FourTemplates_Once()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        CollectionAssert.AreEquivalent(Names, _templates.Table.Select(x => x.Name).ToArray());
        Assert.IsTrue(_templates.Table.All(x => x.IsActive && !string.IsNullOrEmpty(x.EmailAccountId)));
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task ExistingTemplate_Untouched()
    {
        _templates.Insert(new MessageTemplate { Name = Names[0], Subject = "Our subject" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual("Our subject", _templates.Table.Single(x => x.Name == Names[0]).Subject);
        Assert.AreEqual(4, _templates.Table.Count());
    }

    [TestMethod]
    public void Templates_MatchInstallerSeed()
    {
        var seeded = MessageTemplateSeed.Build("ea").Where(x => Names.Contains(x.Name)).ToList();
        var migrated = MigrationMerchandiseReturnStatusTemplates.Templates("ea");

        Assert.AreEqual(4, seeded.Count);
        foreach (var template in migrated)
        {
            var seed = seeded.Single(x => x.Name == template.Name);
            Assert.AreEqual(seed.Subject, template.Subject);
            CollectionAssert.AreEqual(MessageTemplateSeedTests.Tokens(seed.Body).ToList(),
                MessageTemplateSeedTests.Tokens(template.Body).ToList());
            Assert.AreEqual(Normalize(seed.Body), Normalize(template.Body));
        }
    }

    private static string Normalize(string body)
    {
        return Regex.Replace(body, @"\s+", " ");
    }
}
