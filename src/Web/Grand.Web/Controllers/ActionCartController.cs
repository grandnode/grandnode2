using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Grand.Web.Controllers;

[DenySystemAccount]
public class ActionCartController : BasePublicController
{
    #region CTOR

    public ActionCartController(IProductService productService,
        IShoppingCartService shoppingCartService,
        IWorkContext workContext,
        IGroupService groupService,
        ITranslationService translationService,
        ICurrencyService currencyService,
        IShoppingCartValidator shoppingCartValidator,
        IMediator mediator,
        ShoppingCartSettings shoppingCartSettings)
    {
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _workContext = workContext;
        _groupService = groupService;
        _translationService = translationService;
        _currencyService = currencyService;
        _shoppingCartValidator = shoppingCartValidator;
        _mediator = mediator;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IWorkContext _workContext;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly ICurrencyService _currencyService;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly IMediator _mediator;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Private methods

    private IActionResult RedirectToProduct(Product product, ShoppingCartType cartType, int quantity)
    {
        //we can't add grouped products 
        if (product.ProductTypeId == ProductType.GroupedProduct)
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
            });

        switch (cartType)
        {
            //products with "minimum order quantity" more than a specified qty
            case ShoppingCartType.ShoppingCart when product.OrderMinimumQuantity > quantity:
                //we cannot add to the cart such products from category pages
                return Json(new {
                    redirect = Url.RouteUrl("Product",
                        new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
                });
            case ShoppingCartType.ShoppingCart when product.EnteredPrice:
                //cannot be added to the cart (requires a customer to enter price)
                return Json(new {
                    redirect = Url.RouteUrl("Product",
                        new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
                });
        }

        var allowedQuantities = product.ParseAllowedQuantities();
        if (cartType == ShoppingCartType.ShoppingCart && allowedQuantities.Length > 0)
            //cannot be added to the cart (requires a customer to select a quantity from drop down list)
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
            });

        if (cartType != ShoppingCartType.Wishlist && product.ProductAttributeMappings.Any())
            //product has some attributes
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
            });

        return null;
    }

    private string GetWarehouse(Product product)
    {
        return product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
            string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId :
            _workContext.CurrentStore.DefaultWarehouseId;
    }

    private IActionResult ReturnFailMessage(Product product, ShoppingCartType shoppingCartTypeId)
    {
        switch (product.ProductTypeId)
        {
            //you can't add group products
            case ProductType.GroupedProduct:
                return Json(new {
                    success = false,
                    message = "Grouped products couldn't be added to the cart"
                });
            //you can't add reservation product to wishlist
            case ProductType.Reservation when shoppingCartTypeId == ShoppingCartType.Wishlist:
                return Json(new {
                    success = false,
                    message = "Reservation products couldn't be added to the wishlist"
                });
            //you can't add auction product to wishlist
            case ProductType.Auction when shoppingCartTypeId == ShoppingCartType.Wishlist:
                return Json(new {
                    success = false,
                    message = "Auction products couldn't be added to the wishlist"
                });
        }

        //check available date
        if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            return Json(new {
                success = false,
                message = _translationService.GetResource("ShoppingCart.NotAvailable")
            });

        return null;
    }

    private async Task<double?> GetCustomerEnteredPrice(ProductDetailsCart model)
    {
        double? customerEnteredPriceConverted = null;
        if (double.TryParse(model.CustomerEnteredPrice, out var customerEnteredPrice))
            customerEnteredPriceConverted =
                await _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice,
                    _workContext.WorkingCurrency);

        return customerEnteredPriceConverted;
    }

    #endregion

    #region Public methods

    [HttpPost]
    public virtual async Task<IActionResult> AddProductCatalog(ProductCatalogCart model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            //no product found
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        var redirect = RedirectToProduct(product, model.ShoppingCartTypeId, model.Quantity);
        if (redirect != null)
            return redirect;

        var customer = _workContext.CurrentCustomer;

        var warehouseId = GetWarehouse(product);

        var cart = await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, model.ShoppingCartTypeId);

        if (model.ShoppingCartTypeId != ShoppingCartType.Wishlist)
        {
            var shoppingCartItem =
                await _shoppingCartService.FindShoppingCartItem(cart, model.ShoppingCartTypeId, product.Id,
                    warehouseId);

            //if we already have the same product in the cart, then use the total quantity to validate
            var quantityToValidate = shoppingCartItem?.Quantity + model.Quantity ?? model.Quantity;
            var addToCartWarnings = await _shoppingCartValidator
                .GetShoppingCartItemWarnings(customer, new ShoppingCartItem {
                    ShoppingCartTypeId = model.ShoppingCartTypeId,
                    StoreId = _workContext.CurrentStore.Id,
                    WarehouseId = warehouseId,
                    Quantity = quantityToValidate
                }, product, new ShoppingCartValidatorOptions {
                    GetRequiredProductWarnings = false
                });

            if (addToCartWarnings.Any())
                //cannot be added to the cart
                return Json(new {
                    redirect = Url.RouteUrl("Product",
                        new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
                });
        }

        //try adding product to the cart 
        var addToCart = await _shoppingCartService.AddToCart(customer,
            product.Id,
            model.ShoppingCartTypeId,
            _workContext.CurrentStore.Id,
            warehouseId,
            quantity: model.Quantity,
            validator: new ShoppingCartValidatorOptions {
                GetRequiredProductWarnings = false,
                GetInventoryWarnings = model.ShoppingCartTypeId == ShoppingCartType.ShoppingCart ||
                                       !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist,
                GetAttributesWarnings = model.ShoppingCartTypeId != ShoppingCartType.Wishlist,
                GetGiftVoucherWarnings = model.ShoppingCartTypeId != ShoppingCartType.Wishlist
            });

        if (addToCart.warnings.Any())
            //cannot be added to the cart
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
            });

        var addtoCartModel = await _mediator.Send(new GetAddToCart {
            Product = product,
            Customer = customer,
            ShoppingCartItem = addToCart.shoppingCartItem,
            Quantity = model.Quantity,
            CartType = model.ShoppingCartTypeId,
            Currency = _workContext.WorkingCurrency,
            Store = _workContext.CurrentStore,
            Language = _workContext.WorkingLanguage,
            TaxDisplayType = _workContext.TaxDisplayType
        });

        //added to the cart/wishlist
        switch (model.ShoppingCartTypeId)
        {
            case ShoppingCartType.Wishlist:
            {
                if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct || model.ForceRedirection)
                    //redirect to the wishlist page
                    return Json(new {
                        redirect = Url.RouteUrl("Wishlist")
                    });

                //display notification message and update appropriate blocks
                var qty = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id,
                    ShoppingCartType.Wishlist)).Sum(x => x.Quantity);
                var updatetopwishlistsectionhtml =
                    string.Format(_translationService.GetResource("Wishlist.HeaderQuantity"), qty);

                return Json(new {
                    success = true,
                    message = string.Format(
                        _translationService.GetResource("Products.ProductHasBeenAddedToTheWishlist.Link"),
                        Url.RouteUrl("Wishlist")),
                    updatetopwishlistsectionhtml,
                    wishlistqty = qty,
                    model = addtoCartModel
                });
            }
            case ShoppingCartType.ShoppingCart:
            default:
            {
                if (_shoppingCartSettings.DisplayCartAfterAddingProduct || model.ForceRedirection)
                    //redirect to the shopping cart page
                    return Json(new {
                        redirect = Url.RouteUrl("ShoppingCart")
                    });

                //display notification message and update appropriate blocks
                var shoppingCartTypes = new List<ShoppingCartType> {
                    ShoppingCartType.ShoppingCart,
                    ShoppingCartType.Auctions
                };
                if (_shoppingCartSettings.AllowOnHoldCart)
                    shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

                var updatetopcartsectionhtml = string.Format(
                    _translationService.GetResource("ShoppingCart.HeaderQuantity"),
                    (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id,
                        shoppingCartTypes.ToArray()))
                    .Sum(x => x.Quantity));

                var miniShoppingCartmodel = _shoppingCartSettings.MiniShoppingCartEnabled
                    ? await _mediator.Send(new GetMiniShoppingCart {
                        Customer = _workContext.CurrentCustomer,
                        Currency = _workContext.WorkingCurrency,
                        Language = _workContext.WorkingLanguage,
                        TaxDisplayType = _workContext.TaxDisplayType,
                        Store = _workContext.CurrentStore
                    })
                    : null;

                return Json(new {
                    success = true,
                    message = string.Format(
                        _translationService.GetResource("Products.ProductHasBeenAddedToTheCart.Link"),
                        Url.RouteUrl("ShoppingCart")),
                    updatetopcartsectionhtml,
                    sidebarshoppingcartmodel = miniShoppingCartmodel,
                    model = addtoCartModel
                });
            }
        }
    }

    [HttpPost]
    public virtual async Task<IActionResult> AddProductDetails(ProductDetailsCart model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            return Json(new {
                redirect = Url.RouteUrl("HomePage")
            });

        var message = ReturnFailMessage(product, model.ShoppingCartTypeId);
        if (message != null)
            return message;

        double? customerEnteredPriceConverted = null;
        if (product.EnteredPrice) customerEnteredPriceConverted = await GetCustomerEnteredPrice(model);
        //product attributes
        var attributes = await _mediator.Send(new GetParseProductAttributes
            { Product = product, Attributes = model.Attributes });
        //gift voucher 
        if (product.IsGiftVoucher)
            attributes = GiftVoucherExtensions.AddGiftVoucherAttribute(attributes,
                model.RecipientName,
                model.RecipientEmail,
                model.SenderName,
                model.SenderEmail,
                model.Message).ToList();
        //rental attributes
        DateTime? rentalStartDate = null;
        DateTime? rentalEndDate = null;
        if (product.ProductTypeId == ProductType.Reservation)
            product.ParseReservationDates(model.ReservationDatepickerFrom, model.ReservationDatepickerTo,
                out rentalStartDate, out rentalEndDate);

        //product reservation
        var parameter = "";
        var duration = "";
        if (product.ProductTypeId == ProductType.Reservation)
            switch (product.IntervalUnitId)
            {
                case IntervalUnit.Hour or IntervalUnit.Minute when string.IsNullOrEmpty(model.Reservation):
                    return Json(new {
                        success = false,
                        message = _translationService.GetResource("Product.Addtocart.Reservation.Required")
                    });
                case IntervalUnit.Hour or IntervalUnit.Minute:
                {
                    var productReservationService =
                        HttpContext.RequestServices.GetRequiredService<IProductReservationService>();
                    var reservation = await productReservationService.GetProductReservation(model.Reservation);
                    if (reservation == null)
                        return Json(new {
                            success = false,
                            message = "No reservation found"
                        });
                    duration = reservation.Duration;
                    rentalStartDate = reservation.Date;
                    parameter = reservation.Parameter;
                    break;
                }
                case IntervalUnit.Day:
                {
                    const string datePickerFormat = "MM/dd/yyyy";
                    if (!string.IsNullOrEmpty(model.ReservationDatepickerFrom))
                        rentalStartDate = DateTime.ParseExact(model.ReservationDatepickerFrom, datePickerFormat,
                            CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(model.ReservationDatepickerTo))
                        rentalEndDate = DateTime.ParseExact(model.ReservationDatepickerTo, datePickerFormat,
                            CultureInfo.InvariantCulture);

                    break;
                }
            }

        //save item
        var addToCartWarnings = new List<string>();
        var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ? model.WarehouseId :
            product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
            string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId :
            _workContext.CurrentStore.DefaultWarehouseId;


        //add to the cart
        var (warnings, shoppingCartItem) = await _shoppingCartService.AddToCart(_workContext.CurrentCustomer,
            product.Id, model.ShoppingCartTypeId, _workContext.CurrentStore.Id, warehouseId,
            attributes, customerEnteredPriceConverted,
            rentalStartDate, rentalEndDate, model.EnteredQuantity, true, model.Reservation, parameter, duration,
            new ShoppingCartValidatorOptions {
                GetRequiredProductWarnings = false,
                GetInventoryWarnings = model.ShoppingCartTypeId == ShoppingCartType.ShoppingCart ||
                                       !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist
            });

        addToCartWarnings.AddRange(warnings);

        #region Return result

        if (addToCartWarnings.Any())
            //cannot be added to the cart/wishlist
            //display warnings
            return Json(new {
                success = false,
                message = addToCartWarnings.ToArray()
            });

        var addtoCartModel = await _mediator.Send(new GetAddToCart {
            Product = product,
            Customer = _workContext.CurrentCustomer,
            ShoppingCartItem = shoppingCartItem,
            Quantity = model.EnteredQuantity,
            CartType = model.ShoppingCartTypeId,
            CustomerEnteredPrice = customerEnteredPriceConverted,
            Attributes = attributes,
            Currency = _workContext.WorkingCurrency,
            Store = _workContext.CurrentStore,
            Language = _workContext.WorkingLanguage,
            TaxDisplayType = _workContext.TaxDisplayType,
            Duration = duration,
            Parameter = parameter,
            ReservationId = model.Reservation,
            StartDate = rentalStartDate,
            EndDate = rentalEndDate
        });

        //added to the cart/wishlist
        switch (model.ShoppingCartTypeId)
        {
            case ShoppingCartType.Wishlist:
            {
                if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct)
                    //redirect to the wishlist page
                    return Json(new {
                        redirect = Url.RouteUrl("Wishlist")
                    });

                //display notification message and update appropriate blocks
                var qty = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id,
                    ShoppingCartType.Wishlist)).Sum(x => x.Quantity);
                var updatetopwishlistsectionhtml =
                    string.Format(_translationService.GetResource("Wishlist.HeaderQuantity"), qty);

                return Json(new {
                    success = true,
                    message = string.Format(
                        _translationService.GetResource("Products.ProductHasBeenAddedToTheWishlist.Link"),
                        Url.RouteUrl("Wishlist")),
                    updatetopwishlistsectionhtml,
                    wishlistqty = qty,
                    model = addtoCartModel
                });
            }
            case ShoppingCartType.ShoppingCart:
            default:
            {
                if (_shoppingCartSettings.DisplayCartAfterAddingProduct)
                    //redirect to the shopping cart page
                    return Json(new {
                        redirect = Url.RouteUrl("ShoppingCart")
                    });

                //display notification message and update appropriate blocks
                var shoppingCartTypes = new List<ShoppingCartType> {
                    ShoppingCartType.ShoppingCart,
                    ShoppingCartType.Auctions
                };
                if (_shoppingCartSettings.AllowOnHoldCart)
                    shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

                var updatetopcartsectionhtml = string.Format(
                    _translationService.GetResource("ShoppingCart.HeaderQuantity"),
                    (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id,
                        shoppingCartTypes.ToArray()))
                    .Sum(x => x.Quantity));

                var miniShoppingCartmodel = _shoppingCartSettings.MiniShoppingCartEnabled
                    ? await _mediator.Send(new GetMiniShoppingCart {
                        Customer = _workContext.CurrentCustomer,
                        Currency = _workContext.WorkingCurrency,
                        Language = _workContext.WorkingLanguage,
                        TaxDisplayType = _workContext.TaxDisplayType,
                        Store = _workContext.CurrentStore
                    })
                    : null;

                return Json(new {
                    success = true,
                    message = string.Format(
                        _translationService.GetResource("Products.ProductHasBeenAddedToTheCart.Link"),
                        Url.RouteUrl("ShoppingCart")),
                    updatetopcartsectionhtml,
                    sidebarshoppingcartmodel = miniShoppingCartmodel,
                    refreshreservation = product.ProductTypeId == ProductType.Reservation &&
                                         product.IntervalUnitId != IntervalUnit.Day,
                    model = addtoCartModel
                });
            }
        }

        #endregion
    }

    [HttpPost]
    public virtual async Task<IActionResult> UpdateItemCart(ProductDetailsCart model)
    {
        var cart = _workContext.CurrentCustomer.ShoppingCartItems.FirstOrDefault(sci =>
            sci.Id == model.ShoppingCartItemId);
        if (cart == null)
            return Json(new {
                success = false,
                message = "No item cart found with the specified ID"
            });

        var product = await _productService.GetProductById(cart.ProductId);
        if (product == null)
            return Json(new {
                redirect = Url.RouteUrl("HomePage")
            });

        switch (product.ProductTypeId)
        {
            //you can't add group products
            case ProductType.GroupedProduct:
                return Json(new {
                    success = false,
                    message = "Grouped products couldn't be added to the cart"
                });
            //you can't add reservation product to wishlist
            case ProductType.Reservation when cart.ShoppingCartTypeId == ShoppingCartType.Wishlist:
                return Json(new {
                    success = false,
                    message = "Reservation products couldn't be added to the wishlist"
                });
            //you can't add auction product to wishlist
            case ProductType.Auction:
                return Json(new {
                    success = false,
                    message = "Auction products couldn't be added to the wishlist"
                });
        }

        //check available date
        if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            return Json(new {
                success = false,
                message = _translationService.GetResource("ShoppingCart.NotAvailable")
            });

        #region Customer entered price

        double? customerEnteredPriceConverted = null;
        if (product.EnteredPrice)
            if (double.TryParse(model.CustomerEnteredPrice, out var customerEnteredPrice))
                customerEnteredPriceConverted =
                    await _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice,
                        _workContext.WorkingCurrency);

        #endregion

        //product attributes
        var attributes = await _mediator.Send(new GetParseProductAttributes
            { Product = product, Attributes = model.Attributes });

        //gift voucher 
        if (product.IsGiftVoucher)
            attributes = GiftVoucherExtensions.AddGiftVoucherAttribute(attributes,
                model.RecipientName,
                model.RecipientEmail,
                model.SenderName,
                model.SenderEmail,
                model.Message).ToList();

        //rental attributes
        var rentalStartDate = cart.RentalStartDateUtc;
        var rentalEndDate = cart.RentalEndDateUtc;
        if (product.ProductTypeId == ProductType.Reservation)
            product.ParseReservationDates(model.ReservationDatepickerFrom, model.ReservationDatepickerTo,
                out rentalStartDate, out rentalEndDate);

        //product reservation
        if (product.ProductTypeId == ProductType.Reservation)
            switch (product.IntervalUnitId)
            {
                case IntervalUnit.Hour or IntervalUnit.Minute when string.IsNullOrEmpty(model.Reservation):
                    return Json(new {
                        success = false,
                        message = _translationService.GetResource("Product.Addtocart.Reservation.Required")
                    });
                case IntervalUnit.Hour or IntervalUnit.Minute:
                {
                    var productReservationService =
                        HttpContext.RequestServices.GetRequiredService<IProductReservationService>();
                    var reservation = await productReservationService.GetProductReservation(model.Reservation);
                    if (reservation == null)
                        return Json(new {
                            success = false,
                            message = "No reservation found"
                        });

                    rentalStartDate = reservation.Date;
                    break;
                }
                case IntervalUnit.Day:
                {
                    const string datePickerFormat = "MM/dd/yyyy";
                    if (!string.IsNullOrEmpty(model.ReservationDatepickerFrom))
                        rentalStartDate = DateTime.ParseExact(model.ReservationDatepickerFrom, datePickerFormat,
                            CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(model.ReservationDatepickerTo))
                        rentalEndDate = DateTime.ParseExact(model.ReservationDatepickerTo, datePickerFormat,
                            CultureInfo.InvariantCulture);

                    break;
                }
            }

        //save item
        var addToCartWarnings = new List<string>();

        var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ? model.WarehouseId : cart.WarehouseId;

        //update existing item
        addToCartWarnings.AddRange(await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
            cart.Id, warehouseId, attributes, customerEnteredPriceConverted,
            rentalStartDate, rentalEndDate, model.EnteredQuantity));

        if (addToCartWarnings.Any())
            //cannot be updated the cart/wishlist
            return Json(new {
                success = false,
                message = addToCartWarnings.ToArray()
            });

        return Json(new {
            success = true,
            message = ""
        });
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetItemCart(string shoppingCartId)
    {
        var cart = _workContext.CurrentCustomer.ShoppingCartItems.FirstOrDefault(sci => sci.Id == shoppingCartId);
        if (cart == null)
            return Json(new {
                success = false,
                message = "No item cart found with the specified ID"
            });

        var product = await _productService.GetProductById(cart.ProductId);
        if (product == null)
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });


        //availability dates
        if (!product.IsAvailable() && product.ProductTypeId != ProductType.Auction)
            return Json(new {
                success = false,
                message = "No product found with the specified ID"
            });

        //visible individually?
        if (!product.VisibleIndividually)
        {
            //is this one an associated products?
            var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
            if (parentGroupedProduct == null)
                return Json(new {
                    redirect = Url.RouteUrl("HomePage")
                });
            return Json(new {
                redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) })
            });
        }

        //prepare the model
        var model = await _mediator.Send(new GetProductDetailsPage {
            Store = _workContext.CurrentStore,
            Product = product,
            UpdateCartItem = cart
        });

        return Json(new {
            success = true, model
        });
    }

    [HttpPost]
    public virtual async Task<IActionResult> AddBid(AddBidModel model,
        [FromServices] IAuctionService auctionService)
    {
        var customer = _workContext.CurrentCustomer;
        if (!await _groupService.IsRegistered(customer))
            return Json(new {
                success = false,
                message = _translationService.GetResource("ShoppingCart.Mustberegisteredtobid")
            });
        double.TryParse(model.HighestBidValue, out var bid);
        if (bid == 0)
            double.TryParse(model.HighestBidValue, NumberStyles.AllowDecimalPoint,
                CultureInfo.GetCultureInfo("en-US").NumberFormat, out bid);

        bid = Math.Round(bid, 2);

        if (bid <= 0)
            return Json(new {
                success = false,
                message = _translationService.GetResource("ShoppingCart.BidMustBeHigher")
            });

        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (product.HighestBidder == customer.Id)
            return Json(new {
                success = false,
                message = _translationService.GetResource("ShoppingCart.AlreadyHighestBidder")
            });

        var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse
            ? model.WarehouseId
            : _workContext.CurrentStore.DefaultWarehouseId;

        var shoppingCartItem = new ShoppingCartItem {
            Attributes = new List<CustomAttribute>(),
            CreatedOnUtc = DateTime.UtcNow,
            ProductId = product.Id,
            WarehouseId = warehouseId,
            ShoppingCartTypeId = ShoppingCartType.Auctions,
            StoreId = _workContext.CurrentStore.Id,
            EnteredPrice = bid,
            Quantity = 1
        };

        var warnings = (await _shoppingCartValidator.GetStandardWarnings(customer, product, shoppingCartItem)).ToList();
        warnings.AddRange(await _shoppingCartValidator.GetAuctionProductWarning(bid, product, customer));

        if (warnings.Any())
        {
            var toReturn = "";
            foreach (var warning in warnings) toReturn += warning + "</br>";

            return Json(new {
                success = false,
                message = toReturn
            });
        }

        //insert new bid
        await auctionService.NewBid(customer, product, _workContext.CurrentStore, _workContext.WorkingLanguage,
            warehouseId, bid);

        var addtoCartModel = await _mediator.Send(new GetAddToCart {
            Product = product,
            Customer = customer,
            Quantity = 1,
            CartType = ShoppingCartType.Auctions,
            Currency = _workContext.WorkingCurrency,
            Store = _workContext.CurrentStore,
            Language = _workContext.WorkingLanguage,
            TaxDisplayType = _workContext.TaxDisplayType
        });

        return Json(new {
            success = true,
            message = _translationService.GetResource("ShoppingCart.Yourbidhasbeenplaced"),
            model = addtoCartModel
        });
    }

    #endregion
}