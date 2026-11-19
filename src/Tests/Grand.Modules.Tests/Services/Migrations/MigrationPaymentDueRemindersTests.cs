using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Configuration;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Caching;
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
public class MigrationPaymentDueRemindersTests
{
    private static readonly string[] TemplateNames =
        ["OrderPaymentDue.CustomerNotification", "OrderPaymentOverdue.CustomerNotification"];

    private readonly MigrationPaymentDueReminders _migration = new();
    private readonly LiteDBRepositoryMock<ScheduleTask> _tasks = new();
    private readonly LiteDBRepositoryMock<MessageTemplate> _templates = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MessageTemplate>>(_templates);
        services.AddSingleton<IRepository<EmailAccount>>(new LiteDBRepositoryMock<EmailAccount>());
        services.AddSingleton<IRepository<Setting>>(new LiteDBRepositoryMock<Setting>());
        services.AddSingleton<IRepository<ScheduleTask>>(_tasks);
        services.AddSingleton<IRepository<Order>>(new LiteDBRepositoryMock<Order>());
        services.AddSingleton(new Mock<IDatabaseContext>().Object);
        services.AddSingleton(new Mock<ICacheBase>().Object);
        services.AddSingleton<ILogger<MigrationPaymentDueReminders>>(
            NullLogger<MigrationPaymentDueReminders>.Instance);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Upgrade_RunTwice_OneDisabledTaskTwoTemplates()
    {
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));
        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var task = _tasks.Table.Single(x => x.ScheduleTaskName == "Payment due reminders");
        Assert.IsFalse(task.Enabled);
        Assert.AreEqual(1440, task.TimeInterval);
        foreach (var name in TemplateNames)
            Assert.AreEqual(1, _templates.Table.Count(x => x.Name == name));
    }

    [TestMethod]
    public async Task Upgrade_ExistingTemplate_Untouched()
    {
        _templates.Insert(new MessageTemplate { Name = TemplateNames[0], Subject = "Our subject" });

        await _migration.UpgradeProcess(_serviceProvider);

        Assert.AreEqual("Our subject", _templates.Table.Single(x => x.Name == TemplateNames[0]).Subject);
    }

    [TestMethod]
    public void Templates_EqualInstallerTemplates()
    {
        var seeded = MessageTemplateSeed.Build("ea");
        foreach (var migrated in MigrationPaymentDueReminders.Templates("ea"))
        {
            var seed = seeded.Single(x => x.Name == migrated.Name);
            Assert.AreEqual(seed.Subject, migrated.Subject);
            Assert.AreEqual(seed.IsActive, migrated.IsActive);
            CollectionAssert.AreEqual(MessageTemplateSeedTests.Tokens(seed.Body).ToList(),
                MessageTemplateSeedTests.Tokens(migrated.Body).ToList());
            Assert.AreEqual(Regex.Replace(seed.Body, @"\s+", " "), Regex.Replace(migrated.Body, @"\s+", " "));
        }
    }
}
