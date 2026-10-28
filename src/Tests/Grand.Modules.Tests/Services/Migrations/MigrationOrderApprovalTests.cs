using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Configuration;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
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
public class MigrationOrderApprovalTests
{
    private static readonly string[] TemplateNames = ["OrderApproval.Requested", "OrderApproval.Decided"];

    private LiteDBRepositoryMock<MessageTemplate> _templates;
    private LiteDBRepositoryMock<EmailAccount> _emailAccounts;
    private LiteDBRepositoryMock<Setting> _settings;
    private LiteDBRepositoryMock<OrderStatus> _orderStatuses;
    private Mock<IDatabaseContext> _context;
    private Mock<ICacheBase> _cache;
    private IServiceProvider _serviceProvider;
    private MigrationOrderApproval _migration;

    [TestInitialize]
    public void Init()
    {
        _templates = new LiteDBRepositoryMock<MessageTemplate>();
        _emailAccounts = new LiteDBRepositoryMock<EmailAccount>();
        _settings = new LiteDBRepositoryMock<Setting>();
        _orderStatuses = new LiteDBRepositoryMock<OrderStatus>();
        _context = new Mock<IDatabaseContext>();
        _cache = new Mock<ICacheBase>();
        _serviceProvider = BuildServices();
        _migration = new MigrationOrderApproval();
    }

    private IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton<IRepository<EmailAccount>>(_emailAccounts);
        services.AddSingleton<IRepository<Setting>>(_settings);
        services.AddSingleton<IRepository<OrderStatus>>(_orderStatuses);
        services.AddSingleton(Mock.Of<IRepository<OrderApproval>>());
        services.AddSingleton(_context.Object);
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<MigrationOrderApproval>>(NullLogger<MigrationOrderApproval>.Instance);
        return services.BuildServiceProvider();
    }

    private void SetDefaultEmailAccount(string id)
    {
        _settings.Insert(SettingExtensions.CreateSetting(new EmailAccountSettings { DefaultEmailAccountId = id }));
    }

    [TestMethod]
    public async Task Upgrade_CreatesTemplatesOnce()
    {
        var account = new EmailAccount { Email = "first@example.com" };
        _emailAccounts.Insert(account);

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var templates = _templates.Table.ToList();
        CollectionAssert.AreEquivalent(TemplateNames, templates.Select(x => x.Name).ToList());
        foreach (var template in templates)
        {
            Assert.IsTrue(template.IsActive);
            Assert.AreEqual(account.Id, template.EmailAccountId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Subject));
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Body));
        }

        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Upgrade_SecondRun_ChangesNothing()
    {
        _emailAccounts.Insert(new EmailAccount { Email = "first@example.com" });
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        var before = _templates.Table.OrderBy(x => x.Name).ToList().Select(x => (x.Id, x.Name, x.Subject, x.Body)).ToList();

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var after = _templates.Table.OrderBy(x => x.Name).ToList().Select(x => (x.Id, x.Name, x.Subject, x.Body)).ToList();
        CollectionAssert.AreEqual(before, after);
        //nothing inserted, so no reason to drop the cached lookups again
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Upgrade_ExistingTemplateKept()
    {
        _templates.Insert(new MessageTemplate {
            Name = "OrderApproval.Requested", Subject = "Our subject", Body = "<p>Our text</p>", IsActive = false
        });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var kept = _templates.Table.Single(x => x.Name == "OrderApproval.Requested");
        Assert.AreEqual("Our subject", kept.Subject);
        Assert.AreEqual("<p>Our text</p>", kept.Body);
        Assert.IsFalse(kept.IsActive);
        //the one the operator did not write is still added
        Assert.AreEqual(1, _templates.Table.Count(x => x.Name == "OrderApproval.Decided"));
        Assert.AreEqual(2, _templates.Table.Count());
    }

    [TestMethod]
    public async Task Upgrade_DefaultEmailAccountSetting_WinsOverTheFirstAccount()
    {
        _emailAccounts.Insert(new EmailAccount { Email = "first@example.com" });
        var defaultAccount = new EmailAccount { Email = "default@example.com" };
        _emailAccounts.Insert(defaultAccount);
        SetDefaultEmailAccount(defaultAccount.Id);

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsTrue(_templates.Table.All(x => x.EmailAccountId == defaultAccount.Id));
    }

    [TestMethod]
    public async Task Upgrade_DefaultEmailAccountGone_UsesTheFirstAccount()
    {
        var first = new EmailAccount { Email = "first@example.com" };
        _emailAccounts.Insert(first);
        SetDefaultEmailAccount("deleted-account");

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsTrue(_templates.Table.All(x => x.EmailAccountId == first.Id));
    }

    [TestMethod]
    public async Task Upgrade_NoEmailAccount_LeavesItEmptySoTheDefaultApplies()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(2, _templates.Table.Count());
        Assert.IsTrue(_templates.Table.All(x => string.IsNullOrEmpty(x.EmailAccountId)));
    }

    [TestMethod]
    public async Task Upgrade_CreatesTheOrderApprovalIndexesTheInstallerCreates()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        _context.Verify(x => x.CreateIndex(It.IsAny<IRepository<OrderApproval>>(),
            It.IsAny<OrderBuilder<OrderApproval>>(), "CompanyOwnerId_Status_CreatedOnUtc", false), Times.Once);
        _context.Verify(x => x.CreateIndex(It.IsAny<IRepository<OrderApproval>>(),
            It.IsAny<OrderBuilder<OrderApproval>>(), "CustomerId_StoreId_Status", false), Times.Once);
    }

    [TestMethod]
    public async Task Upgrade_IndexFails_ReturnsFalseWithoutThrowing()
    {
        _context.Setup(x => x.CreateIndex(It.IsAny<IRepository<OrderApproval>>(),
                It.IsAny<OrderBuilder<OrderApproval>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("index"));

        Assert.IsFalse(await _migration.UpgradeProcess(_serviceProvider));
    }

    [TestMethod]
    public async Task Upgrade_SeedsTheAwaitingApprovalStatusOnce()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var status = _orderStatuses.Table.Single(x => x.StatusId == (int)OrderStatusSystem.AwaitingApproval);
        Assert.AreEqual("Awaiting approval", status.Name);
        Assert.AreEqual(5, status.DisplayOrder);
        Assert.IsTrue(status.IsSystem);
        Assert.AreEqual(1, _orderStatuses.Table.Count());
        //the cached list of statuses would not show the new one
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Upgrade_ExistingStatusWithThatIdIsKept()
    {
        _orderStatuses.Insert(new OrderStatus { StatusId = 5, Name = "Our status", DisplayOrder = 9, IsSystem = false });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var status = _orderStatuses.Table.Single();
        Assert.AreEqual("Our status", status.Name);
        Assert.AreEqual(9, status.DisplayOrder);
        Assert.IsFalse(status.IsSystem);
    }

    [TestMethod]
    public async Task Upgrade_LeavesTheOtherOrderStatusesAlone()
    {
        _orderStatuses.Insert(new OrderStatus { StatusId = 10, Name = "Pending", IsSystem = true });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.AreEqual(2, _orderStatuses.Table.Count());
        Assert.AreEqual("Pending", _orderStatuses.Table.Single(x => x.StatusId == 10).Name);
    }

    [TestMethod]
    public async Task Upgrade_SeedsTheOrderApprovalSettingsOnce()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var name = nameof(OrderApprovalSettings).ToLowerInvariant();
        var setting = _settings.Table.Single(x => x.Name == name);
        Assert.IsTrue(string.IsNullOrEmpty(setting.StoreId));
        var settings = System.Text.Json.JsonSerializer.Deserialize<OrderApprovalSettings>(setting.Metadata);
        CollectionAssert.AreEqual(new[] { "Payments.CashOnDelivery" }, settings.PaymentMethodSystemNames);
    }

    [TestMethod]
    public async Task Upgrade_ExistingOrderApprovalSettingsAreKept()
    {
        _settings.Insert(SettingExtensions.CreateSetting(new OrderApprovalSettings {
            PaymentMethodSystemNames = ["Payments.Invoice", "Payments.BankTransfer"]
        }));

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var name = nameof(OrderApprovalSettings).ToLowerInvariant();
        var setting = _settings.Table.Single(x => x.Name == name);
        var settings = System.Text.Json.JsonSerializer.Deserialize<OrderApprovalSettings>(setting.Metadata);
        CollectionAssert.AreEqual(new[] { "Payments.Invoice", "Payments.BankTransfer" },
            settings.PaymentMethodSystemNames);
    }

    [TestMethod]
    public void Templates_RequestedShowsTheOrderNumber_DecidedLinksToTheOrder()
    {
        var requested = MigrationOrderApproval.Templates("ea").Single(x => x.Name == "OrderApproval.Requested");
        var decided = MigrationOrderApproval.Templates("ea").Single(x => x.Name == "OrderApproval.Decided");

        StringAssert.Contains(requested.Body, "{{OrderApproval.OrderNumber}}");
        StringAssert.Contains(decided.Body, "{{OrderApproval.OrderUrl}}");
        Assert.IsFalse(decided.Body.Contains("OrderApproval.CartUrl"));
        Assert.IsFalse(requested.Subject.Contains('—') || decided.Subject.Contains('—'));
    }

    [TestMethod]
    [DataRow("OrderApproval.Requested")]
    [DataRow("OrderApproval.Decided")]
    public void Templates_UseTheSubjectAndTokensOfTheInstallerSeed(string name)
    {
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == name);
        var migrated = MigrationOrderApproval.Templates("ea").Single(x => x.Name == name);

        Assert.AreEqual(seeded.Subject, migrated.Subject);
        Assert.IsTrue(migrated.IsActive);
        CollectionAssert.AreEqual(
            MessageTemplateSeedTests.Tokens(seeded.Body).ToList(),
            MessageTemplateSeedTests.Tokens(migrated.Body).ToList());
        // the markup too, whatever the indentation
        Assert.AreEqual(Normalize(seeded.Body), Normalize(migrated.Body));
    }

    private static string Normalize(string body)
    {
        return Regex.Replace(body, @"\s+", " ");
    }

    [TestMethod]
    [DataRow("OrderApproval.Requested")]
    [DataRow("OrderApproval.Decided")]
    public void Templates_DoNotRenderABareOrderSignWithoutAnOrderNumber(string name)
    {
        var body = MigrationOrderApproval.Templates("ea").Single(x => x.Name == name).Body;
        var seeded = MessageTemplateSeed.Build("ea").Single(x => x.Name == name).Body;

        foreach (var text in new[] { body, seeded })
        {
            var unguarded = Regex.Replace(text, @"\{% if OrderApproval\.OrderNumber != '' %\}.*?\{% (else|endif) %\}", "",
                RegexOptions.Singleline);
            Assert.IsFalse(unguarded.Contains("#{{OrderApproval.OrderNumber}}"));
        }
    }
}
