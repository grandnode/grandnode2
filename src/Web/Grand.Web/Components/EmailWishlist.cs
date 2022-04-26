using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.ViewComponents
{
    public class EmailWishlistViewComponent : BaseViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPermissionService _permissionService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CaptchaSettings _captchaSettings;

        public EmailWishlistViewComponent(
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            IPermissionService permissionService,
            ShoppingCartSettings shoppingCartSettings,
            CaptchaSettings captchaSettings
            )
        {
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _permissionService = permissionService;
            _shoppingCartSettings = shoppingCartSettings;
            _captchaSettings = captchaSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) || !_shoppingCartSettings.EmailWishlistEnabled)
                return Content("");

            var cart = await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);

            if (!cart.Any())
                return Content("");

            var model = new WishlistEmailAFriendModel {
                YourEmailAddress = _workContext.CurrentCustomer.Email,
                DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnEmailWishlistToFriendPage
            };
            return View(model);
        }

    }
}
