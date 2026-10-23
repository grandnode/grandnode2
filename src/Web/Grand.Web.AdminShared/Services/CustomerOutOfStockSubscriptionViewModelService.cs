using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Extensions;

namespace Grand.Web.AdminShared.Services;

public class CustomerOutOfStockSubscriptionViewModelService(
    IOutOfStockSubscriptionService outOfStockSubscriptionService,
    IStoreService storeService,
    IProductService productService,
    IProductAttributeFormatter productAttributeFormatter,
    IDateTimeService dateTimeService,
    IContextAccessor contextAccessor)
    : ICustomerOutOfStockSubscriptionViewModelService
{
    public virtual async
        Task<(IEnumerable<CustomerModel.OutOfStockSubscriptionModel> outOfStockSubscriptionModels, int totalCount)>
        PrepareOutOfStockSubscriptionModel(string customerId, int pageIndex, int pageSize)
    {
        var subscriptions =
            await outOfStockSubscriptionService.GetAllSubscriptionsByCustomerId(customerId, "", pageIndex - 1,
                pageSize);
        var items = new List<CustomerModel.OutOfStockSubscriptionModel>();
        if (subscriptions.Any())
        {
            foreach (var x in subscriptions)
            {
                var store = await storeService.GetStoreById(x.StoreId);
                var product = await productService.GetProductById(x.ProductId);
                var m = new CustomerModel.OutOfStockSubscriptionModel {
                    Id = x.Id,
                    StoreName = store != null ? store.Shortcut : "Unknown",
                    ProductId = x.ProductId,
                    ProductName = product != null ? product.Name : "Unknown",
                    AttributeDescription = x.Attributes == null || !x.Attributes.Any()
                        ? ""
                        : await productAttributeFormatter.FormatAttributes(product, x.Attributes,
                            contextAccessor.CalculationContext(), contextAccessor.WorkContext.WorkingLanguage,
                            contextAccessor.StoreContext.CurrentHost),
                    CreatedOn = dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                };
                items.Add(m);
            }
        }

        return (items, subscriptions.TotalCount);
    }
}
