﻿using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

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

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);

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
