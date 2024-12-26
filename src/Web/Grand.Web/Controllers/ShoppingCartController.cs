using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.ShoppingCart;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

public class ShoppingCartController : BasePublicController
{
    #region Constructors

    public ShoppingCartController(
        IWorkContextAccessor workContextAccessor,
        IShoppingCartService shoppingCartService,
        ITranslationService translationService,
        IDiscountService discountService,
        ICustomerService customerService,
        IGroupService groupService,
        ICheckoutAttributeService checkoutAttributeService,
        IPermissionService permissionService,
        IMediator mediator,
        IShoppingCartValidator shoppingCartValidator,
        ShoppingCartSettings shoppingCartSettings,
        OrderSettings orderSettings)
    {
        _workContextAccessor = workContextAccessor;
        _shoppingCartService = shoppingCartService;
        _translationService = translationService;
        _discountService = discountService;
        _customerService = customerService;
        _groupService = groupService;
        _checkoutAttributeService = checkoutAttributeService;
        _permissionService = permissionService;
        _mediator = mediator;
        _shoppingCartValidator = shoppingCartValidator;
        _shoppingCartSettings = shoppingCartSettings;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Utilities

    private ShoppingCartType[] PrepareCartTypes()
    {
        var shoppingCartTypes = new List<ShoppingCartType> {
            ShoppingCartType.ShoppingCart,
            ShoppingCartType.Auctions
        };
        if (_shoppingCartSettings.AllowOnHoldCart)
            shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

        return shoppingCartTypes.ToArray();
    }

    #endregion

    #region Fields

    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITranslationService _translationService;
    private readonly IDiscountService _discountService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly ICheckoutAttributeService _checkoutAttributeService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly OrderSettings _orderSettings;

    #endregion

    #region Shopping cart

    [HttpGet]
    [ProducesResponseType(typeof(MiniShoppingCartModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> SidebarShoppingCart()
    {
        if (!_shoppingCartSettings.MiniShoppingCartEnabled)
            return Content("");

        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return Content("");

        var model = await _mediator.Send(new GetMiniShoppingCart {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return Json(model);
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> CheckoutAttributeChange(CheckoutAttributeSelectedModel model,
        [FromServices] ICheckoutAttributeParser checkoutAttributeParser,
        [FromServices] ICheckoutAttributeFormatter checkoutAttributeFormatter)
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        var checkoutAttributes = await _mediator.Send(new SaveCheckoutAttributesCommand {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Cart = cart,
            SelectedAttributes = model.Attributes
        });

        var enabledAttributeIds = new List<string>();
        var disabledAttributeIds = new List<string>();
        var attributes =
            await _checkoutAttributeService.GetAllCheckoutAttributes(_workContextAccessor.WorkContext.CurrentStore.Id,
                !cart.RequiresShipping());
        foreach (var attribute in attributes)
        {
            var conditionMet = await checkoutAttributeParser.IsConditionMet(attribute, checkoutAttributes);
            if (!conditionMet.HasValue) continue;

            if (conditionMet.Value)
                enabledAttributeIds.Add(attribute.Id);
            else
                disabledAttributeIds.Add(attribute.Id);
        }

        var orderTotals = await _mediator.Send(new GetOrderTotals {
            Cart = cart,
            IsEditable = true,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            enabledattributeids = enabledAttributeIds.ToArray(),
            disabledattributeids = disabledAttributeIds.ToArray(),
            model = orderTotals,
            checkoutattributeinfo =
                await checkoutAttributeFormatter.FormatAttributes(checkoutAttributes, _workContextAccessor.WorkContext.CurrentCustomer)
        });
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> UploadFileCheckoutAttribute(string attributeId,
        [FromServices] IDownloadService downloadService)
    {
        var attribute = await _checkoutAttributeService.GetCheckoutAttributeById(attributeId);
        if (attribute is not { AttributeControlTypeId: AttributeControlType.FileUpload })
            return Json(new {
                success = false,
                downloadGuid = Guid.Empty
            });

        var form = await HttpContext.Request.ReadFormAsync();
        var httpPostedFile = form.Files.FirstOrDefault();
        if (httpPostedFile == null)
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });

        var fileBinary = httpPostedFile.GetDownloadBits();
        var fileName = httpPostedFile.FileName;

        fileName = Path.GetFileName(fileName);

        var contentType = httpPostedFile.ContentType;

        var fileExtension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(fileExtension))
            fileExtension = fileExtension.ToLowerInvariant();

        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.ToLowerInvariant()
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            if (!allowedFileExtensions.Contains(fileExtension.ToLowerInvariant()))
                return Json(new {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                    downloadGuid = Guid.Empty
                });
        }

        if (attribute.ValidationFileMaximumSize.HasValue)
        {
            //compare in bytes
            var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
            if (fileBinary.Length > maxFileSizeBytes)
                //when returning JSON the mime-type must be set to text/plain
                //otherwise some browsers will pop-up a "Save As" dialog.
                return Json(new {
                    success = false,
                    message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"),
                        attribute.ValidationFileMaximumSize.Value),
                    downloadGuid = Guid.Empty
                });
        }

        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = _workContextAccessor.WorkContext.CurrentCustomer.Id,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = contentType,
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            DownloadType = DownloadType.CheckoutAttribute,
            ReferenceId = attributeId
        };
        await downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            message = _translationService.GetResource("ShoppingCart.FileUploaded"),
            downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
            downloadGuid = download.DownloadGuid
        });
    }

    [HttpGet]
    //[ProducesResponseType(typeof(ShoppingCartModel), StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> Cart(bool checkoutAttributes)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return RedirectToRoute("HomePage");

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());
        var model = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            ValidateCheckoutAttributes = checkoutAttributes,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return View(model);
    }

    [HttpGet]
    [DenySystemAccount]
    [ProducesResponseType(typeof(ShoppingCartModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> CartSummary()
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        var model = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            IsEditable = false,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(model);
    }

    [HttpGet]
    [DenySystemAccount]
    [ProducesResponseType(typeof(OrderTotalsModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> CartTotal()
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        var model = await _mediator.Send(new GetOrderTotals {
            Cart = cart,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });
        return Json(model);
    }


    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> UpdateQuantity(UpdateQuantityModel model)
    {
        if (!ModelState.IsValid)
            return Json(new {
                success = false,
                warnings = string.Join(',', ModelState.Values.SelectMany(x => x.Errors.Select(x => x.ErrorMessage)))
            });

        var cart = (await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes()))
            .FirstOrDefault(x => x.Id == model.ShoppingCartId);
        var warnings = new List<string>();

        if (cart != null)
        {
            var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer,
                cart.Id, cart.WarehouseId, cart.Attributes, cart.EnteredPrice,
                cart.RentalStartDateUtc, cart.RentalEndDateUtc,
                model.Quantity);
            warnings.AddRange(currSciWarnings);
        }

        var cartModel = await _mediator.Send(new GetShoppingCart {
            Cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes()),
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            success = !warnings.Any(),
            warnings = string.Join(", ", warnings),
            totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"),
                cartModel.Items.Sum(x => x.Quantity)),
            model = cartModel
        });
    }

    [HttpGet]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ClearCart()
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return RedirectToRoute("HomePage");

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());

        foreach (var sci in cart)
            await _shoppingCartService.DeleteShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer, sci,
                ensureOnlyActiveCheckoutAttributes: true);

        return RedirectToRoute("HomePage");
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> DeleteCartItem(DeleteCartItemModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return RedirectToRoute("HomePage");

        var shoppingCartTypes = new List<ShoppingCartType> { ShoppingCartType.ShoppingCart };
        if (_shoppingCartSettings.AllowOnHoldCart)
            shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

        var item = (await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
                shoppingCartTypes.ToArray()))
            .FirstOrDefault(sci => sci.Id == model.Id);

        if (item != null)
            await _shoppingCartService.DeleteShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer, item,
                ensureOnlyActiveCheckoutAttributes: true);

        var miniShoppingCartmodel = await _mediator.Send(new GetMiniShoppingCart {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        if (!model.ShoppingCartPage)
            return Json(new {
                totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"),
                    miniShoppingCartmodel.TotalProducts),
                sidebarshoppingcartmodel = miniShoppingCartmodel
            });

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());
        var shoppingcartmodel = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            totalproducts = string.Format(_translationService.GetResource("ShoppingCart.HeaderQuantity"),
                miniShoppingCartmodel.TotalProducts),
            sidebarshoppingcartmodel = miniShoppingCartmodel,
            model = shoppingcartmodel
        });
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> ChangeTypeCartItem(ChangeTypeCartItemModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return RedirectToRoute("HomePage");

        if (!_shoppingCartSettings.AllowOnHoldCart)
            return RedirectToRoute("HomePage");

        var shoppingCartTypes = new List<ShoppingCartType> { ShoppingCartType.ShoppingCart };
        if (_shoppingCartSettings.AllowOnHoldCart)
            shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

        var item = (await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
                shoppingCartTypes.ToArray()))
            .FirstOrDefault(sci => sci.Id == model.Id);

        if (item != null)
        {
            item.ShoppingCartTypeId = model.Status ? ShoppingCartType.ShoppingCart : ShoppingCartType.OnHoldCart;
            await _customerService.UpdateShoppingCartItem(_workContextAccessor.WorkContext.CurrentCustomer.Id, item);
        }

        var miniShoppingCart = await _mediator.Send(new GetMiniShoppingCart {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());
        var shoppingcartmodel = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            model = shoppingcartmodel,
            sidebarshoppingcartmodel = miniShoppingCart
        });
    }

    [IgnoreApi]
    [HttpGet]
    public virtual IActionResult ContinueShopping()
    {
        return RedirectToRoute("HomePage");
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> StartCheckout(CheckoutAttributeSelectedModel model)
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
        List<CustomAttribute> checkoutAttributes;
        //parse and save checkout attributes
        if (model?.Attributes is { Count: > 0 })
            checkoutAttributes = (await _mediator.Send(new SaveCheckoutAttributesCommand {
                Customer = _workContextAccessor.WorkContext.CurrentCustomer,
                Store = _workContextAccessor.WorkContext.CurrentStore,
                Cart = cart,
                SelectedAttributes = model.Attributes
            })).ToList();
        else
            checkoutAttributes =
                _workContextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<List<CustomAttribute>>(
                    SystemCustomerFieldNames.CheckoutAttributes, _workContextAccessor.WorkContext.CurrentStore.Id);

        var checkoutAttributeWarnings =
            await _shoppingCartValidator.GetShoppingCartWarnings(cart, checkoutAttributes, true, true);
        if (checkoutAttributeWarnings.Any()) return RedirectToRoute("ShoppingCart", new { checkoutAttributes = true });

        //everything is OK
        if (!await _groupService.IsGuest(_workContextAccessor.WorkContext.CurrentCustomer)) return RedirectToRoute("Checkout");
        if (!_orderSettings.AnonymousCheckoutAllowed)
            return Challenge();

        return RedirectToRoute("LoginCheckoutAsGuest", new { returnUrl = Url.RouteUrl("ShoppingCart") });
    }

    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> ApplyDiscountCoupon(DiscountCouponModel model)
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());

        var message = string.Empty;
        var isApplied = false;

        if (ModelState.IsValid)
        {
            //valid
            var applyCouponCode =
                _workContextAccessor.WorkContext.CurrentCustomer.ApplyCouponCode(SystemCustomerFieldNames.DiscountCoupons,
                    model.DiscountCouponCode);
            //apply new value
            await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.DiscountCoupons, applyCouponCode);
            message = _translationService.GetResource("ShoppingCart.DiscountCouponCode.Applied");
            isApplied = true;
        }
        else
        {
            message = string.Join(',', ModelState.Values.SelectMany(x => x.Errors.Select(x => x.ErrorMessage)));
        }

        var cartModel = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        cartModel.DiscountBox.Message = message;
        cartModel.DiscountBox.IsApplied = isApplied;

        return Json(new {
            model = cartModel
        });
    }

    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> ApplyGiftVoucher(GiftVoucherCouponModel model)
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());

        var message = string.Empty;
        var isApplied = false;

        if (ModelState.IsValid)
        {
            model.GiftVoucherCouponCode = model.GiftVoucherCouponCode.Trim();

            var result = _workContextAccessor.WorkContext.CurrentCustomer.ApplyCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons,
                model.GiftVoucherCouponCode.ToLower());
            //apply new value
            await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                SystemCustomerFieldNames.GiftVoucherCoupons, result);

            message = _translationService.GetResource("ShoppingCart.Code.Applied");
            isApplied = true;
        }
        else
        {
            message = string.Join(',', ModelState.Values.SelectMany(x => x.Errors.Select(x => x.ErrorMessage)));
        }

        var cartModel = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        cartModel.GiftVoucherBox.Message = message;
        cartModel.GiftVoucherBox.IsApplied = isApplied;

        return Json(new {
            model = cartModel
        });
    }

    [AutoValidateAntiforgeryToken]
    [HttpPost]
    public virtual async Task<IActionResult> GetEstimateShipping(EstimateShippingModel model)
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        var result = await _mediator.Send(new GetEstimateShippingResult {
            Cart = cart,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            CountryId = model.CountryId,
            StateProvinceId = model.StateProvinceId,
            ZipPostalCode = model.ZipPostalCode
        });

        return PartialView("Partials/EstimateShippingResult", result);
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> RemoveDiscountCoupon(string discountId)
    {
        var discount = await _discountService.GetDiscountById(discountId);
        if (discount != null)
        {
            var coupons =
                _workContextAccessor.WorkContext.CurrentCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
            foreach (var item in coupons)
            {
                var dd = await _discountService.GetDiscountByCouponCode(item);
                if (dd.Id != discount.Id) continue;

                //remove coupon
                var result =
                    _workContextAccessor.WorkContext.CurrentCustomer.RemoveCouponCode(SystemCustomerFieldNames.DiscountCoupons, item);
                await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.DiscountCoupons, result);
            }
        }

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());

        var model = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            model
        });
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> RemoveGiftVoucherCode(string giftVoucherId,
        [FromServices] IGiftVoucherService giftVoucherService)
    {
        if (!string.IsNullOrEmpty(giftVoucherId))
        {
            //remove card
            var giftvoucher = await giftVoucherService.GetGiftVoucherById(giftVoucherId);
            if (giftvoucher != null)
            {
                var result =
                    _workContextAccessor.WorkContext.CurrentCustomer.RemoveCouponCode(SystemCustomerFieldNames.GiftVoucherCoupons,
                        giftvoucher.Code);
                await _customerService.UpdateUserField(_workContextAccessor.WorkContext.CurrentCustomer,
                    SystemCustomerFieldNames.GiftVoucherCoupons, result);
            }
        }

        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id, PrepareCartTypes());

        var model = await _mediator.Send(new GetShoppingCart {
            Cart = cart,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            TaxDisplayType = _workContextAccessor.WorkContext.TaxDisplayType
        });

        return Json(new {
            model
        });
    }

    #endregion
}