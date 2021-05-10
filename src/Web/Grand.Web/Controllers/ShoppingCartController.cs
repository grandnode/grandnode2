using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.ShoppingCart;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class ShoppingCartController : BasePublicController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITranslationService _translationService;
        private readonly IDiscountService _discountService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly IPermissionService _permissionService;
        private readonly IUserFieldService _userFieldService;
        private readonly IMediator _mediator;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly OrderSettings _orderSettings;

        #endregion

        #region Constructors

        public ShoppingCartController(
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            ITranslationService translationService,
            IDiscountService discountService,
            ICustomerService customerService,
            IGroupService groupService,
            ICheckoutAttributeService checkoutAttributeService,
            IPermissionService permissionService,
            IUserFieldService userFieldService,
            IMediator mediator,
            IShoppingCartValidator shoppingCartValidator,
            ShoppingCartSettings shoppingCartSettings,
            OrderSettings orderSettings)
        {
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _translationService = translationService;
            _discountService = discountService;
            _customerService = customerService;
            _groupService = groupService;
            _checkoutAttributeService = checkoutAttributeService;
            _permissionService = permissionService;
            _userFieldService = userFieldService;
            _mediator = mediator;
            _shoppingCartValidator = shoppingCartValidator;
            _shoppingCartSettings = shoppingCartSettings;
            _orderSettings = orderSettings;
        }

        #endregion

        #region Utilities

        protected ShoppingCartType[] PrepareCartTypes()
        {
            var shoppingCartTypes = new List<ShoppingCartType>();
            shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
            shoppingCartTypes.Add(ShoppingCartType.Auctions);
            if (_shoppingCartSettings.AllowOnHoldCart)
                shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

            return shoppingCartTypes.ToArray();
        }

        #endregion

        #region Shopping cart

        [HttpPost]
        public virtual async Task<IActionResult> CheckoutAttributeChange(IFormCollection form,
            [FromServices] ICheckoutAttributeParser checkoutAttributeParser,
            [FromServices] ICheckoutAttributeFormatter checkoutAttributeFormatter)
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

            var checkoutAttributes = await _mediator.Send(new SaveCheckoutAttributesCommand()
            {
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                Cart = cart,
                Form = form
            });

            var enabledAttributeIds = new List<string>();
            var disabledAttributeIds = new List<string>();
            var attributes = await _checkoutAttributeService.GetAllCheckoutAttributes(_workContext.CurrentStore.Id, !cart.RequiresShipping());
            foreach (var attribute in attributes)
            {
                var conditionMet = await checkoutAttributeParser.IsConditionMet(attribute, checkoutAttributes);
                if (conditionMet.HasValue)
                {
                    if (conditionMet.Value)
                        enabledAttributeIds.Add(attribute.Id);
                    else
                        disabledAttributeIds.Add(attribute.Id);
                }
            }
            var model = await _mediator.Send(new GetOrderTotals()
            {
                Cart = cart,
                IsEditable = true,
                Store = _workContext.CurrentStore,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return Json(new
            {
                enabledattributeids = enabledAttributeIds.ToArray(),
                disabledattributeids = disabledAttributeIds.ToArray(),
                model = model,
                checkoutattributeinfo = await checkoutAttributeFormatter.FormatAttributes(checkoutAttributes, _workContext.CurrentCustomer),
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> UploadFileCheckoutAttribute(string attributeId,
            [FromServices] IDownloadService downloadService)
        {
            var attribute = await _checkoutAttributeService.GetCheckoutAttributeById(attributeId);
            if (attribute == null || attribute.AttributeControlTypeId != AttributeControlType.FileUpload)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }

            var form = await HttpContext.Request.ReadFormAsync();
            var httpPostedFile = form.Files.FirstOrDefault();
            if (httpPostedFile == null)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded",
                    downloadGuid = Guid.Empty,
                });
            }

            var fileBinary = httpPostedFile.GetDownloadBits();

            var qqFileNameParameter = "qqfilename";
            var fileName = httpPostedFile.FileName;
            if (string.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
                fileName = form[qqFileNameParameter].ToString();
            //remove path (passed in IE)
            fileName = Path.GetFileName(fileName);

            var contentType = httpPostedFile.ContentType;

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
            {
                var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.ToLowerInvariant()
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                if (!allowedFileExtensions.Contains(fileExtension.ToLowerInvariant()))
                {
                    return Json(new
                    {
                        success = false,
                        message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                        downloadGuid = Guid.Empty,
                    });
                }
            }
            if (attribute.ValidationFileMaximumSize.HasValue)
            {
                //compare in bytes
                var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
                if (fileBinary.Length > maxFileSizeBytes)
                {
                    //when returning JSON the mime-type must be set to text/plain
                    //otherwise some browsers will pop-up a "Save As" dialog.
                    return Json(new
                    {
                        success = false,
                        message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"), attribute.ValidationFileMaximumSize.Value),
                        downloadGuid = Guid.Empty,
                    });
                }
            }

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = "",
                DownloadBinary = fileBinary,
                ContentType = contentType,
                //we store filename without extension for downloads
                Filename = Path.GetFileNameWithoutExtension(fileName),
                Extension = fileExtension,
                IsNew = true
            };
            await downloadService.InsertDownload(download);

            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new
            {
                success = true,
                message = _translationService.GetResource("ShoppingCart.FileUploaded"),
                downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
                downloadGuid = download.DownloadGuid,
            });
        }

        public virtual async Task<IActionResult> Cart(bool checkoutAttributes)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());
            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                ValidateCheckoutAttributes = checkoutAttributes,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> UpdateCart(IFormCollection form)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var shoppingCartTypes = new List<ShoppingCartType>();
            shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
            if (_shoppingCartSettings.AllowOnHoldCart)
                shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray());

            //current warnings <cart item identifier, warnings>
            var innerWarnings = new Dictionary<string, IList<string>>();
            foreach (var sci in cart)
            {
                foreach (string formKey in form.Keys)
                    if (formKey.Equals(string.Format("itemquantity{0}", sci.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(form[formKey], out int newQuantity))
                        {
                            var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                                sci.Id, sci.WarehouseId, sci.Attributes, sci.EnteredPrice,
                                sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                                newQuantity, true, sci.ReservationId, sci.Id);
                            innerWarnings.Add(sci.Id, currSciWarnings);
                        }
                        break;
                    }
            }

            cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            //update current warnings
            foreach (var kvp in innerWarnings)
            {
                //kvp = <cart item identifier, warnings>
                var sciId = kvp.Key;
                var warnings = kvp.Value;
                //find model
                var sciModel = model.Items.FirstOrDefault(x => x.Id == sciId);
                if (sciModel != null)
                    foreach (var w in warnings)
                        if (!sciModel.Warnings.Contains(w))
                            sciModel.Warnings.Add(w);
            }
            return Json(new
            {
                totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"), model.Items.Sum(x => x.Quantity)),
                model = model
            });
        }

        public virtual async Task<IActionResult> ClearCart()
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            foreach (var sci in cart)
            {
                await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, sci, ensureOnlyActiveCheckoutAttributes: true);
            }

            return RedirectToRoute("HomePage");

        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteCartItem(string id, bool shoppingcartpage = false)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var shoppingCartTypes = new List<ShoppingCartType>();
            shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
            if (_shoppingCartSettings.AllowOnHoldCart)
                shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

            var item = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray())
                .FirstOrDefault(sci => sci.Id == id);

            if (item != null)
            {
                await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, item, ensureOnlyActiveCheckoutAttributes: true);
            }

            var miniShoppingCartmodel = await _mediator.Send(new GetMiniShoppingCart()
            {
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
                Store = _workContext.CurrentStore
            });
            if (!shoppingcartpage)
            {
                return Json(new
                {
                    totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"), miniShoppingCartmodel.TotalProducts),
                    sidebarshoppingcartmodel = miniShoppingCartmodel,
                });
            }
            else
            {
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());
                var shoppingcartmodel = await _mediator.Send(new GetShoppingCart()
                {
                    Cart = cart,
                    Customer = _workContext.CurrentCustomer,
                    Currency = _workContext.WorkingCurrency,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore,
                    TaxDisplayType = _workContext.TaxDisplayType
                });

                return Json(new
                {
                    totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"), miniShoppingCartmodel.TotalProducts),
                    sidebarshoppingcartmodel = miniShoppingCartmodel,
                    model = shoppingcartmodel,
                });
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> ChangeTypeCartItem(string id, bool status = false)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            if (!_shoppingCartSettings.AllowOnHoldCart)
                return RedirectToRoute("HomePage");

            var shoppingCartTypes = new List<ShoppingCartType>();
            shoppingCartTypes.Add(ShoppingCartType.ShoppingCart);
            if (_shoppingCartSettings.AllowOnHoldCart)
                shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

            var item = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, shoppingCartTypes.ToArray())
                .FirstOrDefault(sci => sci.Id == id);

            if (item != null)
            {
                item.ShoppingCartTypeId = status ? ShoppingCartType.ShoppingCart : ShoppingCartType.OnHoldCart;
                await _customerService.UpdateShoppingCartItem(_workContext.CurrentCustomer.Id, item);
            }

            var miniShoppingCart = await _mediator.Send(new GetMiniShoppingCart()
            {
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
                Store = _workContext.CurrentStore
            });

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());
            var shoppingcartmodel = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return Json(new
            {
                model = shoppingcartmodel,
                sidebarshoppingcartmodel = miniShoppingCart
            });

        }

        public virtual IActionResult ContinueShopping()
        {
            var returnUrl = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastContinueShoppingPage, _workContext.CurrentStore.Id);
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToRoute("HomePage");
            }
        }

        public virtual async Task<IActionResult> StartCheckout(IFormCollection form = null)
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            var checkoutAttributes = new List<CustomAttribute>();
            //parse and save checkout attributes
            if (form != null && form.Count > 0)
            {
                checkoutAttributes = (await _mediator.Send(new SaveCheckoutAttributesCommand()
                {
                    Customer = _workContext.CurrentCustomer,
                    Store = _workContext.CurrentStore,
                    Cart = cart,
                    Form = form
                })).ToList();
            }
            else
            {
                checkoutAttributes = _workContext.CurrentCustomer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, _workContext.CurrentStore.Id);
            }

            var checkoutAttributeWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(cart, checkoutAttributes, true);
            if (checkoutAttributeWarnings.Any())
            {
                return RedirectToRoute("ShoppingCart", new { checkoutAttributes = true });
            }

            //everything is OK
            if (await _groupService.IsGuest(_workContext.CurrentCustomer))
            {
                if (!_orderSettings.AnonymousCheckoutAllowed)
                    return Challenge();

                return RedirectToRoute("LoginCheckoutAsGuest", new { returnUrl = Url.RouteUrl("ShoppingCart") });
            }

            return RedirectToRoute("Checkout");
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> ApplyDiscountCoupon(string discountcouponcode)
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            var message = string.Empty;
            var isApplied = false;

            if (!string.IsNullOrWhiteSpace(discountcouponcode))
            {
                discountcouponcode = discountcouponcode.ToUpper();
                //we find even hidden records here. this way we can display a user-friendly message if it's expired
                var discount = await _discountService.GetDiscountByCouponCode(discountcouponcode, true);
                if (discount != null && discount.RequiresCouponCode)
                {
                    var coupons = _workContext.CurrentCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
                    var existsAndUsed = false;
                    foreach (var item in coupons)
                    {
                        if (await _discountService.ExistsCodeInDiscount(item, discount.Id, null))
                            existsAndUsed = true;
                    }
                    if (!existsAndUsed)
                    {
                        if (!discount.Reused)
                            existsAndUsed = !await _discountService.ExistsCodeInDiscount(discountcouponcode, discount.Id, false);

                        if (!existsAndUsed)
                        {
                            var validationResult = await _discountService.ValidateDiscount(discount, _workContext.CurrentCustomer, _workContext.WorkingCurrency, discountcouponcode);
                            if (validationResult.IsValid)
                            {
                                //valid
                                var applyCouponCode = _workContext.CurrentCustomer.ApplyCouponCode(SystemCustomerFieldNames.DiscountCoupons, discountcouponcode);
                                //apply new value
                                await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.DiscountCoupons, applyCouponCode);
                                message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.Applied");
                                isApplied = true;
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(validationResult.UserError))
                                {
                                    //some user error
                                    message = validationResult.UserError;
                                    isApplied = false;
                                }
                                else
                                {
                                    //general error text
                                    message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.WrongDiscount");
                                    isApplied = false;
                                }
                            }
                        }
                        else
                        {
                            message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.WasUsed");
                            isApplied = false;
                        }
                    }
                    else
                    {
                        message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.UsesTheSameDiscount");
                        isApplied = false;
                    }
                }
                else
                {
                    message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.WrongDiscount");
                    isApplied = false;
                }
            }
            else
            {
                message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.Required");
                isApplied = false;
            }

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            model.DiscountBox.Message = message;
            model.DiscountBox.IsApplied = isApplied;

            return Json(new
            {
                model = model
            });
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> ApplyGiftVoucher(string giftvouchercouponcode)
        {
            //trim
            if (giftvouchercouponcode != null)
                giftvouchercouponcode = giftvouchercouponcode.Trim();

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            var message = string.Empty;
            var isApplied = false;

            if (!string.IsNullOrWhiteSpace(giftvouchercouponcode))
            {
                var giftVoucher = (await _mediator.Send(new GetGiftVoucherQuery() { Code = giftvouchercouponcode, IsGiftVoucherActivated = true })).FirstOrDefault();
                bool isGiftVoucherValid = giftVoucher != null && giftVoucher.IsGiftVoucherValid(_workContext.WorkingCurrency);
                if (isGiftVoucherValid)
                {
                    var result = _workContext.CurrentCustomer.ApplyCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons, giftvouchercouponcode.Trim().ToLower());
                    //apply new value
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, result);

                    message = _translationService.GetResource("ShoppingCart.Code.Applied");
                    isApplied = true;
                }
                else
                {
                    message = _translationService.GetResource("ShoppingCart.Code.WrongGiftVoucher");
                    isApplied = false;
                }
            }
            else
            {
                message = _translationService.GetResource("ShoppingCart.Code.Required");
                isApplied = false;
            }

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            model.GiftVoucherBox.Message = message;
            model.GiftVoucherBox.IsApplied = isApplied;

            return Json(new
            {
                model = model
            });
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> GetEstimateShipping(string countryId, string stateProvinceId, string zipPostalCode, IFormCollection form)
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

            var model = await _mediator.Send(new GetEstimateShippingResult()
            {
                Cart = cart,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                CountryId = countryId,
                StateProvinceId = stateProvinceId,
                ZipPostalCode = zipPostalCode
            });

            return PartialView("_EstimateShippingResult", model);
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> RemoveDiscountCoupon(string discountId)
        {
            var discount = await _discountService.GetDiscountById(discountId);
            if (discount != null)
            {
                var coupons = _workContext.CurrentCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
                foreach (var item in coupons)
                {
                    var dd = await _discountService.GetDiscountByCouponCode(item);
                    if (dd.Id == discount.Id)
                    {
                        //remove coupon
                        var result = _workContext.CurrentCustomer.RemoveCouponCode(SystemCustomerFieldNames.DiscountCoupons, item);
                        await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.DiscountCoupons, result);
                    }
                }
            }
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return Json(new
            {
                model = model
            });
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> RemoveGiftVoucherCode(string giftVoucherId, [FromServices] IGiftVoucherService giftVoucherService)
        {
            if (!string.IsNullOrEmpty(giftVoucherId))
            {
                //remove card
                var giftvoucher = await giftVoucherService.GetGiftVoucherById(giftVoucherId);
                if (giftvoucher != null)
                {
                    var result = _workContext.CurrentCustomer.RemoveCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons, giftvoucher.Code);
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.GiftVoucherCoupons, result);
                }
            }
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, PrepareCartTypes());

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return Json(new
            {
                model = model
            });

        }
        #endregion

    }
}