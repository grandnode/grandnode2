using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
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

            //we can add only simple products and 
            if (product.ProductTypeId != ProductType.SimpleProduct || _shoppingCartSettings.AllowToSelectWarehouse)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            //products with "minimum order quantity" more than a specified qty
            if (product.OrderMinimumQuantity > quantity)
            {
                //we cannot add to the cart such products from category pages
                //it can confuse customers. That's why we redirect customers to the product details page
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            if (product.EnteredPrice)
            {
                //cannot be added to the cart (requires a customer to enter price)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0)
            {
                //cannot be added to the cart (requires a customer to select a quantity from dropdownlist)
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            if (product.ProductAttributeMappings.Any())
            {
                //product has some attributes. let a customer see them
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            var customer = _workContext.CurrentCustomer;

            string warehouseId =
               product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
               (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);

            //get standard warnings without attribute validations
            //first, try to find existing shopping cart item
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, cartType);
            var shoppingCartItem = await _shoppingCartService.FindShoppingCartItem(cart, cartType, product.Id, warehouseId);
            //if we already have the same product in the cart, then use the total quantity to validate
            var quantityToValidate = shoppingCartItem != null ? shoppingCartItem.Quantity + quantity : quantity;
            var addToCartWarnings = await _shoppingCartValidator
              .GetShoppingCartItemWarnings(customer, new ShoppingCartItem() {
                  ShoppingCartTypeId = cartType,
                  StoreId = _workContext.CurrentStore.Id,
                  WarehouseId = warehouseId,
                  Quantity = quantityToValidate
              },
                 product, new ShoppingCartValidatorOptions() { GetRequiredProductWarnings = false });
            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            //try adding product to the cart (now including product attribute validation, etc)
            addToCartWarnings = await _shoppingCartService.AddToCart(customer: customer,
                productId: productId,
                shoppingCartType: cartType,
                storeId: _workContext.CurrentStore.Id,
                warehouseId: warehouseId,
                quantity: quantity, getRequiredProductWarnings: false);
            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart
                //but we do not display attribute and gift voucher warnings here. do it on the product details page
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = product.GetSeName(_workContext.WorkingLanguage.Id) }),
                });
            }

            var addtoCartModel = await _mediator.Send(new GetAddToCart() {
                Product = product,
                Customer = customer,
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
                        await _customerActivityService.InsertActivity("PublicStore.AddToWishlist", product.Id, _translationService.GetResource("ActivityLog.PublicStore.AddToWishlist"), product.Name);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct || forceredirection)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var qty = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist).Sum(x => x.Quantity);
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
                        await _customerActivityService.InsertActivity("PublicStore.AddToShoppingCart", product.Id, _translationService.GetResource("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

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
                            _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray())
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


            #region Update existing shopping cart item?
            string updatecartitemid = "";
            foreach (string formKey in form.Keys)
                if (formKey.Equals(string.Format("addtocart_{0}.UpdatedShoppingCartItemId", productId), StringComparison.OrdinalIgnoreCase))
                {
                    updatecartitemid = form[formKey];
                    break;
                }

            ShoppingCartItem updatecartitem = null;
            if (_shoppingCartSettings.AllowCartItemEditing && !string.IsNullOrEmpty(updatecartitemid))
            {
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, (ShoppingCartType)shoppingCartTypeId);
                updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);

                //is it this product?
                if (updatecartitem != null && product.Id != updatecartitem.ProductId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This product does not match a passed shopping cart item identifier"
                    });
                }
            }
            #endregion

            #region Customer entered price
            double? customerEnteredPriceConverted = null;
            if (product.EnteredPrice)
            {
                foreach (string formKey in form.Keys)
                {
                    if (formKey.Equals(string.Format("addtocart_{0}.CustomerEnteredPrice", productId), StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(form[formKey], out double customerEnteredPrice))
                            customerEnteredPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrency(customerEnteredPrice, _workContext.WorkingCurrency);
                        break;
                    }
                }
            }
            #endregion

            #region Quantity

            int quantity = 1;
            foreach (string formKey in form.Keys)
                if (formKey.Equals(string.Format("addtocart_{0}.EnteredQuantity", productId), StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(form[formKey], out quantity);
                    break;
                }

            #endregion

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

            var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                        //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                        updatecartitem.ShoppingCartTypeId;

            //save item
            var addToCartWarnings = new List<string>();


            string warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
                form["WarehouseId"].ToString() :
                product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
                (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);

            if (updatecartitem == null)
            {
                //add to the cart
                addToCartWarnings.AddRange(await _shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                    productId, cartType, _workContext.CurrentStore.Id, warehouseId,
                    attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true, reservationId, parameter, duration, getRequiredProductWarnings: false));
            }
            else
            {
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, (ShoppingCartType)shoppingCartTypeId);
                var otherCartItemWithSameParameters = await _shoppingCartService.FindShoppingCartItem(
                    cart, updatecartitem.ShoppingCartTypeId, productId, warehouseId, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate);
                if (otherCartItemWithSameParameters != null &&
                    otherCartItemWithSameParameters.Id == updatecartitem.Id)
                {
                    //ensure it's other shopping cart cart item
                    otherCartItemWithSameParameters = null;
                }
                //update existing item
                addToCartWarnings.AddRange(await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                    updatecartitem.Id, warehouseId, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true));
                if (otherCartItemWithSameParameters != null && !addToCartWarnings.Any())
                {
                    //delete the same shopping cart item (the other one)
                    await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, otherCartItemWithSameParameters);
                }
            }

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
                        await _customerActivityService.InsertActivity("PublicStore.AddToWishlist", product.Id, _translationService.GetResource("ActivityLog.PublicStore.AddToWishlist"), product.Name);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist"),
                            });
                        }

                        //display notification message and update appropriate blocks
                        var qty = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist).Sum(x => x.Quantity);
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
                        await _customerActivityService.InsertActivity("PublicStore.AddToShoppingCart", product.Id, _translationService.GetResource("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

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
                            _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray())
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
            await _customerActivityService.InsertActivity("PublicStore.AddNewBid", product.Id, _translationService.GetResource("ActivityLog.PublicStore.AddToBid"), product.Name);

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


        #endregion

    }
}
