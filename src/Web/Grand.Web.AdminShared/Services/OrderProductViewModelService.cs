using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class OrderProductViewModelService(
    IOrderService orderService,
    IProductService productService,
    IProductAttributeService productAttributeService,
    IProductAttributeFormatter productAttributeFormatter,
    ICustomerService customerService,
    ICurrencyService currencyService,
    IStoreService storeService,
    ILanguageService languageService,
    IPricingService pricingService,
    ITaxService taxService,
    IShoppingCartValidator shoppingCartValidator,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService,
    IContextAccessor contextAccessor,
    IMediator mediator)
    : IOrderProductViewModelService
{
    public virtual async Task<OrderModel.AddOrderProductModel> PrepareAddOrderProductModel(Order order)
    {
        var model = new OrderModel.AddOrderProductModel {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            //product types
            AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList()
        };

        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "0" });

        return await Task.FromResult(model);
    }

    public virtual async Task<OrderModel.AddOrderProductModel.ProductDetailsModel> PrepareAddProductToOrderModel(
        Order order, string productId)
    {
        var product = await productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var customer = await customerService.GetCustomerById(order.CustomerId);
        var currency = await currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        var store = await storeService.GetStoreById(order.StoreId);
        var presetQty = 1;
        var presetPrice = (await pricingService.GetFinalPrice(product, customer, store, currency, 0, true, presetQty))
            .finalPrice;
        var productPrice = await taxService.GetProductPrice(product, presetPrice, true, customer, store);
        var presetPriceInclTax = productPrice.productprice;
        var presetPriceExclTax =
            (await taxService.GetProductPrice(product, presetPrice, false, customer, store)).productprice;

        var model = new OrderModel.AddOrderProductModel.ProductDetailsModel {
            ProductId = product.Id,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Name = product.Name,
            ProductType = product.ProductTypeId,
            UnitPriceExclTax = presetPriceExclTax,
            UnitPriceInclTax = presetPriceInclTax,
            Quantity = presetQty,
            TaxRate = productPrice.taxRate
        };

        //attributes
        var attributes = product.ProductAttributeMappings;
        foreach (var attribute in attributes)
        {
            var productAttribute =
                await productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
            var attributeModel = new OrderModel.AddOrderProductModel.ProductAttributeModel {
                Id = attribute.Id,
                ProductAttributeId = attribute.ProductAttributeId,
                Name = productAttribute.Name,
                TextPrompt = attribute.TextPrompt,
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlTypeId
            };

            if (attribute.ShouldHaveValues())
            {
                //values
                var attributeValues = attribute.ProductAttributeValues;
                foreach (var attributeValue in attributeValues)
                {
                    var attributeValueModel = new OrderModel.AddOrderProductModel.ProductAttributeValueModel {
                        Id = attributeValue.Id,
                        Name = attributeValue.Name,
                        IsPreSelected = attributeValue.IsPreSelected
                    };
                    attributeModel.Values.Add(attributeValueModel);
                }
            }

            model.ProductAttributes.Add(attributeModel);
        }

        //gift voucher
        model.GiftVoucher.IsGiftVoucher = product.IsGiftVoucher;
        if (model.GiftVoucher.IsGiftVoucher) model.GiftVoucher.GiftVoucherType = product.GiftVoucherTypeId;

        return model;
    }

    public virtual async Task<IList<string>> AddProductToOrderDetails(AddProductToOrderModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        var product = await productService.GetProductById(model.ProductId);
        var customer = await customerService.GetCustomerById(order.CustomerId);

        var warnings = new List<string>();
        var customattributes = new List<CustomAttribute>();

        #region Product attributes

        var attributes = product.ProductAttributeMappings;
        foreach (var attribute in attributes)
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                    {
                        var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                            customattributes = ProductExtensions.AddProductAttribute(customattributes,
                                attribute, ctrlAttributes).ToList();
                    }
                    break;
                case AttributeControlType.Checkboxes:
                    {
                        var cblAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(cblAttributes))
                            foreach (var item in cblAttributes
                                         .Split([','], StringSplitOptions.RemoveEmptyEntries))
                                customattributes = ProductExtensions.AddProductAttribute(
                                    customattributes,
                                    attribute, item).ToList();
                    }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                    {
                        //load read-only (already server-side selected) values
                        var attributeValues = attribute.ProductAttributeValues;
                        foreach (var selectedAttributeId in attributeValues
                                     .Where(v => v.IsPreSelected)
                                     .Select(v => v.Id)
                                     .ToList())
                            customattributes = ProductExtensions.AddProductAttribute(customattributes,
                                attribute, selectedAttributeId).ToList();
                    }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.FileUpload:
                    {
                        var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                        {
                            var enteredText = ctrlAttributes.Trim();
                            customattributes = ProductExtensions.AddProductAttribute(customattributes, attribute, enteredText).ToList();
                        }
                    }
                    break;
            }

        //validate conditional attributes (if specified)
        foreach (var attribute in attributes)
        {
            var conditionMet = product.IsConditionMet(attribute, customattributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customattributes = ProductExtensions
                    .RemoveProductAttribute(customattributes, attribute).ToList();
        }

        #endregion

        #region Gift vouchers

        var recipientName = model.Giftvoucher?.RecipientName;
        var recipientEmail = model.Giftvoucher?.RecipientEmail;
        var senderName = model.Giftvoucher?.SenderName;
        var senderEmail = model.Giftvoucher?.SenderEmail;
        var giftVoucherMessage = model.Giftvoucher?.Message;
        if (product.IsGiftVoucher)
            customattributes = GiftVoucherExtensions.AddGiftVoucherAttribute(customattributes,
                recipientName, recipientEmail, senderName, senderEmail, giftVoucherMessage).ToList();

        #endregion

        //warnings
        var shoppingCartItem = new ShoppingCartItem {
            ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
            Quantity = model.Quantity,
            WarehouseId = product.WarehouseId,
            Attributes = customattributes
        };

        warnings.AddRange(
            await shoppingCartValidator.GetShoppingCartItemAttributeWarnings(customer, product, shoppingCartItem,
                contextAccessor.WorkContext.WorkingLanguage));
        warnings.AddRange(
            await shoppingCartValidator.GetShoppingCartItemGiftVoucherWarnings(customer, product,
                shoppingCartItem, contextAccessor.WorkContext.WorkingLanguage));
        if (warnings.Count == 0)
        {
            //the description is stored on the order, so it is rendered for the order, not for the admin
            var store = await storeService.GetStoreById(order.StoreId) ?? contextAccessor.StoreContext.CurrentStore;
            var currency = await currencyService.GetCurrencyByCode(order.CustomerCurrencyCode) ??
                           contextAccessor.WorkContext.WorkingCurrency;
            var language = await languageService.GetLanguageById(order.CustomerLanguageId) ??
                           contextAccessor.WorkContext.WorkingLanguage;
            var host = store.Domains.FirstOrDefault(x => x.Primary) ?? store.Domains.FirstOrDefault();
            var attributeDescription =
                await productAttributeFormatter.FormatAttributes(product, customattributes,
                    new CalculationContext(customer, store, currency, order.CustomerTaxDisplayTypeId),
                    language, host);

            //save item
            var orderItem = new OrderItem {
                OrderItemGuid = Guid.NewGuid(),
                ProductId = product.Id,
                VendorId = product.VendorId,
                WarehouseId = product.WarehouseId,
                Sku = product.FormatSku(customattributes),
                SeId = order.SeId,
                UnitPriceInclTax = model.UnitPriceInclTax,
                UnitPriceExclTax = model.UnitPriceExclTax,
                PriceInclTax = Math.Round(model.UnitPriceInclTax * model.Quantity, 2),
                PriceExclTax = Math.Round(model.UnitPriceExclTax * model.Quantity, 2),
                TaxRate = model.TaxRate,
                OriginalProductCost = await pricingService.GetProductCost(product, customattributes),
                AttributeDescription = attributeDescription,
                Attributes = customattributes,
                Quantity = model.Quantity,
                OpenQty = model.Quantity,
                DiscountAmountInclTax = 0,
                DiscountAmountExclTax = 0,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = "",
                IsShipEnabled = product.IsShipEnabled,
                CreatedOnUtc = DateTime.UtcNow
            };

            await mediator.Send(new InsertOrderItemCommand { Order = order, OrderItem = orderItem, Product = product });
        }

        return warnings;
    }
}
