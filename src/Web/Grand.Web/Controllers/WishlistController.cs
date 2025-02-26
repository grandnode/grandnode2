using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class WishlistController : BasePublicController
{
    #region Constructors

    public WishlistController(
        IContextAccessor contextAccessor,
        IShoppingCartService shoppingCartService,
        ITranslationService translationService,
        ICustomerService customerService,
        IPermissionService permissionService,
        IMediator mediator,
        ShoppingCartSettings shoppingCartSettings)
    {
        _contextAccessor = contextAccessor;
        _shoppingCartService = shoppingCartService;
        _translationService = translationService;
        _customerService = customerService;
        _permissionService = permissionService;
        _mediator = mediator;
        _shoppingCartSettings = shoppingCartSettings;
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITranslationService _translationService;
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Wishlist

    [HttpGet]
    public async Task<ActionResult<MiniWishlistModel>> SidebarWishlist()
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Content("");

        var cart = _contextAccessor.WorkContext.CurrentCustomer.ShoppingCartItems.Where(sci =>
            sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

        if (!string.IsNullOrEmpty(_contextAccessor.StoreContext.CurrentStore.Id))
            cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _contextAccessor.StoreContext.CurrentStore.Id);

        var model = await _mediator.Send(new GetMiniWishlist {
            Cart = cart.ToList(),
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return Json(model);
    }

    [HttpGet]
    public virtual async Task<ActionResult<WishlistModel>> Index(Guid? customerGuid)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return RedirectToRoute("HomePage");

        var customer = customerGuid.HasValue
            ? await _customerService.GetCustomerByGuid(customerGuid.Value)
            : _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null)
            return RedirectToRoute("HomePage");

        var cart = customer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

        if (!string.IsNullOrEmpty(_contextAccessor.StoreContext.CurrentStore.Id))
            cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _contextAccessor.StoreContext.CurrentStore.Id);

        var model = await _mediator.Send(new GetWishlist {
            Cart = cart.ToList(),
            Customer = customer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Store = _contextAccessor.StoreContext.CurrentStore,
            IsEditable = !customerGuid.HasValue,
            TaxDisplayType = _contextAccessor.WorkContext.TaxDisplayType
        });

        return View(model);
    }

    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> UpdateQuantity(UpdateQuantityModel model)
    {
        var warnings = new List<string>();
        if (ModelState.IsValid)
        {
            var cart =
                (await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id, ShoppingCartType.Wishlist))
                .FirstOrDefault(x => x.Id == model.ShoppingCartId);
            if (cart != null)
            {
                var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(
                    _contextAccessor.WorkContext.CurrentCustomer,
                    cart.Id, cart.WarehouseId, cart.Attributes, cart.EnteredPrice,
                    cart.RentalStartDateUtc, cart.RentalEndDateUtc,
                    model.Quantity);
                warnings.AddRange(currSciWarnings);
            }
        }
        else
        {
            warnings = ModelState.Values.SelectMany(x => x.Errors.Select(x => x.ErrorMessage)).ToList();
        }

        return Json(new
        {
            success = !warnings.Any(),
            warnings = string.Join(", ", warnings),
            totalproducts =
                (await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id, ShoppingCartType.Wishlist))
                .Sum(x => x.Quantity)
        });
    }

    [DenySystemAccount]
    [HttpPost]
    public virtual async Task<IActionResult> AddItemToCartFromWishlist(AddCartFromWishlistModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return Json(new { success = false, message = "No permission" });

        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Json(new { success = false, message = "No permission" });

        var pageCustomer = model.CustomerGuid.HasValue
            ? await _customerService.GetCustomerByGuid(model.CustomerGuid.Value)
            : _contextAccessor.WorkContext.CurrentCustomer;
        if (pageCustomer == null)
            return Json(new { success = false, message = "Customer not found" });

        var itemCart = pageCustomer.ShoppingCartItems
            .FirstOrDefault(
                sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == model.ShoppingCartId);

        if (itemCart == null)
            return Json(new { success = false, message = "Shopping cart ident not found" });

        var warnings = (await _shoppingCartService.AddToCart(_contextAccessor.WorkContext.CurrentCustomer,
            itemCart.ProductId, ShoppingCartType.ShoppingCart,
            _contextAccessor.StoreContext.CurrentStore.Id, itemCart.WarehouseId,
            itemCart.Attributes, itemCart.EnteredPrice,
            itemCart.RentalStartDateUtc, itemCart.RentalEndDateUtc, itemCart.Quantity,
            validator: new ShoppingCartValidatorOptions { GetRequiredProductWarnings = false })).warnings;

        if (warnings.Any())
            return Json(new { success = false, message = string.Join(',', warnings) });

        if (_shoppingCartSettings.MoveItemsFromWishlistToCart)
            await _shoppingCartService.DeleteShoppingCartItem(_contextAccessor.WorkContext.CurrentCustomer, itemCart);

        return Json(new { success = true, message = "" });
    }

    [DenySystemAccount]
    [HttpGet]
    public virtual async Task<IActionResult> DeleteItemFromWishlist(string shoppingCartId)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
            return Json(new { success = false, message = "No permission" });

        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
            return Json(new { success = false, message = "No permission" });

        var itemCart = _contextAccessor.WorkContext.CurrentCustomer.ShoppingCartItems
            .FirstOrDefault(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == shoppingCartId);

        if (itemCart == null)
            return Json(new { success = false, message = "Shopping cart ident not found" });

        await _shoppingCartService.DeleteShoppingCartItem(_contextAccessor.WorkContext.CurrentCustomer, itemCart);

        return Json(new { success = true, message = "" });
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [DenySystemAccount]
    public virtual async Task<IActionResult> EmailWishlist(WishlistEmailAFriendModel model,
        [FromServices] IMessageProviderService messageProviderService,
        [FromServices] CaptchaSettings captchaSettings)
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) ||
            !_shoppingCartSettings.EmailWishlistEnabled)
            return Content("");

        var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id, ShoppingCartType.Wishlist);
        if (!cart.Any())
            return Content("");

        if (ModelState.IsValid)
        {
            //email
            await messageProviderService.SendWishlistEmailAFriendMessage(_contextAccessor.WorkContext.CurrentCustomer,
                _contextAccessor.StoreContext.CurrentStore,
                _contextAccessor.WorkContext.WorkingLanguage.Id, model.YourEmailAddress,
                model.FriendEmail, FormatText.ConvertText(model.PersonalMessage));

            model.SuccessfullySent = true;
            model.Result = _translationService.GetResource("Wishlist.EmailAFriend.SuccessfullySent");

            return Json(model);
        }

        //If we got this far, something failed, redisplay form
        model.DisplayCaptcha = captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage;
        model.Result = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage));

        return Json(model);
    }

    #endregion
}