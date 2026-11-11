using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.SharedKernel.Extensions;
using Grand.Mediator;

namespace Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;

public class
    SendNotificationsToSubscribersCommandHandler : IRequestHandler<SendNotificationsToSubscribersCommand,
    IList<OutOfStockSubscription>>
{
    private readonly ICustomerService _customerService;
    private readonly ICatalogMessageProviderService _catalogMessageProviderService;
    private readonly IRepository<OutOfStockSubscription> _outOfStockSubscriptionRepository;

    public SendNotificationsToSubscribersCommandHandler(
        ICustomerService customerService,
        ICatalogMessageProviderService catalogMessageProviderService,
        IRepository<OutOfStockSubscription> outOfStockSubscriptionRepository)
    {
        _customerService = customerService;
        _catalogMessageProviderService = catalogMessageProviderService;
        _outOfStockSubscriptionRepository = outOfStockSubscriptionRepository;
    }

    public async Task<IList<OutOfStockSubscription>> Handle(SendNotificationsToSubscribersCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Product);

        var subscriptions =
            await GetAllSubscriptionsByProductId(request.Product, request.Attributes, request.Warehouse);

        foreach (var subscription in subscriptions)
        {
            var customer = await _customerService.GetCustomerById(subscription.CustomerId);
            //ensure that customer is registered (simple and fast way)
            if (customer == null || !CommonHelper.IsValidEmail(customer.Email)) continue;
            var customerLanguageId =
                customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, subscription.StoreId);
            await _catalogMessageProviderService.SendBackInStockMessage(customer, request.Product, subscription,
                customerLanguageId);
        }

        return subscriptions;
    }

    /// <summary>
    ///     Gets all subscriptions
    /// </summary>
    /// <param name="product">Product whose subscriptions are being selected</param>
    /// <param name="attributes">Combination attributes, or null for product-level subscriptions</param>
    /// <param name="warehouseId">Warehouse identifier. An empty value selects only global subscriptions
    /// for multi-warehouse products; for single-warehouse products it retains the legacy all-records behavior.</param>
    /// <returns>Subscriptions</returns>
    private async Task<IList<OutOfStockSubscription>> GetAllSubscriptionsByProductId(Product product,
        IList<CustomAttribute> attributes, string warehouseId)
    {
        var query = _outOfStockSubscriptionRepository.Table
            .Where(biss => biss.ProductId == product.Id)
            .OrderByDescending(biss => biss.CreatedOnUtc).ToList();

        // A warehouse-specific event targets only that warehouse. An aggregate event
        // on a multi-warehouse product targets only global subscribers; subscribers
        // assigned to individual warehouses wait for stock in their own warehouse.
        if (!string.IsNullOrEmpty(warehouseId))
            query = query.Where(biss => biss.WarehouseId == warehouseId).ToList();
        else if (product.UseMultipleWarehouses)
            query = query.Where(biss => string.IsNullOrEmpty(biss.WarehouseId)).ToList();

        //attributes
        if (attributes == null || !attributes.Any()) return await Task.FromResult(query.ToList());
        foreach (var item in attributes)
            query = query.Where(x => x.Attributes.Any(y => y.Key == item.Key && y.Value == item.Value)).ToList();

        return await Task.FromResult(query.ToList());
    }
}