using Grand.Data;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Module.Migration.Migrations._2._5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationMerchandiseReturnReasonIdsTests
{
    private readonly LiteDBRepositoryMock<MerchandiseReturnAction> _actions = new();
    private readonly MigrationMerchandiseReturnReasonIds _migration = new();
    private readonly LiteDBRepositoryMock<MerchandiseReturnReason> _reasons = new();
    private readonly LiteDBRepositoryMock<MerchandiseReturn> _returns = new();
    private IServiceProvider _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<MerchandiseReturn>>(_returns);
        services.AddSingleton<IRepository<MerchandiseReturnReason>>(_reasons);
        services.AddSingleton<IRepository<MerchandiseReturnAction>>(_actions);
        services.AddSingleton<ILogger<MigrationMerchandiseReturnReasonIds>>(
            NullLogger<MigrationMerchandiseReturnReasonIds>.Instance);
        _serviceProvider = services.BuildServiceProvider();

        _reasons.Insert(new MerchandiseReturnReason {
            Id = "broken", Name = "Broken",
            Locales = [new TranslationEntity { LanguageId = "pl", LocaleKey = "Name", LocaleValue = "Uszkodzony" }]
        });
        _reasons.Insert(new MerchandiseReturnReason { Id = "wrong", Name = "Wrong item" });
        _actions.Insert(new MerchandiseReturnAction { Id = "refund", Name = "Refund" });
    }

    private MerchandiseReturn Insert(params MerchandiseReturnItem[] items)
    {
        var merchandiseReturn = new MerchandiseReturn();
        foreach (var item in items)
            merchandiseReturn.MerchandiseReturnItems.Add(item);
        _returns.Insert(merchandiseReturn);
        return merchandiseReturn;
    }

    private IList<MerchandiseReturnItem> Items(MerchandiseReturn merchandiseReturn)
    {
        return _returns.Table.First(x => x.Id == merchandiseReturn.Id).MerchandiseReturnItems;
    }

    [TestMethod]
    public async Task Backfill_MatchesNameOrAnyTranslation_CaseInsensitive()
    {
        var merchandiseReturn = Insert(
            new MerchandiseReturnItem { ReasonForReturn = "Broken", RequestedAction = "Refund" },
            new MerchandiseReturnItem { ReasonForReturn = "USZKODZONY", RequestedAction = "REFUND" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var items = Items(merchandiseReturn);
        CollectionAssert.AreEqual(new[] { "broken", "broken" }, items.Select(x => x.ReasonId).ToArray());
        CollectionAssert.AreEqual(new[] { "refund", "refund" }, items.Select(x => x.ActionId).ToArray());
        //the snapshot in the customer's language stays as it was
        Assert.AreEqual("USZKODZONY", items[1].ReasonForReturn);
    }

    [TestMethod]
    public async Task Backfill_UnknownName_LeavesIdEmpty()
    {
        var merchandiseReturn = Insert(
            new MerchandiseReturnItem { ReasonForReturn = "not available", RequestedAction = "Refund", Quantity = 2 });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var item = Items(merchandiseReturn).Single();
        Assert.IsNull(item.ReasonId);
        Assert.AreEqual("refund", item.ActionId);
        Assert.AreEqual(2, item.Quantity);
    }

    [TestMethod]
    public async Task Backfill_AmbiguousName_LeavesIdEmpty()
    {
        //another reason is translated with the same text: the snapshot cannot tell which one the customer chose
        _reasons.Insert(new MerchandiseReturnReason {
            Id = "damaged", Name = "Damaged",
            Locales = [new TranslationEntity { LanguageId = "pl", LocaleKey = "Name", LocaleValue = "Uszkodzony" }]
        });
        var merchandiseReturn = Insert(new MerchandiseReturnItem { ReasonForReturn = "Uszkodzony" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsNull(Items(merchandiseReturn).Single().ReasonId);
    }

    [TestMethod]
    public async Task Backfill_MoreReturnsThanAPage_FillsEvery()
    {
        //read a page at a time; a return filled in leaves the filter without the next page skipping any
        var returns = Enumerable.Range(0, 501)
            .Select(_ => Insert(new MerchandiseReturnItem { ReasonForReturn = "Broken", RequestedAction = "Refund" }))
            .ToList();

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        Assert.IsTrue(returns.All(x => Items(x).Single().ReasonId == "broken"));
    }

    [TestMethod]
    public async Task Backfill_ItemWithIdAlready_IsNotTouched()
    {
        var merchandiseReturn = Insert(
            new MerchandiseReturnItem { ReasonId = "wrong", ReasonForReturn = "Broken", RequestedAction = "Refund" });

        Assert.IsTrue(await _migration.UpgradeProcess(_serviceProvider));

        var item = Items(merchandiseReturn).Single();
        Assert.AreEqual("wrong", item.ReasonId);
        Assert.AreEqual("refund", item.ActionId);
    }
}
