using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class EmailWishlistViewComponent : BaseViewComponent
{
    private readonly CaptchaSettings _captchaSettings;
    private readonly IPermissionService _permissionService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IContextAccessor _contextAccessor;

    public EmailWishlistViewComponent(
        IContextAccessor contextAccessor,
        IShoppingCartService shoppingCartService,
        IPermissionService permissionService,
        ShoppingCartSettings shoppingCartSettings,
        CaptchaSettings captchaSettings
    )
    {
        _contextAccessor = contextAccessor;
        _shoppingCartService = shoppingCartService;
        _permissionService = permissionService;
        _shoppingCartSettings = shoppingCartSettings;
        _captchaSettings = captchaSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) ||
            !_shoppingCartSettings.EmailWishlistEnabled)
            return Content("");

        var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id, ShoppingCartType.Wishlist);

        if (!cart.Any())
            return Content("");

        var model = new WishlistEmailAFriendModel {
            YourEmailAddress = _contextAccessor.WorkContext.CurrentCustomer.Email,
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailWishlistToFriendPage
        };
        return View(model);
    }
}