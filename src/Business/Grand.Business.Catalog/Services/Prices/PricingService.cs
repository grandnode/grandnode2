using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Models;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Business.Catalog.Services.Prices
{
    /// <summary>
    /// Pricing service
    /// </summary>
    public partial class PricingService : IPricingService
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IDiscountService _discountService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly ICollectionService _collectionService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly IMediator _mediator;
        private readonly ICurrencyService _currencyService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public PricingService(IWorkContext workContext,
            IDiscountService discountService,
            ICategoryService categoryService,
            IBrandService brandService,
            ICollectionService collectionService,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            IMediator mediator,
            ICurrencyService currencyService,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
        {
            _workContext = workContext;
            _discountService = discountService;
            _categoryService = categoryService;
            _brandService = brandService;
            _collectionService = collectionService;
            _productAttributeParser = productAttributeParser;
            _productService = productService;
            _mediator = mediator;
            _mediator = mediator;
            _currencyService = currencyService;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Nested classes

        protected class ProductPrice
        {
            public ProductPrice()
            {
                AppliedDiscounts = new List<ApplyDiscount>();
            }
            public double Price { get; set; }
            public double AppliedDiscountAmount { get; set; }
            public IList<ApplyDiscount> AppliedDiscounts { get; set; }
            public TierPrice PreferredTierPrice { get; set; }

        }
        #endregion

        #region Utilities

        /// <summary>
        /// Gets allowed discounts
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discounts</returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToProduct(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            if (product.AppliedDiscounts.Any())
            {
                foreach (var appliedDiscount in product.AppliedDiscounts)
                {
                    var discount = await _discountService.GetDiscountById(appliedDiscount);
                    if (discount != null)
                    {
                        var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                        if (validDiscount.IsValid &&
                            discount.DiscountTypeId == DiscountType.AssignedToSkus)
                            allowedDiscounts.Add(new ApplyDiscount()
                            {
                                CouponCode = validDiscount.CouponCode,
                                DiscountId = discount.Id,
                                IsCumulative = discount.IsCumulative
                            });
                    }
                }
            }
            return allowedDiscounts;
        }

        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToAllProduct(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            var discounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToAllProducts, storeId: _workContext.CurrentStore.Id, currencyCode: currency.CurrencyCode);
            foreach (var discount in discounts)
            {
                var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                if (validDiscount.IsValid)
                    allowedDiscounts.Add(new ApplyDiscount()
                    {
                        CouponCode = validDiscount.CouponCode,
                        DiscountId = discount.Id,
                        IsCumulative = discount.IsCumulative
                    });
            }
            return allowedDiscounts;
        }


        /// <summary>
        /// Gets allowed discounts applied to categories
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discounts</returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToCategories(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            foreach (var productCategory in product.ProductCategories)
            {
                var category = await _categoryService.GetCategoryById(productCategory.CategoryId);
                if (category != null && category.AppliedDiscounts.Any())
                {
                    foreach (var appliedDiscount in category.AppliedDiscounts)
                    {
                        var discount = await _discountService.GetDiscountById(appliedDiscount);
                        if (discount != null)
                        {
                            var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                            if (validDiscount.IsValid && discount.DiscountTypeId == DiscountType.AssignedToCategories)
                                allowedDiscounts.Add(new ApplyDiscount()
                                {
                                    CouponCode = validDiscount.CouponCode,
                                    DiscountId = discount.Id,
                                    IsCumulative = discount.IsCumulative
                                });
                        }
                    }
                }
            }
            return allowedDiscounts;
        }
        /// <summary>
        /// Get allowed discount applied to brands
        /// </summary>
        /// <param name="product"></param>
        /// <param name="customer"></param>
        /// <param name="currency">Currency</param>
        /// <returns></returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToBrands(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            if (!string.IsNullOrEmpty(product.BrandId))
            {
                var brand = await _brandService.GetBrandById(product.BrandId);
                if (brand != null)
                {
                    if (brand.AppliedDiscounts.Any())
                    {
                        foreach (var appliedDiscount in brand.AppliedDiscounts)
                        {
                            var discount = await _discountService.GetDiscountById(appliedDiscount);
                            if (discount != null)
                            {
                                var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                                if (validDiscount.IsValid &&
                                         discount.DiscountTypeId == DiscountType.AssignedToBrands)
                                    allowedDiscounts.Add(new ApplyDiscount()
                                    {
                                        CouponCode = validDiscount.CouponCode,
                                        DiscountId = discount.Id,
                                        IsCumulative = discount.IsCumulative,
                                    });
                            }
                        }
                    }
                }
            }
            return allowedDiscounts;
        }

        /// <summary>
        /// Gets allowed discounts applied to collections
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discounts</returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToCollections(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            foreach (var productCollection in product.ProductCollections)
            {
                var collection = await _collectionService.GetCollectionById(productCollection.CollectionId);
                if (collection != null && collection.AppliedDiscounts.Any())
                {
                    foreach (var appliedDiscount in collection.AppliedDiscounts)
                    {
                        var discount = await _discountService.GetDiscountById(appliedDiscount);
                        if (discount != null)
                        {
                            var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                            if (validDiscount.IsValid &&
                                     discount.DiscountTypeId == DiscountType.AssignedToCollections)
                                allowedDiscounts.Add(new ApplyDiscount()
                                {
                                    CouponCode = validDiscount.CouponCode,
                                    DiscountId = discount.Id,
                                    IsCumulative = discount.IsCumulative
                                });
                        }
                    }
                }
            }
            return allowedDiscounts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="product"></param>
        /// <param name="customer"></param>
        /// <param name="currency">Currency</param>
        /// <returns></returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscountsAppliedToVendors(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            if (!string.IsNullOrEmpty(product.VendorId))
            {
                var vendor = await _mediator.Send(new GetVendorByIdQuery() { Id = product.VendorId });
                if (vendor != null)
                {
                    if (vendor.AppliedDiscounts.Any())
                    {
                        foreach (var appliedDiscount in vendor.AppliedDiscounts)
                        {
                            var discount = await _discountService.GetDiscountById(appliedDiscount);
                            if (discount != null)
                            {
                                var validDiscount = await _discountService.ValidateDiscount(discount, customer, currency);
                                if (validDiscount.IsValid &&
                                         discount.DiscountTypeId == DiscountType.AssignedToVendors)
                                    allowedDiscounts.Add(new ApplyDiscount()
                                    {
                                        CouponCode = validDiscount.CouponCode,
                                        DiscountId = discount.Id,
                                        IsCumulative = discount.IsCumulative,
                                    });
                            }
                        }
                    }
                }
            }
            return allowedDiscounts;
        }

        /// <summary>
        /// Gets allowed discounts
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discounts</returns>
        protected virtual async Task<IList<ApplyDiscount>> GetAllowedDiscounts(Product product, Customer customer, Currency currency)
        {
            var allowedDiscounts = new List<ApplyDiscount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            //discounts applied to products
            foreach (var discount in await GetAllowedDiscountsAppliedToProduct(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            //discounts applied to all products
            foreach (var discount in await GetAllowedDiscountsAppliedToAllProduct(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            //discounts applied to categories
            foreach (var discount in await GetAllowedDiscountsAppliedToCategories(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            //discounts applied to brands
            foreach (var discount in await GetAllowedDiscountsAppliedToBrands(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            //discounts applied to collections
            foreach (var discount in await GetAllowedDiscountsAppliedToCollections(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            //discounts applied to vendors
            foreach (var discount in await GetAllowedDiscountsAppliedToVendors(product, customer, currency))
                if (!allowedDiscounts.Where(x => x.DiscountId == discount.DiscountId).Any())
                    allowedDiscounts.Add(discount);

            return allowedDiscounts;
        }

        /// <summary>
        /// Gets discount amount
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="productPriceWithoutDiscount">Already calculated product price without discount</param>
        /// <returns>Discount amount</returns>
        protected virtual async Task<(double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetDiscountAmount(Product product,
            Customer customer, Currency currency, double productPriceWithoutDiscount)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            List<ApplyDiscount> appliedDiscounts = null;
            double appliedDiscountAmount = 0;

            //we don't apply discounts to products with price entered by a customer
            if (product.EnteredPrice)
                return (appliedDiscountAmount, appliedDiscounts);

            //discounts are disabled
            if (_catalogSettings.IgnoreDiscounts)
                return (appliedDiscountAmount, appliedDiscounts);

            var allowedDiscounts = await GetAllowedDiscounts(product, customer, currency);

            //no discounts
            if (!allowedDiscounts.Any())
                return (appliedDiscountAmount, appliedDiscounts);

            var preferredDiscount = (await _discountService.GetPreferredDiscount(allowedDiscounts, customer, currency, product, productPriceWithoutDiscount));
            appliedDiscounts = preferredDiscount.appliedDiscount;
            appliedDiscountAmount = preferredDiscount.discountAmount;
            return (appliedDiscountAmount, appliedDiscounts);
        }


        #endregion

        #region Methods

        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="currency">The currency</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <returns>Final price</returns>
        public virtual async Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice preferredTierPrice)> GetFinalPrice(
            Product product,
            Customer customer,
            Currency currency,
            double additionalCharge = 0,
            bool includeDiscounts = true,
            int quantity = 1)
        {
            return await GetFinalPrice(product, customer, currency, additionalCharge, includeDiscounts, quantity, null, null);
        }

        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <param name="rentalStartDate">Rental period start date (for rental products)</param>
        /// <param name="rentalEndDate">Rental period end date (for rental products)</param>
        /// <returns>Final price</returns>
        public virtual async Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice preferredTierPrice)> GetFinalPrice(
            Product product,
            Customer customer,
            Currency currency,
            double additionalCharge,
            bool includeDiscounts,
            int quantity,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            double discountAmount = 0;
            var appliedDiscounts = new List<ApplyDiscount>();

            async Task<ProductPrice> PrepareModel()
            {
                var result = new ProductPrice();

                //initial price
                double price =
                    product.ProductPrices.FirstOrDefault(x => x.CurrencyCode == currency.CurrencyCode)?.Price ??
                    await _currencyService.ConvertFromPrimaryStoreCurrency(product.Price, currency);

                //tier prices
                var tierPrice = product.GetPreferredTierPrice(customer, _workContext.CurrentStore.Id, currency.CurrencyCode, quantity);
                if (tierPrice != null)
                {
                    price = tierPrice.Price;
                    result.PreferredTierPrice = tierPrice;
                }

                //customer product price
                if (_catalogSettings.CustomerProductPrice)
                {
                    var customerPrice = await _mediator.Send(new GetPriceByCustomerProductQuery() { CustomerId = customer.Id, ProductId = product.Id });
                    if (customerPrice.HasValue && customerPrice.Value < await _currencyService.ConvertToPrimaryStoreCurrency(price, currency))
                        price = await _currencyService.ConvertFromPrimaryStoreCurrency(customerPrice.Value, currency);
                }

                //additional charge
                price += additionalCharge;

                //reservations
                if (product.ProductTypeId == ProductType.Reservation)
                    if (rentalStartDate.HasValue && rentalEndDate.HasValue)
                    {
                        double d = 0;
                        if (product.IncBothDate)
                        {
                            _ = double.TryParse(((rentalEndDate - rentalStartDate).Value.TotalDays + 1).ToString(), out d);
                        }
                        else
                        {
                            _ = double.TryParse((rentalEndDate - rentalStartDate).Value.TotalDays.ToString(), out d);
                        }
                        price *= d;
                    }

                if (includeDiscounts)
                {
                    //discount
                    var discountamount = await GetDiscountAmount(product, customer, currency, price);
                    double tmpDiscountAmount = discountamount.discountAmount;
                    List<ApplyDiscount> tmpAppliedDiscounts = discountamount.appliedDiscounts;
                    price -= tmpDiscountAmount;

                    if (tmpAppliedDiscounts != null)
                    {
                        result.AppliedDiscounts = tmpAppliedDiscounts.ToList();
                        result.AppliedDiscountAmount = tmpDiscountAmount;
                    }
                }

                if (price < 0)
                    price = 0;

                //rounding
                if (_shoppingCartSettings.RoundPrices)
                {
                    result.Price = RoundingHelper.RoundPrice(price, currency);
                }
                else
                    result.Price = price;

                return result;
            }

            var modelprice = await PrepareModel();

            if (includeDiscounts)
            {
                appliedDiscounts = modelprice.AppliedDiscounts.ToList();
                if (appliedDiscounts.Any())
                {
                    discountAmount = modelprice.AppliedDiscountAmount;
                }
            }

            return (modelprice.Price, discountAmount, appliedDiscounts, modelprice.PreferredTierPrice);
        }


        /// <summary>
        /// Gets the shopping cart unit price
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="product">Product</param>
        /// <param name="includeDiscounts">Include discounts or not for price</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        public virtual async Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetUnitPrice(ShoppingCartItem shoppingCartItem,
            Product product, bool includeDiscounts = true)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            return await GetUnitPrice(product,
                _workContext.CurrentCustomer,
                _workContext.WorkingCurrency,
                shoppingCartItem.ShoppingCartTypeId,
                shoppingCartItem.Quantity,
                shoppingCartItem.Attributes,
                shoppingCartItem.EnteredPrice,
                shoppingCartItem.RentalStartDateUtc,
                shoppingCartItem.RentalEndDateUtc,
                includeDiscounts);
        }
        /// <summary>
        /// Gets the shopping cart unit price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">The currency</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="attributes">Product atrributes</param>
        /// <param name="customerEnteredPrice">Customer entered price</param>
        /// <param name="rentalStartDate">Rental start date</param>
        /// <param name="rentalEndDate">Rental end date</param>
        /// <param name="includeDiscounts">Include discounts or not for price</param>
        /// <returns>Shopping cart unit price</returns>
        public virtual async Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetUnitPrice(
            Product product,
            Customer customer,
            Currency currency,
            ShoppingCartType shoppingCartType,
            int quantity,
            IList<CustomAttribute> attributes,
            double? customerEnteredPrice,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate,
            bool includeDiscounts)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            double discountAmount = 0;
            var appliedDiscounts = new List<ApplyDiscount>();

            double? finalPrice = null;

            if (customerEnteredPrice.HasValue)
                finalPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(customerEnteredPrice.Value, currency);

            if (!finalPrice.HasValue)
            {
                var combination = _productAttributeParser.FindProductAttributeCombination(product, attributes);
                if (combination != null)
                {
                    if (combination.OverriddenPrice.HasValue)
                        finalPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(combination.OverriddenPrice.Value, currency);
                    if (combination.TierPrices.Any())
                    {
                        var storeId = _workContext.CurrentStore.Id;
                        var actualTierPrices = combination.TierPrices.Where(x => string.IsNullOrEmpty(x.StoreId) || x.StoreId == storeId)
                            .Where(x => string.IsNullOrEmpty(x.CustomerGroupId) ||
                            customer.Groups.Contains(x.CustomerGroupId)).ToList();
                        var tierPrice = actualTierPrices.LastOrDefault(price => quantity >= price.Quantity);
                        if (tierPrice != null)
                            finalPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(tierPrice.Price, currency);
                    }
                }
            }
            if (!finalPrice.HasValue)
            {
                //summarize price of all attributes
                double attributesTotalPrice = 0;
                if (attributes != null && attributes.Any())
                {
                    if (product.ProductTypeId != ProductType.BundledProduct)
                    {
                        var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, attributes);
                        if (attributeValues != null)
                        {
                            foreach (var attributeValue in attributeValues)
                            {
                                attributesTotalPrice += await GetProductAttributeValuePriceAdjustment(attributeValue);
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in product.BundleProducts)
                        {
                            var p1 = await _productService.GetProductById(item.ProductId);
                            if (p1 != null)
                            {
                                var attributeValues = _productAttributeParser.ParseProductAttributeValues(p1, attributes);
                                if (attributeValues != null)
                                {
                                    foreach (var attributeValue in attributeValues)
                                    {
                                        attributesTotalPrice += (item.Quantity * await GetProductAttributeValuePriceAdjustment(attributeValue));
                                    }
                                }
                            }
                        }
                    }
                }

                if (product.EnteredPrice)
                {
                    finalPrice = customerEnteredPrice;
                }
                else
                {
                    int qty = 0;
                    if (_shoppingCartSettings.GroupTierPrices)
                    {
                        qty = customer.ShoppingCartItems
                            .Where(x => x.ProductId == product.Id)
                            .Where(x => x.ShoppingCartTypeId == shoppingCartType)
                            .Sum(x => x.Quantity);
                    }
                    if (qty == 0)
                        qty = quantity;

                    var getfinalPrice = await GetFinalPrice(product,
                        customer,
                        currency,
                        attributesTotalPrice,
                        includeDiscounts,
                        qty,
                        rentalStartDate,
                        rentalEndDate);

                    finalPrice = getfinalPrice.finalPrice;
                    discountAmount = getfinalPrice.discountAmount;
                    appliedDiscounts = getfinalPrice.appliedDiscounts;
                }
            }

            if (!finalPrice.HasValue)
                finalPrice = 0;

            //rounding
            if (_shoppingCartSettings.RoundPrices)
            {
                finalPrice = RoundingHelper.RoundPrice(finalPrice.Value, _workContext.WorkingCurrency);
            }
            return (finalPrice.Value, discountAmount, appliedDiscounts);
        }

        /// <summary>
        /// Gets the shopping cart item sub total
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="product">Product</param>
        /// <param name="includeDiscounts">Include discounts or not for price</param>
        /// <returns>Shopping cart item sub total</returns>
        public virtual async Task<(double subTotal, double discountAmount, List<ApplyDiscount> appliedDiscounts)> GetSubTotal(ShoppingCartItem shoppingCartItem, Product product,
           bool includeDiscounts = true)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            double subTotal;
            //unit price
            var getunitPrice = await GetUnitPrice(shoppingCartItem, product, includeDiscounts);
            var unitPrice = getunitPrice.unitprice;
            double discountAmount = getunitPrice.discountAmount;
            List<ApplyDiscount> appliedDiscounts = getunitPrice.appliedDiscounts;

            //discount
            if (appliedDiscounts.Any())
            {
                Discount oneAndOnlyDiscount = null;
                if (appliedDiscounts.Count == 1)
                    oneAndOnlyDiscount = await _discountService.GetDiscountById(appliedDiscounts.FirstOrDefault().DiscountId);

                if (oneAndOnlyDiscount != null &&
                    oneAndOnlyDiscount.MaximumDiscountedQuantity.HasValue &&
                    shoppingCartItem.Quantity > oneAndOnlyDiscount.MaximumDiscountedQuantity.Value)
                {
                    //we cannot apply discount for all shopping cart items
                    var discountedQuantity = oneAndOnlyDiscount.MaximumDiscountedQuantity.Value;
                    var discountedSubTotal = unitPrice * discountedQuantity;
                    discountAmount *= discountedQuantity;

                    var notDiscountedQuantity = shoppingCartItem.Quantity - discountedQuantity;
                    var notDiscountedUnitPrice = await GetUnitPrice(shoppingCartItem, product, false);
                    var notDiscountedSubTotal = notDiscountedUnitPrice.unitprice * notDiscountedQuantity;

                    subTotal = discountedSubTotal + notDiscountedSubTotal;
                }
                else
                {
                    //discount is applied to all items (quantity)
                    discountAmount *= shoppingCartItem.Quantity;

                    subTotal = unitPrice * shoppingCartItem.Quantity;
                }
            }
            else
            {
                subTotal = unitPrice * shoppingCartItem.Quantity;
            }
            return (subTotal, discountAmount, appliedDiscounts);
        }



        /// <summary>
        /// Gets the product cost (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Shopping cart item attributes</param>
        /// <returns>Product cost (one item)</returns>
        public virtual async Task<double> GetProductCost(Product product, IList<CustomAttribute> attributes)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            double cost = product.ProductCost;
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, attributes);
            foreach (var attributeValue in attributeValues)
            {
                switch (attributeValue.AttributeValueTypeId)
                {
                    case AttributeValueType.Simple:
                        {
                            //simple attribute
                            cost += attributeValue.Cost;
                        }
                        break;
                    case AttributeValueType.AssociatedToProduct:
                        {
                            //bundled product
                            var associatedProduct = await _productService.GetProductById(attributeValue.AssociatedProductId);
                            if (associatedProduct != null)
                                cost += associatedProduct.ProductCost * attributeValue.Quantity;
                        }
                        break;
                    default:
                        break;
                }
            }

            return cost;
        }



        /// <summary>
        /// Get a price adjustment of a product attribute value
        /// </summary>
        /// <param name="value">Product attribute value</param>
        /// <returns>Price adjustment</returns>
        public virtual async Task<double> GetProductAttributeValuePriceAdjustment(ProductAttributeValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            double adjustment = 0;
            switch (value.AttributeValueTypeId)
            {
                case AttributeValueType.Simple:
                    {
                        //simple attribute
                        adjustment = value.PriceAdjustment;
                        if (adjustment > 0)
                            adjustment = await _currencyService.ConvertFromPrimaryStoreCurrency(adjustment, _workContext.WorkingCurrency);
                    }
                    break;
                case AttributeValueType.AssociatedToProduct:
                    {
                        //bundled product
                        var associatedProduct = await _productService.GetProductById(value.AssociatedProductId);
                        if (associatedProduct != null)
                        {
                            adjustment = (await GetFinalPrice(associatedProduct, _workContext.CurrentCustomer, _workContext.WorkingCurrency,
                                additionalCharge: value.PriceAdjustment, includeDiscounts: true)).finalPrice * value.Quantity;
                        }
                    }
                    break;
                default:
                    break;
            }

            return adjustment;
        }

        #endregion
    }
}
