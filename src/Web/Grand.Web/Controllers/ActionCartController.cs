using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Filters;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Grand.Web.Controllers
{
    [DenySystemAccount]
    public partial class ActionCartController : BasePublicController
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly ICurrencyService _currencyService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IMediator _mediator;

        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region CTOR

        public ActionCartController(IProductService productService,
            IShoppingCartService shoppingCartService,
            IWorkContext workContext,
            IGroupService groupService,
            ITranslationService translationService,
            ICurrencyService currencyService,
            IShoppingCartValidator shoppingCartValidator,
            ICustomerActivityService customerActivityService,
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
            _customerActivityService = customerActivityService;
            _mediator = mediator;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        protected IActionResult RedirectToProduct(Product product, ShoppingCartType cartType, int quantity)
        {
            //we can't add grouped products 
            if (product.ProductTypeId == ProductType.GroupedProduct)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            //products with "minimum order quantity" more than a specified qty
            if (cartType == ShoppingCartType.ShoppingCart && product.OrderMinimumQuantity > quantity)
            {
                //we cannot add to the cart such products from category pages
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            if (cartType == ShoppingCartType.ShoppingCart && product.EnteredPrice)
            {
                //cannot be added to the cart (requires a customer to enter price)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }
            var allowedQuantities = product.ParseAllowedQuantities();
            if (cartType == ShoppingCartType.ShoppingCart && allowedQuantities.Length > 0)
            {
                //cannot be added to the cart (requires a customer to select a quantity from dropdownlist)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            if (cartType != ShoppingCartType.Wishlist && product.ProductAttributeMappings.Any())
            {
                //product has some attributes
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            return null;
        }

        protected string GetWarehouse(Product product)
        {
            return product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
               (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddProductCatalog(string productId, int shoppingCartTypeId,
            int quantity, bool forceredirection = false)
        {
            var cartType = (ShoppingCartType)shoppingCartTypeId;

            var product = await _productService.GetProductById(productId);
            if (product == null)
                //no product found
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            var redirect = RedirectToProduct(product, cartType, quantity);
            if (redirect != null)
                return redirect;

            var customer = _workContext.CurrentCustomer;

            string warehouseId = GetWarehouse(product);

            var cart = await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, cartType);

            if (cartType != ShoppingCartType.Wishlist)
            {
                var shoppingCartItem = await _shoppingCartService.FindShoppingCartItem(cart, cartType, product.Id, warehouseId);

                //if we already have the same product in the cart, then use the total quantity to validate
                var quantityToValidate = shoppingCartItem != null ? shoppingCartItem.Quantity + quantity : quantity;
                var addToCartWarnings = await _shoppingCartValidator
                  .GetShoppingCartItemWarnings(customer, new ShoppingCartItem() {
                      ShoppingCartTypeId = cartType,
                      StoreId = _workContext.CurrentStore.Id,
                      WarehouseId = warehouseId,
                      Quantity = quantityToValidate
                  }, product, new ShoppingCartValidatorOptions() {
                      GetRequiredProductWarnings = false
                  });

                if (addToCartWarnings.Any())
                {
                    //cannot be added to the cart
                    return Json(new
                    {
                        redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                    });
                }
            }

            //try adding product to the cart 
            var addToCart = await _shoppingCartService.AddToCart(customer: customer,
                productId: productId,
                shoppingCartType: cartType,
                storeId: _workContext.CurrentStore.Id,
                warehouseId: warehouseId,
                quantity: quantity,
                validator: new ShoppingCartValidatorOptions() {
                    GetRequiredProductWarnings = false,
                    GetInventoryWarnings = (cartType == ShoppingCartType.ShoppingCart || !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist),
                    GetAttributesWarnings = (cartType != ShoppingCartType.Wishlist),
                    GetGiftVoucherWarnings = (cartType != ShoppingCartType.Wishlist)
                });

            if (addToCart.warnings.Any())
            {
                //cannot be added to the cart
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            var addtoCartModel = await _mediator.Send(new GetAddToCart() {
                Product = product,
                Customer = customer,
                ShoppingCartItem = addToCart.shoppingCartItem,
                Quantity = quantity,
                CartType = cartType,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
            });

            //added to the cart/wishlist
            switch (cartType)
            {
                case ShoppingCartType.Wishlist:
                    {
                        //activity log
                        _ = _customerActivityService.InsertActivity("PublicStore.AddToWishlist", product.Id,
                            _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                            _translationService.GetResource("ActivityLog.PublicStore.AddToWishlist"), product.Name);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct || forceredirection)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var qty = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist)).Sum(x => x.Quantity);
                        var updatetopwishlistsectionhtml = string.Format(_translationService.GetResource("Wishlist.HeaderQuantity"), qty);

                        return Json(new
                        {
                            success = true,
                            message = string.Format(_translationService.GetResource("Products.ProductHasBeenAddedToTheWishlist.Link"), Url.RouteUrl("Wishlist")),
                            updatetopwishlistsectionhtml = updatetopwishlistsectionhtml,
                            wishlistqty = qty,
                            model = addtoCartModel
                        });
                    }
                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        //activity log
                        _ = _customerActivityService.InsertActivity("PublicStore.AddToShoppingCart", product.Id,
                            _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                            _translationService.GetResource("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

                        if (_shoppingCartSettings.DisplayCartAfterAddingProduct || forceredirection)
                        {
                            //redirect to the shopping cart page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("ShoppingCart"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCartTypes = new List<ShoppingCartType>();
                        shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
                        shoppingCartTypes.Add(ShoppingCartType.Auctions);
                        if (_shoppingCartSettings.AllowOnHoldCart)
                            shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

                        var updatetopcartsectionhtml = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"),
                            (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray()))
                                .Sum(x => x.Quantity));

                        var miniShoppingCartmodel = _shoppingCartSettings.MiniShoppingCartEnabled ? await _mediator.Send(new GetMiniShoppingCart() {
                            Customer = _workContext.CurrentCustomer,
                            Currency = _workContext.WorkingCurrency,
                            Language = _workContext.WorkingLanguage,
                            TaxDisplayType = _workContext.TaxDisplayType,
                            Store = _workContext.CurrentStore
                        }) : null;

                        return Json(new
                        {
                            success = true,
                            message = string.Format(_translationService.GetResource("Products.ProductHasBeenAddedToTheCart.Link"), Url.RouteUrl("ShoppingCart")),
                            updatetopcartsectionhtml = updatetopcartsectionhtml,
                            sidebarshoppingcartmodel = miniShoppingCartmodel,
                            model = addtoCartModel
                        });
                    }
            }
        }

        protected IActionResult ReturnFailMessage(Product product, ShoppingCartType shoppingCartTypeId)
        {
            //you can't add group products
            if (product.ProductTypeId == ProductType.GroupedProduct)
            {
                return Json(new
                {
                    success = false,
                    message = "Grouped products couldn't be added to the cart"
                });
            }

            //you can't add reservation product to wishlist
            if (product.ProductTypeId == ProductType.Reservation && (ShoppingCartType)shoppingCartTypeId == ShoppingCartType.Wishlist)
            {
                return Json(new
                {
                    success = false,
                    message = "Reservation products couldn't be added to the wishlist"
                });
            }

            //you can't add auction product to wishlist
            if (product.ProductTypeId == ProductType.Auction && (ShoppingCartType)shoppingCartTypeId == ShoppingCartType.Wishlist)
            {
                return Json(new
                {
                    success = false,
                    message = "Auction products couldn't be added to the wishlist"
                });
            }
            //check available date
            if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            {
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.NotAvailable")
                });
            }

            return null;
        }

        private async Task<double?> GetCustomerEnteredPrice(IFormCollection form, string productId)
        {
            double? customerEnteredPriceConverted = null;
            foreach (string formKey in form.Keys)
            {
                if (formKey.Equals(string.Format("addtocart_{0}.CustomerEnteredPrice", productId), StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(form[formKey], out double customerEnteredPrice))
                        customerEnteredPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice, _workContext.WorkingCurrency);
                    break;
                }
            }
            return customerEnteredPriceConverted;
        }

        private int GetQuantity(IFormCollection form, string productId)
        {
            int quantity = 1;
            foreach (string formKey in form.Keys)
                if (formKey.Equals(string.Format("addtocart_{0}.EnteredQuantity", productId), StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(form[formKey], out quantity);
                    break;
                }

            return quantity;
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddProductDetails(string productId, int shoppingCartTypeId, IFormCollection form)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("HomePage"),
                });
            }

            var message = ReturnFailMessage(product, (ShoppingCartType)shoppingCartTypeId);
            if (message != null)
                return message;

            double? customerEnteredPriceConverted = null;
            if (product.EnteredPrice)
            {
                customerEnteredPriceConverted = await GetCustomerEnteredPrice(form, productId);
            }

            var quantity = GetQuantity(form, productId);

            //product and gift voucher attributes
            var attributes = await _mediator.Send(new GetParseProductAttributes() { Product = product, Form = form });

            //rental attributes
            DateTime? rentalStartDate = null;
            DateTime? rentalEndDate = null;
            if (product.ProductTypeId == ProductType.Reservation)
            {
                product.ParseReservationDates(form, out rentalStartDate, out rentalEndDate);
            }

            //product reservation
            string reservationId = "";
            string parameter = "";
            string duration = "";
            if (product.ProductTypeId == ProductType.Reservation)
            {
                foreach (string formKey in form.Keys)
                {
                    if (formKey.Contains("Reservation"))
                    {
                        reservationId = form["Reservation"].ToString();
                        break;
                    }
                }

                if (product.IntervalUnitId == IntervalUnit.Hour || product.IntervalUnitId == IntervalUnit.Minute)
                {
                    if (string.IsNullOrEmpty(reservationId))
                    {
                        return Json(new
                        {
                            success = false,
                            message = _translationService.GetResource("Product.Addtocart.Reservation.Required")
                        });
                    }
                    var productReservationService = HttpContext.RequestServices.GetRequiredService<IProductReservationService>();
                    var reservation = await productReservationService.GetProductReservation(reservationId);
                    if (reservation == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "No reservation found"
                        });
                    }
                    duration = reservation.Duration;
                    rentalStartDate = reservation.Date;
                    parameter = reservation.Parameter;
                }
                else if (product.IntervalUnitId == IntervalUnit.Day)
                {
                    string datefrom = "";
                    string dateto = "";
                    foreach (var item in form)
                    {
                        if (item.Key == "reservationDatepickerFrom")
                        {
                            datefrom = item.Value;
                        }

                        if (item.Key == "reservationDatepickerTo")
                        {
                            dateto = item.Value;
                        }
                    }

                    string datePickerFormat = "MM/dd/yyyy";
                    if (!string.IsNullOrEmpty(datefrom))
                    {
                        rentalStartDate = DateTime.ParseExact(datefrom, datePickerFormat, CultureInfo.InvariantCulture);
                    }

                    if (!string.IsNullOrEmpty(dateto))
                    {
                        rentalEndDate = DateTime.ParseExact(dateto, datePickerFormat, CultureInfo.InvariantCulture);
                    }
                }
            }

            var cartType = (ShoppingCartType)shoppingCartTypeId;

            //save item
            var addToCartWarnings = new List<string>();


            string warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
                form["WarehouseId"].ToString() :
                product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
                (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);


            //add to the cart
            var (warnings, shoppingCartItem) = await _shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                productId, cartType, _workContext.CurrentStore.Id, warehouseId,
                attributes, customerEnteredPriceConverted,
                rentalStartDate, rentalEndDate, quantity, true, reservationId, parameter, duration,
                new ShoppingCartValidatorOptions() {
                    GetRequiredProductWarnings = false,
                    GetInventoryWarnings = (cartType == ShoppingCartType.ShoppingCart || !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist),
                });

            addToCartWarnings.AddRange(warnings);

            #region Return result

            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart/wishlist
                //display warnings
                return Json(new
                {
                    success = false,
                    message = addToCartWarnings.ToArray()
                });
            }

            var addtoCartModel = await _mediator.Send(new GetAddToCart() {
                Product = product,
                Customer = _workContext.CurrentCustomer,
                ShoppingCartItem = shoppingCartItem,
                Quantity = quantity,
                CartType = cartType,
                CustomerEnteredPrice = customerEnteredPriceConverted,
                Attributes = attributes,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
                Duration = duration,
                Parameter = parameter,
                ReservationId = reservationId,
                StartDate = rentalStartDate,
                EndDate = rentalEndDate
            });

            //added to the cart/wishlist
            switch (cartType)
            {
                case ShoppingCartType.Wishlist:
                    {
                        //activity log
                        _ = _customerActivityService.InsertActivity("PublicStore.AddToWishlist", product.Id,
                            _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                            _translationService.GetResource("ActivityLog.PublicStore.AddToWishlist"), product.Name);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var qty = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist)).Sum(x => x.Quantity);
                        var updatetopwishlistsectionhtml = string.Format(_translationService.GetResource("Wishlist.HeaderQuantity"), qty);

                        return Json(new
                        {
                            success = true,
                            message = string.Format(_translationService.GetResource("Products.ProductHasBeenAddedToTheWishlist.Link"), Url.RouteUrl("Wishlist")),
                            updatetopwishlistsectionhtml = updatetopwishlistsectionhtml,
                            wishlistqty = qty,
                            model = addtoCartModel
                        });
                    }
                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        //activity log
                        _ = _customerActivityService.InsertActivity("PublicStore.AddToShoppingCart", product.Id,
                            _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                            _translationService.GetResource("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

                        if (_shoppingCartSettings.DisplayCartAfterAddingProduct)
                        {
                            //redirect to the shopping cart page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("ShoppingCart"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCartTypes = new List<ShoppingCartType>();
                        shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
                        shoppingCartTypes.Add(ShoppingCartType.Auctions);
                        if (_shoppingCartSettings.AllowOnHoldCart)
                            shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

                        var updatetopcartsectionhtml = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"),
                            (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray()))
                                .Sum(x => x.Quantity));

                        var miniShoppingCartmodel = _shoppingCartSettings.MiniShoppingCartEnabled ? await _mediator.Send(new GetMiniShoppingCart() {
                            Customer = _workContext.CurrentCustomer,
                            Currency = _workContext.WorkingCurrency,
                            Language = _workContext.WorkingLanguage,
                            TaxDisplayType = _workContext.TaxDisplayType,
                            Store = _workContext.CurrentStore
                        }) : null;

                        return Json(new
                        {
                            success = true,
                            message = string.Format(_translationService.GetResource("Products.ProductHasBeenAddedToTheCart.Link"), Url.RouteUrl("ShoppingCart")),
                            updatetopcartsectionhtml = updatetopcartsectionhtml,
                            sidebarshoppingcartmodel = miniShoppingCartmodel,
                            refreshreservation = product.ProductTypeId == ProductType.Reservation && product.IntervalUnitId != IntervalUnit.Day,
                            model = addtoCartModel
                        });
                    }
            }


            #endregion
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddBid(string productId, int shoppingCartTypeId, IFormCollection form,
            [FromServices] IAuctionService auctionService)
        {
            var customer = _workContext.CurrentCustomer;
            if (!await _groupService.IsRegistered(customer))
            {
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.Mustberegisteredtobid")
                });
            }
            double bid = 0;
            foreach (string formKey in form.Keys)
            {
                if (formKey.Equals(string.Format("auction_{0}.HighestBidValue", productId), StringComparison.OrdinalIgnoreCase))
                {
                    double.TryParse(form[formKey], out bid);
                    if (bid == 0)
                        double.TryParse(form[formKey], NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("en-US").NumberFormat, out bid);

                    bid = Math.Round(bid, 2);
                    break;
                }
            }
            if (bid <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.BidMustBeHigher")
                });
            }

            Product product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.HighestBidder == customer.Id)
            {
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.AlreadyHighestBidder")
                });
            }

            var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ? form["WarehouseId"].ToString() : _workContext.CurrentStore.DefaultWarehouseId;

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
            warnings.AddRange(_shoppingCartValidator.GetAuctionProductWarning(bid, product, customer));

            if (warnings.Any())
            {
                string toReturn = "";
                foreach (var warning in warnings)
                {
                    toReturn += warning + "</br>";
                }

                return Json(new
                {
                    success = false,
                    message = toReturn
                });
            }

            //insert new bid
            await auctionService.NewBid(customer, product, _workContext.CurrentStore, _workContext.WorkingLanguage, warehouseId, bid);

            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.AddNewBid", product.Id,
                _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.PublicStore.AddToBid"), product.Name);

            var addtoCartModel = await _mediator.Send(new GetAddToCart() {
                Product = product,
                Customer = customer,
                Quantity = 1,
                CartType = ShoppingCartType.Auctions,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
            });

            return Json(new
            {
                success = true,
                message = _translationService.GetResource("ShoppingCart.Yourbidhasbeenplaced"),
                model = addtoCartModel
            });
        }

        public virtual async Task<IActionResult> GetItemCart(string shoppingcartId)
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.FirstOrDefault(sci => sci.Id == shoppingcartId);
            if (cart == null)
                return Json(new
                {
                    success = false,
                    message = "No item cart found with the specified ID"
                });

            var product = await _productService.GetProductById(cart.ProductId);
            if (product == null)
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });


            //availability dates
            if (!product.IsAvailable() && !(product.ProductTypeId == ProductType.Auction))
                return Json(new
                {
                    success = false,
                    message = "No product found with the specified ID"
                });

            //visible individually?
            if (!product.VisibleIndividually)
            {
                //is this one an associated products?
                var parentGroupedProduct = await _productService.GetProductById(product.ParentGroupedProductId);
                if (parentGroupedProduct == null)
                {
                    return Json(new
                    {
                        redirect = Url.RouteUrl("HomePage"),
                    });
                }
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            //prepare the model
            var model = await _mediator.Send(new GetProductDetailsPage() {
                Store = _workContext.CurrentStore,
                Product = product,
                UpdateCartItem = cart
            });

            return Json(new
            {
                success = true,
                model = model,
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> UpdateItemCart(string shoppingCartItemId, IFormCollection form)
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.FirstOrDefault(sci => sci.Id == shoppingCartItemId);
            if (cart == null)
                return Json(new
                {
                    success = false,
                    message = "No item cart found with the specified ID"
                });

            var product = await _productService.GetProductById(cart.ProductId);
            if (product == null)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("HomePage"),
                });
            }

            //you can't add group products
            if (product.ProductTypeId == ProductType.GroupedProduct)
            {
                return Json(new
                {
                    success = false,
                    message = "Grouped products couldn't be added to the cart"
                });
            }

            //you can't add reservation product to wishlist
            if (product.ProductTypeId == ProductType.Reservation && cart.ShoppingCartTypeId == ShoppingCartType.Wishlist)
            {
                return Json(new
                {
                    success = false,
                    message = "Reservation products couldn't be added to the wishlist"
                });
            }

            //you can't add auction product to wishlist
            if (product.ProductTypeId == ProductType.Auction)
            {
                return Json(new
                {
                    success = false,
                    message = "Auction products couldn't be added to the wishlist"
                });
            }
            //check available date
            if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            {
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.NotAvailable")
                });
            }

            #region Customer entered price
            double? customerEnteredPriceConverted = null;
            if (product.EnteredPrice)
            {
                foreach (string formKey in form.Keys)
                {
                    if (formKey.Equals(string.Format("addtocart_{0}.CustomerEnteredPrice", cart.ProductId), StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(form[formKey], out double customerEnteredPrice))
                            customerEnteredPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice, _workContext.WorkingCurrency);
                        break;
                    }
                }
            }
            #endregion

            #region Quantity

            var quantity = cart.Quantity;
            foreach (string formKey in form.Keys)
                if (formKey.Equals(string.Format("addtocart_{0}.EnteredQuantity", cart.ProductId), StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(form[formKey], out quantity);
                    break;
                }

            #endregion

            //product and gift voucher attributes
            var attributes = await _mediator.Send(new GetParseProductAttributes() { Product = product, Form = form });

            //rental attributes
            DateTime? rentalStartDate = cart.RentalStartDateUtc;
            DateTime? rentalEndDate = cart.RentalEndDateUtc;
            if (product.ProductTypeId == ProductType.Reservation)
            {
                product.ParseReservationDates(form, out rentalStartDate, out rentalEndDate);
            }

            //product reservation
            string reservationId = cart.ReservationId;
            string parameter = cart.Parameter;
            string duration = cart.Duration;
            if (product.ProductTypeId == ProductType.Reservation)
            {
                foreach (string formKey in form.Keys)
                {
                    if (formKey.Contains("Reservation"))
                    {
                        reservationId = form["Reservation"].ToString();
                        break;
                    }
                }

                if (product.IntervalUnitId == IntervalUnit.Hour || product.IntervalUnitId == IntervalUnit.Minute)
                {
                    if (string.IsNullOrEmpty(reservationId))
                    {
                        return Json(new
                        {
                            success = false,
                            message = _translationService.GetResource("Product.Addtocart.Reservation.Required")
                        });
                    }
                    var productReservationService = HttpContext.RequestServices.GetRequiredService<IProductReservationService>();
                    var reservation = await productReservationService.GetProductReservation(reservationId);
                    if (reservation == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "No reservation found"
                        });
                    }
                    duration = reservation.Duration;
                    rentalStartDate = reservation.Date;
                    parameter = reservation.Parameter;
                }
                else if (product.IntervalUnitId == IntervalUnit.Day)
                {
                    string datefrom = "";
                    string dateto = "";
                    foreach (var item in form)
                    {
                        if (item.Key == "reservationDatepickerFrom")
                        {
                            datefrom = item.Value;
                        }

                        if (item.Key == "reservationDatepickerTo")
                        {
                            dateto = item.Value;
                        }
                    }

                    string datePickerFormat = "MM/dd/yyyy";
                    if (!string.IsNullOrEmpty(datefrom))
                    {
                        rentalStartDate = DateTime.ParseExact(datefrom, datePickerFormat, CultureInfo.InvariantCulture);
                    }

                    if (!string.IsNullOrEmpty(dateto))
                    {
                        rentalEndDate = DateTime.ParseExact(dateto, datePickerFormat, CultureInfo.InvariantCulture);
                    }
                }
            }

            //save item
            var addToCartWarnings = new List<string>();

            var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
                form["WarehouseId"].ToString() : cart.WarehouseId;

            //update existing item
            addToCartWarnings.AddRange(await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                cart.Id, warehouseId, attributes, customerEnteredPriceConverted,
                rentalStartDate, rentalEndDate, quantity, true));

            if (addToCartWarnings.Any())
            {
                //cannot be updated the cart/wishlist
                return Json(new
                {
                    success = false,
                    message = addToCartWarnings.ToArray()
                });
            }

            return Json(new
            {
                success = true,
                message = ""
            });

        }

        #endregion

    }
}
