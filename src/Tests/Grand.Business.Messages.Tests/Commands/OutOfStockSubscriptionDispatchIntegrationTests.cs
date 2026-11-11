using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Mediator;
using Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

/// <summary>
/// Exercises the real subscription service, handler and MongoDB repository together.
/// A global availability transition must never consume a subscription for an unavailable
/// warehouse, including a store's default warehouse not yet assigned to the product.
/// </summary>
[TestClass]
public class OutOfStockSubscriptionDispatchIntegrationTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AggregateRestock_LeavesUnavailableWarehouseSubscriptionUntilItsOwnRestock(bool withCombinations)
    {
        var repository = new MongoDBRepositoryTest<OutOfStockSubscription>();
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Customer { Id = id, Email = id + "@example.com" });

        var sent = new List<string>();
        var messages = new Mock<ICatalogMessageProviderService>();
        messages.Setup(x => x.SendBackInStockMessage(
                It.IsAny<Customer>(), It.IsAny<Product>(),
                It.IsAny<OutOfStockSubscription>(), It.IsAny<string>()))
            .Callback<Customer, Product, OutOfStockSubscription, string>((_, _, s, _) => sent.Add(s.Id))
            .ReturnsAsync(1);

        var handler = new SendNotificationsToSubscribersCommandHandler(
            customers.Object, messages.Object, repository);
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(
                It.IsAny<SendNotificationsToSubscribersCommand>(), It.IsAny<CancellationToken>()))
            .Returns((SendNotificationsToSubscribersCommand command, CancellationToken token) =>
                handler.Handle(command, token));

        // The real service deletes precisely the subscriptions returned by the real handler.
        var service = new OutOfStockSubscriptionService(repository, mediator.Object);
        var product = new Product {
            Id = "product", UseMultipleWarehouses = true,
            ManageInventoryMethodId = withCombinations
                ? ManageInventoryMethod.ManageStockByAttributes
                : ManageInventoryMethod.ManageStock
        };
        var attributes = withCombinations
            ? new List<CustomAttribute> { new() { Key = "color", Value = "blue" } }
            : null;

        // Only w1 is assigned and stocked. The store's default w2 has no product inventory.
        if (withCombinations)
        {
            var combination = new ProductAttributeCombination { Attributes = attributes };
            combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory {
                WarehouseId = "w1", StockQuantity = 3
            });
            product.ProductAttributeCombinations.Add(combination);
        }
        else
            product.ProductWarehouseInventory.Add(new ProductWarehouseInventory {
                WarehouseId = "w1", StockQuantity = 3
            });

        OutOfStockSubscription NewSubscription(string customerId, string warehouseId)
        {
            var subscription = new OutOfStockSubscription {
                ProductId = product.Id, CustomerId = customerId,
                WarehouseId = warehouseId, StoreId = "store"
            };
            if (attributes != null)
                foreach (var attribute in attributes)
                    subscription.Attributes.Add(new CustomAttribute {
                        Key = attribute.Key, Value = attribute.Value
                    });
            return subscription;
        }

        var global = NewSubscription("global", "");
        var available = NewSubscription("w1-customer", "w1");
        var unavailable = NewSubscription("w2-customer", "w2");
        await service.InsertSubscription(global);
        await service.InsertSubscription(available);
        await service.InsertSubscription(unavailable);

        async Task Dispatch(string warehouse)
        {
            if (withCombinations)
                await service.SendNotificationsToSubscribers(product, attributes, warehouse);
            else
                await service.SendNotificationsToSubscribers(product, warehouse);
        }

        // The aggregate transition must send and delete only the global subscription.
        await Dispatch("");
        Assert.IsNull(await repository.GetByIdAsync(global.Id));
        Assert.IsNotNull(await repository.GetByIdAsync(available.Id));
        Assert.IsNotNull(await repository.GetByIdAsync(unavailable.Id));
        CollectionAssert.AreEquivalent(new[] { global.Id }, sent.ToArray());

        // When w1 is explicitly restocked, w2 must remain queued, with no premature email.
        await Dispatch("w1");
        Assert.IsNull(await repository.GetByIdAsync(available.Id));
        Assert.IsNotNull(await repository.GetByIdAsync(unavailable.Id));
        CollectionAssert.AreEquivalent(new[] { global.Id, available.Id }, sent.ToArray());

        // w2 is notified and removed only when its own warehouse gains stock.
        if (withCombinations)
            product.ProductAttributeCombinations.Single().WarehouseInventory.Add(
                new ProductCombinationWarehouseInventory { WarehouseId = "w2", StockQuantity = 2 });
        else
            product.ProductWarehouseInventory.Add(
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 2 });

        await Dispatch("w2");
        Assert.IsNull(await repository.GetByIdAsync(unavailable.Id));
        CollectionAssert.AreEquivalent(new[] { global.Id, available.Id, unavailable.Id }, sent.ToArray());
    }
}
