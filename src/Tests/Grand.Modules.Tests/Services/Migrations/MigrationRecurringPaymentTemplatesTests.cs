using Grand.Business.Messages.DotLiquidDrops;
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
public class MigrationRecurringPaymentTemplatesTests
{
    private const string Edited = "RecurringPayment.Cancelled.CustomerNotification";

    private static readonly string[] Names = [
        Edited, "RecurringPayment.Cancelled.StoreOwnerNotification",
        "RecurringPayment.PaymentFailed.CustomerNotification",
        "RecurringPayment.PaymentFailed.StoreOwnerNotification"
    ];

    private Mock<ICacheBase> _cache;
    private MigrationRecurringPaymentTemplates _migration;
    private IServiceProvider _serviceProvider;
    private LiteDBRepositoryMock<MessageTemplate> _templates;

    [TestInitialize]
    public void Init()
    {
        _templates = new LiteDBRepositoryMock<MessageTemplate>();
        _serviceProvider = BuildProvider(_templates);
        _migration = new MigrationRecurringPaymentTemplates();
    }

    private IServiceProvider BuildProvider(IRepository<MessageTemplate> templates)
    {
        var emailAccounts = new LiteDBRepositoryMock<EmailAccount>();
        emailAccounts.Insert(new EmailAccount { Email = "shop@example.com" });
        _cache = new Mock<ICacheBase>();

        var services = new ServiceCollection();
        services.AddSingleton(templates);
        services.AddSingleton<IRepository<EmailAccount>>(emailAccounts);
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton(_cache.Object);
        services.AddSingleton<ILogger<MigrationRecurringPaymentTemplates>>(
            NullLogger<MigrationRecurringPaymentTemplates>.Instance);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Migration_InsertsTheMissingTemplates_LeavesExistingOnes()
    {
        _templates.Insert(new MessageTemplate { Name = Edited, Subject = "mine" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        CollectionAssert.AreEquivalent(Names, _templates.Table.Select(x => x.Name).ToArray());
        Assert.AreEqual("mine", _templates.Table.Single(x => x.Name == Edited).Subject);
        Assert.IsTrue(_templates.Table.Where(x => x.Name != Edited)
            .All(x => x.IsActive && !string.IsNullOrEmpty(x.EmailAccountId)));
        _cache.Verify(x => x.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY, It.IsAny<bool>()),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Migration_RepositoryFails_ReturnsFalseWithoutThrowing()
    {
        var failing = new Mock<IRepository<MessageTemplate>>();
        failing.Setup(x => x.Table).Throws(new InvalidOperationException("db down"));
        var provider = BuildProvider(failing.Object);

        Assert.IsFalse(await _migration.UpgradeProcess(provider));
    }

    [TestMethod]
    [DataRow("RecurringPayment.Cancelled.CustomerNotification")]
    [DataRow("RecurringPayment.Cancelled.StoreOwnerNotification")]
    [DataRow("RecurringPayment.PaymentFailed.CustomerNotification")]
    [DataRow("RecurringPayment.PaymentFailed.StoreOwnerNotification")]
    public void Template_UsesTheSubjectAndTokensOfTheInstallerSeed(string name)
    {
        var seed = MessageTemplateSeed.Build("ea").Single(x => x.Name == name);
        var template = MigrationRecurringPaymentTemplates.Templates("ea").Single(x => x.Name == name);

        Assert.AreEqual(seed.Subject, template.Subject);
        CollectionAssert.AreEqual(MessageTemplateSeedTests.Tokens(seed.Body).ToList(),
            MessageTemplateSeedTests.Tokens(template.Body).ToList());
        Assert.AreEqual(Normalize(seed.Body), Normalize(template.Body));
    }

    [TestMethod]
    public void Templates_UseOnlyTokensTheDropsHave()
    {
        var properties = typeof(LiquidRecurringPayment).GetProperties().Select(x => x.Name).ToHashSet();
        var used = MigrationRecurringPaymentTemplates.Templates("ea")
            .SelectMany(x => MessageTemplateSeedTests.Tokens(x.Subject + x.Body))
            .Where(x => x.StartsWith("RecurringPayment."))
            .Select(x => x["RecurringPayment.".Length..])
            .ToList();

        Assert.IsTrue(used.Count > 0);
        foreach (var token in used)
            Assert.IsTrue(properties.Contains(token), $"{token} is not a property of LiquidRecurringPayment");
    }

    private static string Normalize(string body)
    {
        return Regex.Replace(body, @"\s+", " ");
    }
}
