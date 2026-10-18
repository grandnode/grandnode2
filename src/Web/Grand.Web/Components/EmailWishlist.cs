using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Components;

public class EmailWishlistViewComponent : BaseViewComponent
{
    private readonly CaptchaSettings _captchaSettings;
    private readonly IRequestPermissionService _permissionService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IContextAccessor _contextAccessor;

    public EmailWishlistViewComponent(
        IContextAccessor contextAccessor,
        IShoppingCartService shoppingCartService,
        IRequestPermissionService permissionService,
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

        var cart = await _shoppingCartService.GetCurrentCart(_contextAccessor, ShoppingCartType.Wishlist);

        if (!cart.Any())
            return Content("");

        var model = new WishlistEmailAFriendModel {
            YourEmailAddress = _contextAccessor.WorkContext.CurrentCustomer.Email,
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailWishlistToFriendPage
        };
        return View(model);
    }
}