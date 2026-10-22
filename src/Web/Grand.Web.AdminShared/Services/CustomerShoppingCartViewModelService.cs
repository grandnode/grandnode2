using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.ShoppingCart;
using Grand.Web.Common.Extensions;

namespace Grand.Web.AdminShared.Services;

public class CustomerShoppingCartViewModelService(
    ICustomerService customerService,
    IStoreService storeService,
    IProductService productService,
    IDateTimeService dateTimeService,
    IContextAccessor contextAccessor,
    ITaxService taxService,
    IPricingService pricingService,
    IPriceFormatter priceFormatter,
    IProductAttributeFormatter productAttributeFormatter,
    IShoppingCartService shoppingCartService)
    : ICustomerShoppingCartViewModelService
{
    public virtual async Task<IList<ShoppingCartItemModel>> PrepareShoppingCartItemModel(string customerId,
        int cartTypeId)
    {
        var customer = await customerService.GetCustomerById(customerId);
        var cart = customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == (ShoppingCartType)cartTypeId).ToList();
        var items = new List<ShoppingCartItemModel>();
        if (cart.Any())
        {
            foreach (var sci in cart)
            {
                var store = await storeService.GetStoreById(sci.StoreId);
                var priceStore = store ?? contextAccessor.StoreContext.CurrentStore;
                var product = await productService.GetProductById(sci.ProductId);
                if (product != null)
                {
                    var price = (await taxService.GetProductPrice(product,
                        (await pricingService.GetUnitPrice(sci, product,
                            customer, priceStore,
                            contextAccessor.WorkContext.WorkingCurrency)).unitprice,
                            contextAccessor.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax,
                            customer, priceStore)).productprice;
                    var sciModel = new ShoppingCartItemModel {
                        Id = sci.Id,
                        Store = store != null ? store.Shortcut : "Unknown",
                        ProductId = sci.ProductId,
                        Quantity = sci.Quantity,
                        ProductName = product.Name,
                        AttributeInfo = await productAttributeFormatter.FormatAttributes(product, sci.Attributes,
                            contextAccessor.CalculationContext() with { Customer = customer, Store = priceStore },
                            contextAccessor.WorkContext.WorkingLanguage, contextAccessor.StoreContext.CurrentHost),
                        UnitPrice = priceFormatter.FormatPrice(price, contextAccessor.WorkContext.WorkingCurrency),
                        UnitPriceValue = price,
                        Total = priceFormatter.FormatPrice((await taxService.GetProductPrice(product,
                            (await pricingService.GetSubTotal(sci, product,
                                customer, priceStore,
                                contextAccessor.WorkContext.WorkingCurrency)).subTotal,
                                contextAccessor.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax,
                                customer, priceStore)).productprice,
                                contextAccessor.WorkContext.WorkingCurrency),
                        UpdatedOn = sci.UpdatedOnUtc.HasValue
                            ? dateTimeService.ConvertToUserTime(sci.UpdatedOnUtc.Value, DateTimeKind.Utc)
                            : dateTimeService.ConvertToUserTime(sci.CreatedOnUtc, DateTimeKind.Utc)
                    };
                    items.Add(sciModel);
                }
            }
        }

        return items;
    }

    public virtual async Task DeleteCart(Customer customer, string id)
    {
        var cart = customer.ShoppingCartItems.FirstOrDefault(a => a.Id == id);
        if (cart != null)
        {
            await shoppingCartService.DeleteShoppingCartItem(customer, cart, ensureOnlyActiveCheckoutAttributes: true);
            await customerService.UpdateCustomerInAdminPanel(customer);
        }
    }

    public virtual async Task<IList<string>> UpdateCart(Customer customer, string shoppingCartId, double? unitprice)
    {
        var cart = customer.ShoppingCartItems.FirstOrDefault(a => a.Id == shoppingCartId);
        if (cart != null)
            return await shoppingCartService.UpdateShoppingCartItem(
                customer, contextAccessor.WorkContext.WorkingLanguage,
                shoppingCartId,
                cart.WarehouseId,
                cart.Attributes,
                unitprice,
                cart.RentalStartDateUtc,
                cart.RentalEndDateUtc,
                cart.Quantity,
                true,
                cart.ReservationId,
                shoppingCartId);
        return new List<string>();
    }
}
