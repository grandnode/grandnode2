using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Grand.Business.Checkout.Services.Orders
{
    public class ShoppingCartValidator : IShoppingCartValidator
    {

        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;
        private readonly IProductService _productService;
        private readonly ITranslationService _translationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        private readonly IAclService _aclService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductReservationService _productReservationService;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly IWarehouseService _warehouseService;
        private readonly ShoppingCartSettings _shoppingCartSettings;


        public ShoppingCartValidator(
            IWorkContext workContext,
            ICurrencyService currencyService,
            IProductService productService,
            ITranslationService translationService,
            IProductAttributeParser productAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICheckoutAttributeParser checkoutAttributeParser,
            IPriceFormatter priceFormatter,
            IMediator mediator,
            IPermissionService permissionService,
            IAclService aclService,
            IProductAttributeService productAttributeService,
            IProductReservationService productReservationService,
            IStockQuantityService stockQuantityService,
            IWarehouseService warehouseService,
            ShoppingCartSettings shoppingCartSettings)
        {
            _workContext = workContext;
            _currencyService = currencyService;
            _productService = productService;
            _translationService = translationService;
            _productAttributeParser = productAttributeParser;
            _checkoutAttributeService = checkoutAttributeService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _priceFormatter = priceFormatter;
            _mediator = mediator;
            _permissionService = permissionService;
            _aclService = aclService;
            _productAttributeService = productAttributeService;
            _productReservationService = productReservationService;
            _stockQuantityService = stockQuantityService;
            _warehouseService = warehouseService;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public virtual async Task<IList<string>> GetStandardWarnings(Customer customer, Product product, ShoppingCartItem shoppingCartItem)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var warnings = new List<string>();

            //published
            if (!product.Published)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.ProductUnpublished"));
            }

            //we can't add grouped product
            if (product.ProductTypeId == ProductType.GroupedProduct)
            {
                warnings.Add("You can't add grouped product");
            }

            //ACL
            if (!_aclService.Authorize(product, customer))
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.ProductUnpublished"));
            }

            //Store acl
            if (!_aclService.Authorize(product, shoppingCartItem.StoreId))
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.ProductUnpublished"));
            }

            //disabled "add to cart" button
            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.BuyingDisabled"));
            }

            //disabled "add to wishlist" button
            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.WishlistDisabled"));
            }

            //call for price
            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart && product.CallForPrice)
            {
                warnings.Add(_translationService.GetResource("Products.CallForPrice"));
            }

            //customer entered price
            if (product.EnteredPrice)
            {
                var shoppingCartItemEnteredPrice = shoppingCartItem.EnteredPrice.HasValue ? shoppingCartItem.EnteredPrice.Value : 0;
                if (shoppingCartItemEnteredPrice < product.MinEnteredPrice ||
                    shoppingCartItemEnteredPrice > product.MaxEnteredPrice)
                {
                    double minimumCustomerEnteredPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.MinEnteredPrice, _workContext.WorkingCurrency);
                    double maximumCustomerEnteredPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.MaxEnteredPrice, _workContext.WorkingCurrency);
                    warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.CustomerEnteredPrice.RangeError"),
                        _priceFormatter.FormatPrice(minimumCustomerEnteredPrice, false),
                        _priceFormatter.FormatPrice(maximumCustomerEnteredPrice, false)));
                }
            }

            //quantity validation
            var hasQtyWarnings = false;
            if (shoppingCartItem.Quantity < product.OrderMinimumQuantity)
            {
                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.MinimumQuantity"), product.OrderMinimumQuantity));
                hasQtyWarnings = true;
            }
            if (shoppingCartItem.Quantity > product.OrderMaximumQuantity)
            {
                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.MaximumQuantity"), product.OrderMaximumQuantity));
                hasQtyWarnings = true;
            }
            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(shoppingCartItem.Quantity))
            {
                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.AllowedQuantities"), string.Join(", ", allowedQuantities)));
            }

            if (_shoppingCartSettings.AllowToSelectWarehouse && string.IsNullOrEmpty(shoppingCartItem.WarehouseId))
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.RequiredWarehouse"));
            }

            var warehouseId = !string.IsNullOrEmpty(shoppingCartItem.WarehouseId) ? shoppingCartItem.WarehouseId : _workContext.CurrentStore?.DefaultWarehouseId;

            if (!string.IsNullOrEmpty(warehouseId))
            {
                var warehouse = await _warehouseService.GetWarehouseById(warehouseId);
                if (warehouse == null)
                    warnings.Add(_translationService.GetResource("ShoppingCart.WarehouseNotExists"));
            }

            var validateOutOfStock = shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart || !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
            if (validateOutOfStock && !hasQtyWarnings)
            {
                switch (product.ManageInventoryMethodId)
                {
                    case ManageInventoryMethod.DontManageStock:
                        {
                            //do nothing
                        }
                        break;
                    case ManageInventoryMethod.ManageStock:
                        {
                            if (product.BackorderModeId == BackorderMode.NoBackorders)
                            {
                                var qty = shoppingCartItem.Quantity;

                                qty += customer.ShoppingCartItems
                                    .Where(x => x.ShoppingCartTypeId == shoppingCartItem.ShoppingCartTypeId &&
                                        x.WarehouseId == warehouseId &&
                                        x.ProductId == shoppingCartItem.ProductId &&
                                        x.StoreId == shoppingCartItem.StoreId &&
                                        x.Id != shoppingCartItem.Id)
                                    .Sum(x => x.Quantity);

                                var maximumQuantityCanBeAdded = _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId);
                                if (maximumQuantityCanBeAdded < qty)
                                {
                                    if (maximumQuantityCanBeAdded <= 0)
                                        warnings.Add(_translationService.GetResource("ShoppingCart.OutOfStock"));
                                    else
                                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.QuantityExceedsStock"), maximumQuantityCanBeAdded));
                                }
                            }
                        }
                        break;
                    case ManageInventoryMethod.ManageStockByBundleProducts:
                        {
                            foreach (var item in product.BundleProducts)
                            {
                                var _qty = shoppingCartItem.Quantity * item.Quantity;
                                var p1 = await _productService.GetProductById(item.ProductId);
                                if (p1 != null)
                                {
                                    if (p1.BackorderModeId == BackorderMode.NoBackorders)
                                    {
                                        if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                                        {
                                            int maximumQuantityCanBeAdded = _stockQuantityService.GetTotalStockQuantity(p1, warehouseId: warehouseId);
                                            if (maximumQuantityCanBeAdded < _qty)
                                            {
                                                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct"), p1.Name));
                                            }
                                        }
                                        if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                                        {
                                            var combination = _productAttributeParser.FindProductAttributeCombination(p1, shoppingCartItem.Attributes);
                                            if (combination != null)
                                            {
                                                //combination exists - check stock level
                                                var stockquantity = _stockQuantityService.GetTotalStockQuantityForCombination(p1, combination, warehouseId: warehouseId);
                                                if (!combination.AllowOutOfStockOrders && stockquantity < _qty)
                                                {
                                                    if (stockquantity <= 0)
                                                    {
                                                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct"), p1.Name));
                                                    }
                                                    else
                                                    {
                                                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.QuantityExceedsStock.BundleProduct"), p1.Name, stockquantity));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                warnings.Add(_translationService.GetResource("ShoppingCart.Combination.NotExist"));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case ManageInventoryMethod.ManageStockByAttributes:
                        {
                            var combination = _productAttributeParser.FindProductAttributeCombination(product, shoppingCartItem.Attributes);
                            if (combination != null)
                            {
                                //combination exists - check stock level
                                var stockquantity = _stockQuantityService.GetTotalStockQuantityForCombination(product, combination, warehouseId: warehouseId);
                                if (!combination.AllowOutOfStockOrders && stockquantity < shoppingCartItem.Quantity)
                                {
                                    int maximumQuantityCanBeAdded = stockquantity;
                                    if (maximumQuantityCanBeAdded <= 0)
                                    {
                                        warnings.Add(_translationService.GetResource("ShoppingCart.OutOfStock"));
                                    }
                                    else
                                    {
                                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.QuantityExceedsStock"), maximumQuantityCanBeAdded));
                                    }
                                }
                            }
                            else
                            {
                                warnings.Add(_translationService.GetResource("ShoppingCart.Combination.NotExist"));
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            //availability dates
            bool availableStartDateError = false;
            if (product.AvailableStartDateTimeUtc.HasValue)
            {
                DateTime now = DateTime.UtcNow;
                DateTime availableStartDateTime = DateTime.SpecifyKind(product.AvailableStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableStartDateTime.CompareTo(now) > 0)
                {
                    warnings.Add(_translationService.GetResource("ShoppingCart.NotAvailable"));
                    availableStartDateError = true;
                }
            }
            if (product.AvailableEndDateTimeUtc.HasValue && !availableStartDateError && shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            {
                DateTime now = DateTime.UtcNow;
                DateTime availableEndDateTime = DateTime.SpecifyKind(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (availableEndDateTime.CompareTo(now) < 0)
                {
                    warnings.Add(_translationService.GetResource("ShoppingCart.NotAvailable"));
                }
            }
            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartItemAttributeWarnings(Customer customer,
         Product product, ShoppingCartItem shoppingCartItem, bool ignoreNonCombinableAttributes = false)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var warnings = new List<string>();

            //ensure it's our attributes
            var attributes1 = _productAttributeParser.ParseProductAttributeMappings(product, shoppingCartItem.Attributes).ToList();
            if (product.ProductTypeId == ProductType.BundledProduct)
            {
                foreach (var bundle in product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 != null)
                    {
                        var a1 = _productAttributeParser.ParseProductAttributeMappings(p1, shoppingCartItem.Attributes).ToList();
                        attributes1.AddRange(a1);
                    }
                }

            }
            if (ignoreNonCombinableAttributes)
            {
                attributes1 = attributes1.Where(x => !x.IsNonCombinable()).ToList();

            }

            //foreach (var attribute in attributes1)
            //{
            //    if (string.IsNullOrEmpty(attribute.ProductId))
            //    {
            //        warnings.Add("Attribute error");
            //        return warnings;
            //    }
            //}

            //validate required product attributes (whether they're chosen/selected/entered)
            var attributes2 = product.ProductAttributeMappings.ToList();
            if (product.ProductTypeId == ProductType.BundledProduct)
            {
                foreach (var bundle in product.BundleProducts)
                {
                    var p1 = await _productService.GetProductById(bundle.ProductId);
                    if (p1 != null && p1.ProductAttributeMappings.Any())
                    {
                        attributes2.AddRange(p1.ProductAttributeMappings);
                    }
                }
            }
            if (ignoreNonCombinableAttributes)
            {
                attributes2 = attributes2.Where(x => !x.IsNonCombinable()).ToList();
            }
            //validate conditional attributes only (if specified)
            attributes2 = attributes2.Where(x =>
            {
                var conditionMet = _productAttributeParser.IsConditionMet(product, x, shoppingCartItem.Attributes);
                return !conditionMet.HasValue || conditionMet.Value;
            }).ToList();
            foreach (var a2 in attributes2)
            {
                if (a2.IsRequired)
                {
                    bool found = false;
                    //selected product attributes
                    foreach (var a1 in attributes1)
                    {
                        if (a1.Id == a2.Id)
                        {
                            var attributeValuesStr = _productAttributeParser.ParseValues(shoppingCartItem.Attributes, a1.Id);
                            foreach (string str1 in attributeValuesStr)
                            {
                                if (!String.IsNullOrEmpty(str1.Trim()))
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }

                    //if not found
                    if (!found)
                    {
                        var paa = await _productAttributeService.GetProductAttributeById(a2.ProductAttributeId);
                        if (paa != null)
                        {
                            var notFoundWarning = !string.IsNullOrEmpty(a2.TextPrompt) ?
                                a2.TextPrompt :
                                string.Format(_translationService.GetResource("ShoppingCart.SelectAttribute"), paa.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id));

                            warnings.Add(notFoundWarning);
                        }
                    }
                }

                if (a2.AttributeControlTypeId == AttributeControlType.ReadonlyCheckboxes)
                {
                    //customers cannot edit read-only attributes
                    var allowedReadOnlyValueIds = a2.ProductAttributeValues
                        .Where(x => x.IsPreSelected)
                        .Select(x => x.Id)
                        .ToArray();

                    var selectedReadOnlyValueIds = _productAttributeParser.ParseProductAttributeValues(product, shoppingCartItem.Attributes)
                        //.Where(x => x.ProductAttributeMappingId == a2.Id)
                        .Select(x => x.Id)
                        .ToArray();

                    if (!CommonHelper.ArraysEqual(allowedReadOnlyValueIds, selectedReadOnlyValueIds))
                    {
                        warnings.Add("You cannot change read-only values");
                    }
                }
            }

            //validation rules
            foreach (var pam in attributes2)
            {
                if (!pam.ValidationRulesAllowed())
                    continue;

                //minimum length
                if (pam.ValidationMinLength.HasValue)
                {
                    if (pam.AttributeControlTypeId == AttributeControlType.TextBox ||
                        pam.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                    {
                        var valuesStr = _productAttributeParser.ParseValues(shoppingCartItem.Attributes, pam.Id);
                        var enteredText = valuesStr.FirstOrDefault();
                        int enteredTextLength = String.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                        if (pam.ValidationMinLength.Value > enteredTextLength)
                        {
                            var _pam = await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMinimumLength"), _pam.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), pam.ValidationMinLength.Value));
                        }
                    }
                }

                //maximum length
                if (pam.ValidationMaxLength.HasValue)
                {
                    if (pam.AttributeControlTypeId == AttributeControlType.TextBox ||
                        pam.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                    {
                        var valuesStr = _productAttributeParser.ParseValues(shoppingCartItem.Attributes, pam.Id);
                        var enteredText = valuesStr.FirstOrDefault();
                        int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                        if (pam.ValidationMaxLength.Value < enteredTextLength)
                        {
                            var _pam = await _productAttributeService.GetProductAttributeById(pam.ProductAttributeId);
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMaximumLength"), _pam.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), pam.ValidationMaxLength.Value));
                        }
                    }
                }
            }

            if (warnings.Any())
                return warnings;

            //validate bundled products
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, shoppingCartItem.Attributes);
            foreach (var attributeValue in attributeValues)
            {
                var _productAttributeMapping = product.ProductAttributeMappings.Where(x => x.ProductAttributeValues.Any(z => z.Id == attributeValue.Id)).FirstOrDefault();
                //TODO - check product.ProductAttributeMappings.Where(x => x.Id == attributeValue.ProductAttributeMappingId).FirstOrDefault();
                if (attributeValue.AttributeValueTypeId == AttributeValueType.AssociatedToProduct && _productAttributeMapping != null)
                {
                    if (ignoreNonCombinableAttributes && _productAttributeMapping.IsNonCombinable())
                        continue;

                    //associated product (bundle)
                    var associatedProduct = await _productService.GetProductById(attributeValue.AssociatedProductId);
                    if (associatedProduct != null)
                    {
                        var totalQty = shoppingCartItem.Quantity * attributeValue.Quantity;
                        var associatedProductWarnings = await GetShoppingCartItemWarnings(customer, new ShoppingCartItem() {
                            ShoppingCartTypeId = shoppingCartItem.ShoppingCartTypeId,
                            StoreId = _workContext.CurrentStore.Id,
                            Quantity = totalQty,
                            WarehouseId = shoppingCartItem.WarehouseId
                        }, associatedProduct, new ShoppingCartValidatorOptions());
                        foreach (var associatedProductWarning in associatedProductWarnings)
                        {
                            var productAttribute = await _productAttributeService.GetProductAttributeById(_productAttributeMapping.ProductAttributeId);
                            var attributeName = productAttribute.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
                            var attributeValueName = attributeValue.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id);
                            warnings.Add(string.Format(
                                _translationService.GetResource("ShoppingCart.AssociatedAttributeWarning"),
                                attributeName, attributeValueName, associatedProductWarning));
                        }
                    }
                    else
                    {
                        warnings.Add(string.Format("Associated product cannot be loaded - {0}", attributeValue.AssociatedProductId));
                    }
                }
            }

            return warnings;
        }


        public virtual IList<string> GetShoppingCartItemGiftVoucherWarnings(ShoppingCartType shoppingCartType,
           Product product, IList<CustomAttribute> attributes)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var warnings = new List<string>();

            //gift vouchers
            if (product.IsGiftVoucher)
            {
                _productAttributeParser.GetGiftVoucherAttribute(attributes,
                    out string giftVoucherRecipientName, out string giftVoucherRecipientEmail,
                    out string giftVoucherSenderName, out string giftVoucherSenderEmail, out string giftVoucherMessage);

                if (string.IsNullOrEmpty(giftVoucherRecipientName))
                    warnings.Add(_translationService.GetResource("ShoppingCart.RecipientNameError"));

                if (product.GiftVoucherTypeId == GiftVoucherType.Virtual)
                {
                    //validate for virtual gift vouchers only
                    if (string.IsNullOrEmpty(giftVoucherRecipientEmail) || !CommonHelper.IsValidEmail(giftVoucherRecipientEmail))
                        warnings.Add(_translationService.GetResource("ShoppingCart.RecipientEmailError"));
                }

                if (string.IsNullOrEmpty(giftVoucherSenderName))
                    warnings.Add(_translationService.GetResource("ShoppingCart.SenderNameError"));

                if (product.GiftVoucherTypeId == GiftVoucherType.Virtual)
                {
                    //validate for virtual gift vouchers only
                    if (string.IsNullOrEmpty(giftVoucherSenderEmail) || !CommonHelper.IsValidEmail(giftVoucherSenderEmail))
                        warnings.Add(_translationService.GetResource("ShoppingCart.SenderEmailError"));
                }
            }

            return warnings;
        }

        public virtual IList<string> GetAuctionProductWarning(double bid, Product product, Customer customer)
        {
            var warnings = new List<string>();
            if (bid <= product.HighestBid || bid <= product.StartPrice)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.BidMustBeHigher"));
            }

            if (!product.AvailableEndDateTimeUtc.HasValue)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.NotAvailable"));
            }

            if (product.AvailableEndDateTimeUtc < DateTime.UtcNow)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.NotAvailable"));
            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetReservationProductWarnings(Customer customer, Product product, ShoppingCartItem shoppingCartItem)
        {
            var warnings = new List<string>();

            if (product.ProductTypeId != ProductType.Reservation)
                return warnings;

            if (string.IsNullOrEmpty(shoppingCartItem.ReservationId) && product.IntervalUnitId != IntervalUnit.Day)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.NoReservationFound"));
                return warnings;
            }

            if (product.IntervalUnitId != IntervalUnit.Day)
            {
                var reservation = await _productReservationService.GetProductReservation(shoppingCartItem.ReservationId);
                if (reservation == null)
                {
                    warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.ReservationDeleted"));
                }
                else
                {
                    if (!string.IsNullOrEmpty(reservation.OrderId))
                    {
                        warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.AlreadyReserved"));
                    }
                }
            }
            else
            {
                if (!(shoppingCartItem.RentalStartDateUtc.HasValue && shoppingCartItem.RentalEndDateUtc.HasValue))
                {
                    warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.ChoosebothDates"));
                }
                else
                {
                    if (!product.IncBothDate)
                    {
                        if (shoppingCartItem.RentalStartDateUtc.Value >= shoppingCartItem.RentalEndDateUtc.Value)
                        {
                            warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.EndDateMustBeLaterThanStartDate"));
                        }
                    }
                    else
                    {
                        if (shoppingCartItem.RentalStartDateUtc.Value > shoppingCartItem.RentalEndDateUtc.Value)
                        {
                            warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.EndDateMustBeLaterThanStartDate"));
                        }
                    }

                    if (shoppingCartItem.RentalStartDateUtc.Value < DateTime.Now || shoppingCartItem.RentalEndDateUtc.Value < DateTime.Now)
                    {
                        warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.ReservationDatesMustBeLaterThanToday"));
                    }

                    if (customer.ShoppingCartItems.Any(x => x.Id == shoppingCartItem.Id))
                    {
                        var reserved = await _productReservationService.GetCustomerReservationsHelperBySciId(shoppingCartItem.Id);
                        if (!reserved.Any())
                            warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.ReservationDeleted"));
                        else
                            foreach (var item in reserved)
                            {
                                var reservation = await _productReservationService.GetProductReservation(item.ReservationId);
                                if (reservation == null)
                                {
                                    warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.ReservationDeleted"));
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(reservation.OrderId))
                                {
                                    warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.AlreadyReserved"));
                                    break;
                                }
                            }
                    }
                }
            }

            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartWarnings(IList<ShoppingCartItem> shoppingCart,
            IList<CustomAttribute> checkoutAttributes, bool validateCheckoutAttributes)
        {
            var warnings = new List<string>();
            if (checkoutAttributes == null)
                checkoutAttributes = new List<CustomAttribute>();

            bool hasStandartProducts = false;
            bool hasRecurringProducts = false;

            foreach (var sci in shoppingCart)
            {
                var product = await _productService.GetProductById(sci.ProductId);
                if (product == null)
                {
                    warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.CannotLoadProduct"), sci.ProductId));
                    return warnings;
                }

                if (product.IsRecurring)
                    hasRecurringProducts = true;
                else
                    hasStandartProducts = true;
            }

            //don't mix standard and recurring products
            if (hasStandartProducts && hasRecurringProducts)
                warnings.Add(_translationService.GetResource("ShoppingCart.CannotMixStandardAndAutoshipProducts"));

            //validate checkout attributes
            if (validateCheckoutAttributes)
            {
                //selected attributes
                var attributes1 = await _checkoutAttributeParser.ParseCheckoutAttributes(checkoutAttributes);

                //existing checkout attributes
                var attributes2 = await _checkoutAttributeService.GetAllCheckoutAttributes(_workContext.CurrentStore.Id, !shoppingCart.RequiresShipping());
                foreach (var a2 in attributes2)
                {
                    var conditionMet = await _checkoutAttributeParser.IsConditionMet(a2, checkoutAttributes);
                    if (a2.IsRequired && ((conditionMet.HasValue && conditionMet.Value) || !conditionMet.HasValue))
                    {
                        bool found = false;
                        //selected checkout attributes
                        foreach (var a1 in attributes1)
                        {
                            if (a1.Id == a2.Id)
                            {
                                var attributeValuesStr = checkoutAttributes.Where(x => x.Key == a1.Id).Select(x => x.Value);
                                foreach (var str1 in attributeValuesStr)
                                    if (!string.IsNullOrEmpty(str1.Trim()))
                                    {
                                        found = true;
                                        break;
                                    }
                            }
                        }

                        //if not found
                        if (!found)
                        {
                            if (!string.IsNullOrEmpty(a2.GetTranslation(a => a.TextPrompt, _workContext.WorkingLanguage.Id)))
                                warnings.Add(a2.GetTranslation(a => a.TextPrompt, _workContext.WorkingLanguage.Id));
                            else
                                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.SelectAttribute"), a2.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id)));
                        }
                    }
                }

                //now validation rules

                //minimum length
                foreach (var ca in attributes2)
                {
                    if (ca.ValidationMinLength.HasValue)
                    {
                        if (ca.AttributeControlTypeId == AttributeControlType.TextBox ||
                            ca.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            var valuesStr = checkoutAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value);
                            var enteredText = valuesStr?.FirstOrDefault();
                            int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                            if (ca.ValidationMinLength.Value > enteredTextLength)
                            {
                                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMinimumLength"), ca.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), ca.ValidationMinLength.Value));
                            }
                        }
                    }

                    //maximum length
                    if (ca.ValidationMaxLength.HasValue)
                    {
                        if (ca.AttributeControlTypeId == AttributeControlType.TextBox ||
                            ca.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            var valuesStr = checkoutAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value);
                            var enteredText = valuesStr?.FirstOrDefault();
                            int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                            if (ca.ValidationMaxLength.Value < enteredTextLength)
                            {
                                warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.TextboxMaximumLength"), ca.GetTranslation(a => a.Name, _workContext.WorkingLanguage.Id), ca.ValidationMaxLength.Value));
                            }
                        }
                    }
                }
            }

            //event notification
            await _mediator.ShoppingCartWarningsAdd(warnings, shoppingCart, checkoutAttributes, validateCheckoutAttributes);

            return warnings;
        }

        public async Task<IList<string>> CheckCommonWarnings(Customer customer, IList<ShoppingCartItem> currentCart, Product product,
          ShoppingCartType shoppingCartType, DateTime? rentalStartDate, DateTime? rentalEndDate,
          int quantity, string reservationId)
        {
            var warnings = new List<string>();

            //maximum items validation
            switch (shoppingCartType)
            {
                case ShoppingCartType.ShoppingCart:
                    {
                        if (currentCart.Count >= _shoppingCartSettings.MaximumShoppingCartItems)
                        {
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.MaximumShoppingCartItems"), _shoppingCartSettings.MaximumShoppingCartItems));
                        }
                    }
                    break;
                case ShoppingCartType.Wishlist:
                    {
                        if (currentCart.Count >= _shoppingCartSettings.MaximumWishlistItems)
                        {
                            warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.MaximumWishlistItems"), _shoppingCartSettings.MaximumWishlistItems));
                        }
                    }
                    break;
                default:
                    break;
            }

            if (shoppingCartType == ShoppingCartType.ShoppingCart && !await _permissionService.Authorize(StandardPermission.EnableShoppingCart, customer))
            {
                warnings.Add("Shopping cart is disabled");
                return warnings;
            }
            if (shoppingCartType == ShoppingCartType.Wishlist && !await _permissionService.Authorize(StandardPermission.EnableWishlist, customer))
            {
                warnings.Add("Wishlist is disabled");
                return warnings;
            }
            if (customer.IsSystemAccount())
            {
                warnings.Add("System account can't add to cart");
                return warnings;
            }

            if (quantity <= 0)
            {
                warnings.Add(_translationService.GetResource("ShoppingCart.QuantityShouldPositive"));
                return warnings;
            }

            if (!string.IsNullOrEmpty(reservationId))
            {
                var reservations = await _productReservationService.GetCustomerReservationsHelpers(_workContext.CurrentCustomer.Id);
                if (reservations.Where(x => x.ReservationId == reservationId).Any())
                    warnings.Add(_translationService.GetResource("ShoppingCart.AlreadyReservation"));
            }

            if (rentalStartDate.HasValue && rentalEndDate.HasValue)
            {
                var _canBeBook = false;
                var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                var reserved = await _productReservationService.GetCustomerReservationsHelpers(_workContext.CurrentCustomer.Id);
                foreach (var item in reserved)
                {
                    var match = reservations.Where(x => x.Id == item.ReservationId).FirstOrDefault();
                    if (match != null)
                    {
                        reservations.Remove(match);
                    }
                }

                var grouped = reservations.GroupBy(x => x.Resource);
                foreach (var group in grouped)
                {
                    bool groupCanBeBooked = true;
                    if (product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day)
                    {
                        for (DateTime iterator = rentalStartDate.Value; iterator <= rentalEndDate.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (!group.Select(x => x.Date).Contains(iterator))
                            {
                                groupCanBeBooked = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (DateTime iterator = rentalStartDate.Value; iterator < rentalEndDate.Value; iterator += new TimeSpan(24, 0, 0))
                        {
                            if (!group.Select(x => x.Date).Contains(iterator))
                            {
                                groupCanBeBooked = false;
                                break;
                            }
                        }
                    }
                    if (groupCanBeBooked)
                    {
                        _canBeBook = true;
                        break;
                    }
                }

                if (!_canBeBook)
                {
                    warnings.Add(_translationService.GetResource("ShoppingCart.Reservation.NoFreeReservationsInThisPeriod"));
                    return warnings;
                }
            }
            return warnings;
        }

        public virtual async Task<IList<string>> GetShoppingCartItemWarnings(Customer customer, ShoppingCartItem shoppingCartItem,
         Product product, ShoppingCartValidatorOptions options)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var warnings = new List<string>();

            //standard properties
            if (options.GetStandardWarnings)
                warnings.AddRange(await GetStandardWarnings(customer, product, shoppingCartItem));

            //selected attributes
            if (options.GetAttributesWarnings)
                warnings.AddRange(await GetShoppingCartItemAttributeWarnings(customer, product, shoppingCartItem));

            //gift vouchers
            if (options.GetGiftVoucherWarnings)
                warnings.AddRange(GetShoppingCartItemGiftVoucherWarnings(shoppingCartItem.ShoppingCartTypeId, product, shoppingCartItem.Attributes));

            //required products
            if (options.GetRequiredProductWarnings)
                warnings.AddRange(await GetRequiredProductWarnings(customer, shoppingCartItem.ShoppingCartTypeId, product, shoppingCartItem.StoreId));

            //reservation products
            if (options.GetReservationWarnings)
                warnings.AddRange(await GetReservationProductWarnings(customer, product, shoppingCartItem));

            //event notification
            await _mediator.ShoppingCartItemWarningsAdded(warnings, customer, shoppingCartItem, product);

            return warnings;
        }


        /// <summary>
        /// Validates required products (products which require some other products to be added to the cart)
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="product">Product</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="automaticallyAddRequiredProductsIfEnabled">Automatically add required products if enabled</param>
        /// <returns>Warnings</returns>
        public virtual async Task<IList<string>> GetRequiredProductWarnings(Customer customer,
            ShoppingCartType shoppingCartType, Product product,
            string storeId)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == shoppingCartType)
                .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, storeId)
                .ToList();

            var warnings = new List<string>();

            if (product.RequireOtherProducts)
            {
                var requiredProducts = new List<Product>();
                foreach (var id in product.ParseRequiredProductIds())
                {
                    var rp = await _productService.GetProductById(id);
                    if (rp != null)
                        requiredProducts.Add(rp);
                }

                foreach (var rp in requiredProducts)
                {
                    //ensure that product is in the cart
                    bool alreadyInTheCart = false;
                    foreach (var sci in cart)
                    {
                        if (sci.ProductId == rp.Id)
                        {
                            alreadyInTheCart = true;
                            break;
                        }
                    }
                    //not in the cart
                    if (!alreadyInTheCart)
                    {
                        warnings.Add(string.Format(_translationService.GetResource("ShoppingCart.RequiredProductWarning"), rp.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id)));
                    }
                }
            }
            return warnings;
        }
    }
}
