using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class SendNotificationsToSubscribersCommandHandlerTests
{
    private static OutOfStockSubscription Subscription(string id, string warehouse)
    {
        var subscription = new OutOfStockSubscription {
            Id = id, ProductId = "p1", CustomerId = id, WarehouseId = warehouse
        };
        subscription.Attributes.Add(new CustomAttribute { Key = "color", Value = "blue" });
        return subscription;
    }

    private static SendNotificationsToSubscribersCommandHandler Handler(params OutOfStockSubscription[] subscriptions)
    {
        var repository = new Mock<IRepository<OutOfStockSubscription>>();
        repository.Setup(r => r.Table).Returns(subscriptions.AsQueryable());
        return new SendNotificationsToSubscribersCommandHandler(
            new Mock<ICustomerService>().Object,
            new Mock<ICatalogMessageProviderService>().Object,
            repository.Object);
    }

    private static SendNotificationsToSubscribersCommand Command(bool multiple, string warehouse) => new() {
        Product = new Product { Id = "p1", UseMultipleWarehouses = multiple },
        Warehouse = warehouse,
        Attributes = [new CustomAttribute { Key = "color", Value = "blue" }]
    };

    [TestMethod]
    public async Task MultipleWarehouses_AggregateNotificationDoesNotSelectUnavailableWarehouseSubscribers()
    {
        var handler = Handler(
            Subscription("global", ""),
            Subscription("available", "w1"),
            Subscription("unavailable", "w2"));

        var notifications = await handler.Handle(Command(true, ""), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "global" }, notifications.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task MultipleWarehouses_AggregateNotificationDoesNotSendEmailForUnavailableWarehouse()
    {
        var subscriptions = new[] {
            Subscription("global", ""),
            Subscription("still-out-of-stock", "w2")
        };
        var repository = new Mock<IRepository<OutOfStockSubscription>>();
        repository.Setup(r => r.Table).Returns(subscriptions.AsQueryable());
        var customers = new Mock<ICustomerService>();
        customers.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Customer { Id = id, Email = id + "@example.com" });
        var messages = new Mock<ICatalogMessageProviderService>();
        var sentSubscriptionIds = new List<string>();
        messages.Setup(m => m.SendBackInStockMessage(
                It.IsAny<Customer>(), It.IsAny<Product>(),
                It.IsAny<OutOfStockSubscription>(), It.IsAny<string>()))
            .Callback<Customer, Product, OutOfStockSubscription, string>((_, _, subscription, _) =>
                sentSubscriptionIds.Add(subscription.Id))
            .ReturnsAsync(1);
        var handler = new SendNotificationsToSubscribersCommandHandler(
            customers.Object, messages.Object, repository.Object);

        var notifications = await handler.Handle(Command(true, ""), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "global" }, notifications.Select(x => x.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "global" }, sentSubscriptionIds.ToArray());
        messages.Verify(m => m.SendBackInStockMessage(
            It.IsAny<Customer>(), It.IsAny<Product>(), It.IsAny<OutOfStockSubscription>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task MultipleWarehouses_ManageStockWithoutCombinations_OnlyNotifiesGlobalSubscribers()
    {
        var handler = Handler(
            Subscription("global", ""),
            Subscription("unassigned-default-warehouse", "w2"));
        var request = Command(true, "");
        request.Product.ManageInventoryMethodId = ManageInventoryMethod.ManageStock;
        request.Attributes = null;

        var notifications = await handler.Handle(request, CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "global" }, notifications.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task MultipleWarehouses_SpecificNotificationOnlySelectsItsWarehouse()
    {
        var handler = Handler(
            Subscription("global", ""),
            Subscription("available", "w1"),
            Subscription("unavailable", "w2"));

        var notifications = await handler.Handle(Command(true, "w1"), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "available" }, notifications.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task SingleWarehouse_EmptyWarehousePreservesExistingAllWarehouseBehavior()
    {
        var handler = Handler(
            Subscription("global", ""),
            Subscription("legacy", "w1"));

        var notifications = await handler.Handle(Command(false, ""), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "global", "legacy" }, notifications.Select(x => x.Id).ToArray());
    }
}
