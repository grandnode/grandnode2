using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Web.Controllers
{
    public partial class WishlistController : BasePublicController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IPermissionService _permissionService;
        private readonly IMediator _mediator;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Constructors

        public WishlistController(
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            ITranslationService translationService,
            ICustomerService customerService,
            IGroupService groupService,
            IPermissionService permissionService,
            IMediator mediator,
            ShoppingCartSettings shoppingCartSettings)
        {
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _translationService = translationService;
            _customerService = customerService;
            _groupService = groupService;
            _permissionService = permissionService;
            _mediator = mediator;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Wishlist


        public async Task<IActionResult> SidebarWishlist()
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return Content("");

            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

            if (!string.IsNullOrEmpty(_workContext.CurrentStore.Id))
                cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id);

            var model = await _mediator.Send(new GetMiniWishlist() {
                Cart = cart.ToList(),
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
            });

            return Json(model);
        }

        [HttpGet]
        public virtual async Task<IActionResult> Index(Guid? customerGuid)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return RedirectToRoute("HomePage");

            Customer customer = customerGuid.HasValue ?
                await _customerService.GetCustomerByGuid(customerGuid.Value)
                : _workContext.CurrentCustomer;
            if (customer == null)
                return RedirectToRoute("HomePage");

            var cart = customer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);

            if (!string.IsNullOrEmpty(_workContext.CurrentStore.Id))
                cart = cart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id);

            var model = await _mediator.Send(new GetWishlist() {
                Cart = cart.ToList(),
                Customer = customer,
                Language = _workContext.WorkingLanguage,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
                IsEditable = !customerGuid.HasValue,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return View(model);
        }

        [AutoValidateAntiforgeryToken]
        [DenySystemAccount]
        [HttpPost]
        public virtual async Task<IActionResult> UpdateQuantity(string shoppingcartId, int quantity)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return RedirectToRoute("HomePage");

            var cart = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist)).FirstOrDefault(x => x.Id == shoppingcartId);
            if (cart == null)
            {
                return Json(new
                {
                    success = false,
                    warnings = "Shopping cart item not found",
                });
            }
            if (quantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    warnings = "Wrong quantity ",
                });
            }


            var warnings = new List<string>();
            var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                cart.Id, cart.WarehouseId, cart.Attributes, cart.EnteredPrice,
                cart.RentalStartDateUtc, cart.RentalEndDateUtc,
                quantity, true);
            warnings.AddRange(currSciWarnings);

            return Json(new
            {
                success = !warnings.Any(),
                warnings = string.Join(", ", warnings),
                totalproducts = (await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist)).Sum(x => x.Quantity),
            });

        }

        [DenySystemAccount]
        [HttpPost]
        public virtual async Task<IActionResult> AddItemToCartFromWishlist(Guid? customerGuid, string shoppingcartId)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return Json(new { success = false, message = "No permission" });

            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return Json(new { success = false, message = "No permission" });

            var pageCustomer = customerGuid.HasValue
                ? await _customerService.GetCustomerByGuid(customerGuid.Value)
                : _workContext.CurrentCustomer;
            if (pageCustomer == null)
                return Json(new { success = false, message = "Customer not found" });

            var itemCart = pageCustomer.ShoppingCartItems
                .FirstOrDefault(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == shoppingcartId);

            if (itemCart == null)
                return Json(new { success = false, message = "Shopping cart ident not found" });

            var warnings = (await _shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                       itemCart.ProductId, ShoppingCartType.ShoppingCart,
                       _workContext.CurrentStore.Id, itemCart.WarehouseId,
                       itemCart.Attributes, itemCart.EnteredPrice,
                       itemCart.RentalStartDateUtc, itemCart.RentalEndDateUtc, itemCart.Quantity, true,
                       validator: new ShoppingCartValidatorOptions() { GetRequiredProductWarnings = false })).warnings;

            if (warnings.Any())
                return Json(new { success = false, message = string.Join(',', warnings) });

            if (_shoppingCartSettings.MoveItemsFromWishlistToCart)
                await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, itemCart);

            return Json(new { success = true, message = "" });

        }

        [DenySystemAccount]
        [HttpPost]
        public virtual async Task<IActionResult> DeleteItemFromWishlist(string shoppingcartId)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return Json(new { success = false, message = "No permission" });

            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return Json(new { success = false, message = "No permission" });

            var itemCart = _workContext.CurrentCustomer.ShoppingCartItems
                .FirstOrDefault(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist && sci.Id == shoppingcartId);

            if (itemCart == null)
                return Json(new { success = false, message = "Shopping cart ident not found" });

            await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, itemCart);

            return Json(new { success = true, message = "" });

        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [DenySystemAccount]
        public virtual async Task<IActionResult> EmailWishlist(WishlistEmailAFriendModel model, bool captchaValid,
            [FromServices] IMessageProviderService messageProviderService,
            [FromServices] CaptchaSettings captchaSettings)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) || !_shoppingCartSettings.EmailWishlistEnabled)
                return Content("");

            var cart = await _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);
            if (!cart.Any())
                return Content("");

            //validate CAPTCHA
            if (captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage && !captchaValid)
            {
                ModelState.AddModelError("", captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            //check whether the current customer is guest and ia allowed to email wishlist
            if (await _groupService.IsGuest(_workContext.CurrentCustomer) && !_shoppingCartSettings.AllowAnonymousUsersToEmailWishlist)
            {
                ModelState.AddModelError("", _translationService.GetResource("Wishlist.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (ModelState.IsValid)
            {
                //email
                await messageProviderService.SendWishlistEmailAFriendMessage(_workContext.CurrentCustomer, _workContext.CurrentStore,
                        _workContext.WorkingLanguage.Id, model.YourEmailAddress,
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
}