using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Features.Handlers.ShoppingCart;

public class GetShoppingCartHandler : IRequestHandler<GetShoppingCart, ShoppingCartModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly ICheckoutAttributeService _checkoutAttributeService;
    private readonly CommonSettings _commonSettings;
    private readonly ICurrencyService _currencyService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IDiscountService _discountService;
    private readonly IDiscountValidationService _discountValidationService;
    private readonly IDownloadService _downloadService;
    private readonly IGroupService _groupService;
    private readonly LinkGenerator _linkGenerator;
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly ITaxService _taxService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IWarehouseService _warehouseService;

    public GetShoppingCartHandler(
        IProductService productService,
        IPictureService pictureService,
        ITranslationService translationService,
        ICheckoutAttributeFormatter checkoutAttributeFormatter,
        ICurrencyService currencyService,
        IDiscountService discountService,
        ICheckoutAttributeService checkoutAttributeService,
        IPermissionService permissionService,
        ITaxService taxService,
        IPriceFormatter priceFormatter,
        ICheckoutAttributeParser checkoutAttributeParser,
        IDownloadService downloadService,
        IWarehouseService warehouseService,
        IProductAttributeFormatter productAttributeFormatter,
        IPricingService priceCalculationService,
        IDateTimeService dateTimeService,
        IVendorService vendorService,
        IGroupService groupService,
        IMediator mediator,
        IShoppingCartValidator shoppingCartValidator,
        IDiscountValidationService discountValidationService,
        LinkGenerator linkGenerator,
        MediaSettings mediaSettings,
        OrderSettings orderSettings,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings,
        CommonSettings commonSettings)
    {
        _productService = productService;
        _pictureService = pictureService;
        _translationService = translationService;
        _checkoutAttributeFormatter = checkoutAttributeFormatter;
        _currencyService = currencyService;
        _discountService = discountService;
        _checkoutAttributeService = checkoutAttributeService;
        _permissionService = permissionService;
        _taxService = taxService;
        _priceFormatter = priceFormatter;
        _checkoutAttributeParser = checkoutAttributeParser;
        _downloadService = downloadService;
        _warehouseService = warehouseService;
        _productAttributeFormatter = productAttributeFormatter;
        _pricingService = priceCalculationService;
        _dateTimeService = dateTimeService;
        _vendorService = vendorService;
        _groupService = groupService;
        _mediator = mediator;
        _shoppingCartValidator = shoppingCartValidator;
        _discountValidationService = discountValidationService;
        _linkGenerator = linkGenerator;
        _mediaSettings = mediaSettings;
        _orderSettings = orderSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
        _commonSettings = commonSettings;
    }

    public async Task<ShoppingCartModel> Handle(GetShoppingCart request, CancellationToken cancellationToken)
    {
        var model = new ShoppingCartModel();

        if (!request.Cart.Any())
            return model;

        await PrepareSimpleProperties(model, request);

        await PrepareCheckoutAttributes(model, request);

        await PrepareCartItems(model, request);

        return model;
    }

    private async Task PrepareSimpleProperties(ShoppingCartModel model, GetShoppingCart request)
    {
        #region Simple properties

        model.IsEditable = request.IsEditable;
        model.IsAllowOnHold = _shoppingCartSettings.AllowOnHoldCart;
        model.TermsOfServicePopup = _commonSettings.PopupForTermsOfServiceLinks;
        model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
        model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;
        model.IsGuest = await _groupService.IsGuest(request.Customer);
        model.ShowCheckoutAsGuestButton = model.IsGuest && _orderSettings.AnonymousCheckoutAllowed;
        var checkoutAttributes =
            request.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes,
                request.Store.Id);
        model.CheckoutAttributeInfo =
            await _checkoutAttributeFormatter.FormatAttributes(checkoutAttributes, request.Customer);
        if (!request.Cart.Where(x => x.ShoppingCartTypeId is ShoppingCartType.ShoppingCart or ShoppingCartType.Auctions)
                .ToList().Any())
        {
            model.MinOrderSubtotalWarning = _translationService.GetResource("Checkout.MinOrderOneProduct");
        }
        else
        {
            var minOrderSubtotalAmountOk = await _mediator.Send(new ValidateMinShoppingCartSubtotalAmountCommand {
                Customer = request.Customer,
                Cart = request.Cart.Where(x =>
                    x.ShoppingCartTypeId is ShoppingCartType.ShoppingCart or ShoppingCartType.Auctions).ToList()
            });
            if (!minOrderSubtotalAmountOk)
            {
                var minOrderSubtotalAmount =
                    await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount,
                        request.Currency);
                model.MinOrderSubtotalWarning =
                    string.Format(_translationService.GetResource("Checkout.MinOrderSubtotalAmount"),
                        _priceFormatter.FormatPrice(minOrderSubtotalAmount, request.Currency));
            }
        }

        model.TermsOfServiceOnShoppingCartPage = _orderSettings.TermsOfServiceOnShoppingCartPage;
        model.TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage;

        model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
        var discountCouponCodes = request.Customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
        foreach (var couponCode in discountCouponCodes)
        {
            var discount = await _discountService.GetDiscountByCouponCode(couponCode);
            if (discount is { RequiresCouponCode: true } &&
                (await _discountValidationService.ValidateDiscount(discount, request.Customer, request.Store,
                    request.Currency)).IsValid)
                model.DiscountBox.AppliedDiscountsWithCodes.Add(new ShoppingCartModel.DiscountBoxModel.DiscountInfoModel {
                    Id = discount.Id,
                    CouponCode = couponCode
                });
        }

        model.GiftVoucherBox.Display = _shoppingCartSettings.ShowGiftVoucherBox;

        //cart warnings
        var cartWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(request.Cart, checkoutAttributes,
            request.ValidateCheckoutAttributes, true);
        foreach (var warning in cartWarnings)
            model.Warnings.Add(warning);

        #endregion
    }

    private async Task PrepareCheckoutAttributes(ShoppingCartModel model, GetShoppingCart request)
    {
        #region Checkout attributes

        var checkoutAttributes =
            await _checkoutAttributeService.GetAllCheckoutAttributes(request.Store.Id,
                !request.Cart.RequiresShipping());
        foreach (var attribute in checkoutAttributes)
        {
            var attributeModel = new ShoppingCartModel.CheckoutAttributeModel {
                Id = attribute.Id,
                Name = attribute.GetTranslation(x => x.Name, request.Language.Id),
                TextPrompt = attribute.GetTranslation(x => x.TextPrompt, request.Language.Id),
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlTypeId,
                DefaultValue = attribute.DefaultValue
            };
            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

            if (attribute.ShouldHaveValues())
            {
                //values
                var attributeValues = attribute.CheckoutAttributeValues;
                foreach (var attributeValue in attributeValues)
                {
                    var attributeValueModel = new ShoppingCartModel.CheckoutAttributeValueModel {
                        Id = attributeValue.Id,
                        Name = attributeValue.GetTranslation(x => x.Name, request.Language.Id),
                        ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                        IsPreSelected = attributeValue.IsPreSelected
                    };
                    attributeModel.Values.Add(attributeValueModel);

                    //display price if allowed
                    if (!await _permissionService.Authorize(StandardPermission.DisplayPrices)) continue;
                    var priceAdjustmentBase = (await _taxService.GetCheckoutAttributePrice(attribute, attributeValue))
                        .checkoutPrice;
                    var priceAdjustment =
                        await _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, request.Currency);
                    switch (priceAdjustmentBase)
                    {
                        case > 0:
                            attributeValueModel.PriceAdjustment = "+" + _priceFormatter.FormatPrice(priceAdjustment);
                            break;
                        case < 0:
                            attributeValueModel.PriceAdjustment = "-" + _priceFormatter.FormatPrice(-priceAdjustment);
                            break;
                    }
                }
            }

            //set already selected attributes
            var selectedCheckoutAttributes =
                request.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(
                    SystemCustomerFieldNames.CheckoutAttributes, request.Store.Id);
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.Checkboxes:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                    {
                        //clear default selection
                        foreach (var item in attributeModel.Values)
                            item.IsPreSelected = false;

                        //select new values
                        var selectedValues =
                            await _checkoutAttributeParser.ParseCheckoutAttributeValues(selectedCheckoutAttributes);
                        foreach (var attributeValue in selectedValues)
                            if (attributeModel.Id == attributeValue.CheckoutAttributeId)
                                foreach (var item in attributeModel.Values)
                                    if (attributeValue.Id == item.Id)
                                        item.IsPreSelected = true;
                    }
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    //do nothing
                    //values are already pre-set
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Datepicker:
                {
                    if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                    {
                        var enteredText = selectedCheckoutAttributes.Where(x => x.Key == attribute.Id)
                            .Select(x => x.Value).ToList();
                        if (enteredText.Any())
                            attributeModel.DefaultValue = enteredText[0];
                    }
                }
                    break;
                case AttributeControlType.FileUpload:
                {
                    if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                    {
                        var downloadGuidStr = selectedCheckoutAttributes.Where(x => x.Key == attribute.Id)
                            .Select(x => x.Value).FirstOrDefault();
                        Guid.TryParse(downloadGuidStr, out var downloadGuid);
                        var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                        if (download != null)
                            attributeModel.DefaultValue = download.DownloadGuid.ToString();
                    }
                }
                    break;
            }

            model.CheckoutAttributes.Add(attributeModel);
        }

        #endregion
    }

    private async Task PrepareCartItems(ShoppingCartModel model, GetShoppingCart request)
    {
        #region Cart items

        foreach (var sci in request.Cart)
        {
            var product = await _productService.GetProductById(sci.ProductId);
            if (product == null)
                continue;
            var sename = product.GetSeName(request.Language.Id);
            var cartItemModel = new ShoppingCartModel.ShoppingCartItemModel {
                Id = sci.Id,
                Sku = product.FormatSku(sci.Attributes),
                IsCart = sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart,
                ProductId = product.Id,
                WarehouseId = sci.WarehouseId,
                ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                ProductSeName = sename,
                ProductUrl = _linkGenerator.GetPathByRouteValues("Product", new { SeName = sename }),
                Quantity = sci.Quantity,
                AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes),
                AllowItemEditing = _shoppingCartSettings.AllowCartItemEditing && product.VisibleIndividually
            };

            if (product.RequireOtherProducts)
                cartItemModel.DisableRemoval = product.RequireOtherProducts && product.ParseRequiredProductIds()
                    .Intersect(request.Cart.Select(x => x.ProductId)).Any();

            //warehouse
            if (!string.IsNullOrEmpty(cartItemModel.WarehouseId))
            {
                var warehouse = await _warehouseService.GetWarehouseById(cartItemModel.WarehouseId);
                cartItemModel.WarehouseName = warehouse?.Name;
                cartItemModel.WarehouseCode = warehouse?.Code;
            }

            //vendor
            if (!string.IsNullOrEmpty(product.VendorId))
            {
                var vendor = await _vendorService.GetVendorById(product.VendorId);
                if (vendor != null)
                {
                    cartItemModel.VendorId = product.VendorId;
                    cartItemModel.VendorName = vendor.Name;
                    cartItemModel.VendorSeName = vendor.GetSeName(request.Language.Id);
                }
            }

            //allowed quantities
            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
                cartItemModel.AllowedQuantities.Add(new SelectListItem {
                    Text = qty.ToString(),
                    Value = qty.ToString(),
                    Selected = sci.Quantity == qty
                });

            //recurring info
            if (product.IsRecurring)
                cartItemModel.RecurringInfo = string.Format(
                    _translationService.GetResource("ShoppingCart.RecurringPeriod"),
                    product.RecurringCycleLength,
                    product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, request.Language.Id),
                    product.RecurringTotalCycles);

            //reservation info
            if (product.ProductTypeId == ProductType.Reservation)
            {
                if (sci.RentalEndDateUtc == default(DateTime) || sci.RentalEndDateUtc == null)
                    cartItemModel.ReservationInfo =
                        string.Format(_translationService.GetResource("ShoppingCart.Reservation.StartDate"),
                            sci.RentalStartDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat));
                else
                    cartItemModel.ReservationInfo = string.Format(
                        _translationService.GetResource("ShoppingCart.Reservation.Date"),
                        sci.RentalStartDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat),
                        sci.RentalEndDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat));

                if (!string.IsNullOrEmpty(sci.Parameter))
                {
                    cartItemModel.ReservationInfo += "<br>" +
                                                     string.Format(
                                                         _translationService.GetResource(
                                                             "ShoppingCart.Reservation.Option"), sci.Parameter);
                    cartItemModel.Parameter = sci.Parameter;
                }

                if (!string.IsNullOrEmpty(sci.Duration))
                    cartItemModel.ReservationInfo += "<br>" +
                                                     string.Format(
                                                         _translationService.GetResource(
                                                             "ShoppingCart.Reservation.Duration"), sci.Duration);
            }

            if (sci.ShoppingCartTypeId == ShoppingCartType.Auctions)
            {
                cartItemModel.DisableRemoval = true;
                cartItemModel.AuctionInfo = _translationService.GetResource("ShoppingCart.auctionwonon") + " " +
                                            _dateTimeService.ConvertToUserTime(product.AvailableEndDateTimeUtc.Value,
                                                DateTimeKind.Utc);
            }

            //unit prices
            if (product.CallForPrice)
            {
                cartItemModel.UnitPrice = _translationService.GetResource("Products.CallForPrice");
                cartItemModel.SubTotal = _translationService.GetResource("Products.CallForPrice");
                cartItemModel.UnitPriceWithoutDiscount = _translationService.GetResource("Products.CallForPrice");
            }
            else
            {
                var unitprices = await _pricingService.GetUnitPrice(sci, product);
                var appliedDiscounts = unitprices.appliedDiscounts;
                var productprices = await _taxService.GetProductPrice(product, unitprices.unitprice);

                cartItemModel.UnitPriceWithoutDiscountValue =
                    (await _taxService.GetProductPrice(product,
                        (await _pricingService.GetUnitPrice(sci, product, false)).unitprice)).productprice;

                cartItemModel.UnitPriceWithoutDiscount =
                    _priceFormatter.FormatPrice(cartItemModel.UnitPriceWithoutDiscountValue);
                cartItemModel.UnitPriceValue = productprices.productprice;
                cartItemModel.UnitPrice = _priceFormatter.FormatPrice(productprices.productprice);
                if (appliedDiscounts != null && appliedDiscounts.Any())
                {
                    var discount = await _discountService.GetDiscountById(appliedDiscounts.FirstOrDefault().DiscountId);
                    if (discount is { MaximumDiscountedQuantity: not null })
                        cartItemModel.DiscountedQty = discount.MaximumDiscountedQuantity.Value;

                    appliedDiscounts.ForEach(x => cartItemModel.Discounts.Add(x.DiscountId));
                }

                //sub total
                var subtotal = await _pricingService.GetSubTotal(sci, product);
                var shoppingCartItemDiscountBase = subtotal.discountAmount;
                var shoppingCartItemSubTotalWithDiscount =
                    (await _taxService.GetProductPrice(product, subtotal.subTotal)).productprice;
                cartItemModel.SubTotal = _priceFormatter.FormatPrice(shoppingCartItemSubTotalWithDiscount);
                cartItemModel.SubTotalValue = shoppingCartItemSubTotalWithDiscount;

                //display an applied discount amount
                if (shoppingCartItemDiscountBase > 0)
                {
                    shoppingCartItemDiscountBase =
                        (await _taxService.GetProductPrice(product, shoppingCartItemDiscountBase)).productprice;
                    if (shoppingCartItemDiscountBase > 0)
                        cartItemModel.Discount = _priceFormatter.FormatPrice(shoppingCartItemDiscountBase);
                }
            }

            //picture
            if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                cartItemModel.Picture = await PrepareCartItemPicture(product, sci.Attributes);

            //item warnings
            var itemWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(request.Customer, sci, product,
                new ShoppingCartValidatorOptions());
            foreach (var warning in itemWarnings)
                cartItemModel.Warnings.Add(warning);

            model.Items.Add(cartItemModel);
        }

        #endregion
    }

    private async Task<PictureModel> PrepareCartItemPicture(Product product, IList<CustomAttribute> attributes)
    {
        var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService);
        return new PictureModel {
            Id = sciPicture?.Id,
            ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.CartThumbPictureSize),
            Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), product.Name),
            AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"),
                product.Name)
        };
    }
}