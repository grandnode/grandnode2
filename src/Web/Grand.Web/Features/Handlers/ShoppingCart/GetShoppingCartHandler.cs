using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Common;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetShoppingCartHandler : IRequestHandler<GetShoppingCart, ShoppingCartModel>
    {
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ITranslationService _translationService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ICurrencyService _currencyService;
        private readonly IDiscountService _discountService;
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly IPermissionService _permissionService;
        private readonly ITaxService _taxService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IDownloadService _downloadService;
        private readonly ICountryService _countryService;
        private readonly IWarehouseService _warehouseService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IPricingService _pricingService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IVendorService _vendorService;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;
        private readonly MediaSettings _mediaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly CommonSettings _commonSettings;

        public GetShoppingCartHandler(
            IPaymentService paymentService,
            IProductService productService,
            IPictureService pictureService,
            IProductAttributeParser productAttributeParser,
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
            ICountryService countryService,
            IWarehouseService warehouseService,
            IProductAttributeFormatter productAttributeFormatter,
            IPricingService priceCalculationService,
            IDateTimeService dateTimeService,
            IVendorService vendorService,
            IGroupService groupService,
            IMediator mediator,
            IShoppingCartValidator shoppingCartValidator,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            ShippingSettings shippingSettings,
            CommonSettings commonSettings)
        {
            _paymentService = paymentService;
            _productService = productService;
            _pictureService = pictureService;
            _productAttributeParser = productAttributeParser;
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
            _countryService = countryService;
            _warehouseService = warehouseService;
            _productAttributeFormatter = productAttributeFormatter;
            _pricingService = priceCalculationService;
            _dateTimeService = dateTimeService;
            _vendorService = vendorService;
            _groupService = groupService;
            _mediator = mediator;
            _shoppingCartValidator = shoppingCartValidator;
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
            _mediaSettings = mediaSettings;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _shippingSettings = shippingSettings;
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

            await PrepareOrderReview(model, request);

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
            var checkoutAttributes = request.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, request.Store.Id);
            model.CheckoutAttributeInfo = await _checkoutAttributeFormatter.FormatAttributes(checkoutAttributes, request.Customer);
            if (!request.Cart.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart || x.ShoppingCartTypeId == ShoppingCartType.Auctions).ToList().Any())
            {
                model.MinOrderSubtotalWarning = _translationService.GetResource("Checkout.MinOrderOneProduct");
            }
            else
            {
                var minOrderSubtotalAmountOk = await _mediator.Send(new ValidateMinShoppingCartSubtotalAmountCommand()
                {
                    Customer = request.Customer,
                    Cart = request.Cart.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart || x.ShoppingCartTypeId == ShoppingCartType.Auctions).ToList()
                });
                if (!minOrderSubtotalAmountOk)
                {
                    double minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, request.Currency);
                    model.MinOrderSubtotalWarning = string.Format(_translationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, false));
                }
            }
            model.TermsOfServiceOnShoppingCartPage = _orderSettings.TermsOfServiceOnShoppingCartPage;
            model.TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage;

            model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
            var discountCouponCodes = request.Customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
            foreach (var couponCode in discountCouponCodes)
            {
                var discount = await _discountService.GetDiscountByCouponCode(couponCode);
                if (discount != null &&
                    discount.RequiresCouponCode &&
                    (await _discountService.ValidateDiscount(discount, request.Customer, request.Currency)).IsValid)
                {
                    model.DiscountBox.AppliedDiscountsWithCodes.Add(new ShoppingCartModel.DiscountBoxModel.DiscountInfoModel()
                    {
                        Id = discount.Id,
                        CouponCode = couponCode
                    });
                }
            }

            model.GiftVoucherBox.Display = _shoppingCartSettings.ShowGiftVoucherBox;

            //cart warnings
            var cartWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(request.Cart, checkoutAttributes, request.ValidateCheckoutAttributes);
            foreach (var warning in cartWarnings)
                model.Warnings.Add(warning);

            #endregion
        }

        private async Task PrepareCheckoutAttributes(ShoppingCartModel model, GetShoppingCart request)
        {
            #region Checkout attributes

            var checkoutAttributes = await _checkoutAttributeService.GetAllCheckoutAttributes(request.Store.Id, !request.Cart.RequiresShipping());
            foreach (var attribute in checkoutAttributes)
            {
                var attributeModel = new ShoppingCartModel.CheckoutAttributeModel
                {
                    Id = attribute.Id,
                    Name = attribute.GetTranslation(x => x.Name, request.Language.Id),
                    TextPrompt = attribute.GetTranslation(x => x.TextPrompt, request.Language.Id),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId,
                    DefaultValue = attribute.DefaultValue
                };
                if (!String.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.CheckoutAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new ShoppingCartModel.CheckoutAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetTranslation(x => x.Name, request.Language.Id),
                            ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                            IsPreSelected = attributeValue.IsPreSelected,
                        };
                        attributeModel.Values.Add(attributeValueModel);

                        //display price if allowed
                        if (await _permissionService.Authorize(StandardPermission.DisplayPrices))
                        {
                            double priceAdjustmentBase = (await _taxService.GetCheckoutAttributePrice(attribute, attributeValue)).checkoutPrice;
                            double priceAdjustment = await _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, request.Currency);
                            if (priceAdjustmentBase > 0)
                                attributeValueModel.PriceAdjustment = "+" + _priceFormatter.FormatPrice(priceAdjustment);
                            else if (priceAdjustmentBase < 0)
                                attributeValueModel.PriceAdjustment = "-" + _priceFormatter.FormatPrice(-priceAdjustment);
                        }
                    }
                }
                //set already selected attributes
                var selectedCheckoutAttributes = request.Customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, request.Store.Id);
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
                                var selectedValues = await _checkoutAttributeParser.ParseCheckoutAttributeValues(selectedCheckoutAttributes);
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
                        {
                            if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                            {
                                var enteredText = selectedCheckoutAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                                if (enteredText.Any())
                                    attributeModel.DefaultValue = enteredText[0];
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                            {
                                //keep in mind my that the code below works only in the current culture
                                var selectedDateStr = selectedCheckoutAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                                if (selectedDateStr.Any())
                                {
                                    DateTime selectedDate;
                                    if (DateTime.TryParseExact(selectedDateStr[0], "D", CultureInfo.CurrentCulture,
                                                           DateTimeStyles.None, out selectedDate))
                                    {
                                        //successfully parsed
                                        attributeModel.SelectedDay = selectedDate.Day;
                                        attributeModel.SelectedMonth = selectedDate.Month;
                                        attributeModel.SelectedYear = selectedDate.Year;
                                    }
                                }
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            if (selectedCheckoutAttributes != null && selectedCheckoutAttributes.Any())
                            {
                                var downloadGuidStr = selectedCheckoutAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).FirstOrDefault();
                                Guid downloadGuid;
                                Guid.TryParse(downloadGuidStr, out downloadGuid);
                                var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                                if (download != null)
                                    attributeModel.DefaultValue = download.DownloadGuid.ToString();
                            }
                        }
                        break;
                    default:
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
                var cartItemModel = new ShoppingCartModel.ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Sku = product.FormatSku(sci.Attributes, _productAttributeParser),
                    IsCart = sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart,
                    ProductId = product.Id,
                    WarehouseId = sci.WarehouseId,
                    ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                    ProductSeName = sename,
                    ProductUrl = _linkGenerator.GetUriByRouteValues(_httpContextAccessor.HttpContext, "Product", new { SeName = sename }),
                    Quantity = sci.Quantity,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes),
                };

                cartItemModel.AllowItemEditing = _shoppingCartSettings.AllowCartItemEditing &&
                    product.ProductTypeId == ProductType.SimpleProduct &&
                    (!String.IsNullOrEmpty(cartItemModel.AttributeInfo) || product.IsGiftVoucher) &&
                    product.VisibleIndividually;

                if (product.RequireOtherProducts)
                    cartItemModel.DisableRemoval = product.RequireOtherProducts && product.ParseRequiredProductIds().Intersect(request.Cart.Select(x => x.ProductId)).Any();

                //warehouse
                if (!string.IsNullOrEmpty(cartItemModel.WarehouseId))
                    cartItemModel.WarehouseName = (await _warehouseService.GetWarehouseById(cartItemModel.WarehouseId))?.Name;

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
                {
                    cartItemModel.AllowedQuantities.Add(new SelectListItem
                    {
                        Text = qty.ToString(),
                        Value = qty.ToString(),
                        Selected = sci.Quantity == qty
                    });
                }

                //recurring info
                if (product.IsRecurring)
                    cartItemModel.RecurringInfo = string.Format(_translationService.GetResource("ShoppingCart.RecurringPeriod"), product.RecurringCycleLength, product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, request.Language.Id));

                //reservation info
                if (product.ProductTypeId == ProductType.Reservation)
                {
                    if (sci.RentalEndDateUtc == default(DateTime) || sci.RentalEndDateUtc == null)
                    {
                        cartItemModel.ReservationInfo = string.Format(_translationService.GetResource("ShoppingCart.Reservation.StartDate"), sci.RentalStartDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat));
                    }
                    else
                    {
                        cartItemModel.ReservationInfo = string.Format(_translationService.GetResource("ShoppingCart.Reservation.Date"), sci.RentalStartDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat), sci.RentalEndDateUtc?.ToString(_shoppingCartSettings.ReservationDateFormat));
                    }

                    if (!string.IsNullOrEmpty(sci.Parameter))
                    {
                        cartItemModel.ReservationInfo += "<br>" + string.Format(_translationService.GetResource("ShoppingCart.Reservation.Option"), sci.Parameter);
                        cartItemModel.Parameter = sci.Parameter;
                    }
                    if (!string.IsNullOrEmpty(sci.Duration))
                    {
                        cartItemModel.ReservationInfo += "<br>" + string.Format(_translationService.GetResource("ShoppingCart.Reservation.Duration"), sci.Duration);
                    }
                }
                if (sci.ShoppingCartTypeId == ShoppingCartType.Auctions)
                {
                    cartItemModel.DisableRemoval = true;
                    cartItemModel.AuctionInfo = _translationService.GetResource("ShoppingCart.auctionwonon") + " " + _dateTimeService.ConvertToUserTime(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc).ToString();
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
                    var unitprices = await _pricingService.GetUnitPrice(sci, product, true);
                    double discountAmount = unitprices.discountAmount;
                    List<ApplyDiscount> appliedDiscounts = unitprices.appliedDiscounts;
                    var productprices = await _taxService.GetProductPrice(product, unitprices.unitprice);
                    double taxRate = productprices.taxRate;

                    cartItemModel.UnitPriceWithoutDiscountValue =
                        (await _taxService.GetProductPrice(product, (await _pricingService.GetUnitPrice(sci, product, false)).unitprice)).productprice;

                    cartItemModel.UnitPriceWithoutDiscount = _priceFormatter.FormatPrice(cartItemModel.UnitPriceWithoutDiscountValue);
                    cartItemModel.UnitPriceValue = productprices.productprice;
                    cartItemModel.UnitPrice = _priceFormatter.FormatPrice(productprices.productprice);
                    if (appliedDiscounts != null && appliedDiscounts.Any())
                    {
                        var discount = await _discountService.GetDiscountById(appliedDiscounts.FirstOrDefault().DiscountId);
                        if (discount != null && discount.MaximumDiscountedQuantity.HasValue)
                            cartItemModel.DiscountedQty = discount.MaximumDiscountedQuantity.Value;

                        foreach (var disc in appliedDiscounts)
                        {
                            cartItemModel.Discounts.Add(disc.DiscountId);
                        }
                    }
                    //sub total
                    var subtotal = await _pricingService.GetSubTotal(sci, product, true);
                    double shoppingCartItemDiscountBase = subtotal.discountAmount;
                    List<ApplyDiscount> scDiscounts = subtotal.appliedDiscounts;
                    var shoppingCartItemSubTotalWithDiscount = (await _taxService.GetProductPrice(product, subtotal.subTotal)).productprice;
                    cartItemModel.SubTotal = _priceFormatter.FormatPrice(shoppingCartItemSubTotalWithDiscount);
                    cartItemModel.SubTotalValue = shoppingCartItemSubTotalWithDiscount;

                    //display an applied discount amount
                    if (shoppingCartItemDiscountBase > 0)
                    {
                        shoppingCartItemDiscountBase = (await _taxService.GetProductPrice(product, shoppingCartItemDiscountBase)).productprice;
                        if (shoppingCartItemDiscountBase > 0)
                        {
                            cartItemModel.Discount = _priceFormatter.FormatPrice(shoppingCartItemDiscountBase);
                        }
                    }
                }
                //picture
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    cartItemModel.Picture = await PrepareCartItemPicture(product, sci.Attributes);
                }

                //item warnings
                var itemWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(request.Customer, sci, product, new ShoppingCartValidatorOptions());
                foreach (var warning in itemWarnings)
                    cartItemModel.Warnings.Add(warning);

                model.Items.Add(cartItemModel);
            }

            #endregion

        }

        private async Task PrepareOrderReview(ShoppingCartModel model, GetShoppingCart request)
        {
            #region Order review data

            if (request.PrepareAndDisplayOrderReviewData)
            {
                model.OrderReviewData.Display = true;

                //billing info
                var billingAddress = request.Customer.BillingAddress;
                if (billingAddress != null)
                    model.OrderReviewData.BillingAddress = await _mediator.Send(new GetAddressModel()
                    {
                        Language = request.Language,
                        Address = billingAddress,
                        ExcludeProperties = false,
                    });
                //shipping info
                if (request.Cart.RequiresShipping())
                {
                    model.OrderReviewData.IsShippable = true;

                    var pickupPoint = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPickupPoint, request.Store.Id);

                    model.OrderReviewData.SelectedPickUpInStore = _shippingSettings.AllowPickUpInStore && !String.IsNullOrEmpty(pickupPoint);

                    if (!model.OrderReviewData.SelectedPickUpInStore)
                    {
                        var shippingAddress = request.Customer.ShippingAddress;
                        if (shippingAddress != null)
                            model.OrderReviewData.ShippingAddress = await _mediator.Send(new GetAddressModel()
                            {
                                Language = request.Language,
                                Address = shippingAddress,
                                ExcludeProperties = false,
                            });
                    }
                    else
                    {
                        var pickup = await _mediator.Send(new GetPickupPointById() { Id = pickupPoint });
                        if (pickup != null)
                        {
                            var country = await _countryService.GetCountryById(pickup.Address.CountryId);
                            model.OrderReviewData.PickupAddress = new AddressModel
                            {
                                Address1 = pickup.Address.Address1,
                                City = pickup.Address.City,
                                CountryName = country != null ? country.Name : string.Empty,
                                ZipPostalCode = pickup.Address.ZipPostalCode
                            };
                        }
                    }
                    //selected shipping method
                    var shippingOption = request.Customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption, request.Store.Id);
                    if (shippingOption != null)
                    {
                        model.OrderReviewData.ShippingMethod = shippingOption.Name;
                        model.OrderReviewData.ShippingAdditionDescription = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ShippingOptionAttributeDescription, request.Store.Id);
                    }
                }
                //payment info
                var selectedPaymentMethodSystemName = request.Customer.GetUserFieldFromEntity<string>(
                    SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(selectedPaymentMethodSystemName);
                model.OrderReviewData.PaymentMethod = paymentMethod != null ? paymentMethod.FriendlyName : "";

            }
            #endregion
        }

        private async Task<PictureModel> PrepareCartItemPicture(Product product, IList<CustomAttribute> attributes)
        {
            var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService, _productAttributeParser);
            return new PictureModel
            {
                Id = sciPicture?.Id,
                ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.CartThumbPictureSize, true),
                Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), product.Name),
                AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"), product.Name),
            };
        }

    }
}
