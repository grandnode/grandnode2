using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var model = await _mediator.Send(new GetWishlist()
            {
                Cart = cart.ToList(),
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Currency = _workContext.WorkingCurrency,
                Store = _workContext.CurrentStore,
                IsEditable = !customerGuid.HasValue,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> UpdateWishlist(IFormCollection form)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return RedirectToRoute("HomePage");

            var customer = _workContext.CurrentCustomer;

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);

            var allIdsToRemove = !string.IsNullOrEmpty(form["removefromcart"].ToString())
                ? form["removefromcart"].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToList()
                : new List<string>();

            //current warnings <cart item identifier, warnings>
            var innerWarnings = new Dictionary<string, IList<string>>();
            foreach (var sci in cart)
            {
                bool remove = allIdsToRemove.Contains(sci.Id);
                if (remove)
                    await _shoppingCartService.DeleteShoppingCartItem(customer, sci);
                else
                {
                    foreach (string formKey in form.Keys)
                        if (formKey.Equals(string.Format("itemquantity{0}", sci.Id), StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(form[formKey], out int newQuantity))
                            {
                                var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                                    sci.Id, sci.WarehouseId, sci.Attributes, sci.EnteredPrice,
                                    sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                                    newQuantity, true);
                                innerWarnings.Add(sci.Id, currSciWarnings);
                            }
                            break;
                        }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid, IFormCollection form)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist))
                return RedirectToRoute("HomePage");

            var pageCustomer = customerGuid.HasValue
                ? await _customerService.GetCustomerByGuid(customerGuid.Value)
                : _workContext.CurrentCustomer;
            if (pageCustomer == null)
                return RedirectToRoute("HomePage");

            var pageCart = pageCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist);
            if (!string.IsNullOrEmpty(_workContext.CurrentStore.Id))
                pageCart = pageCart.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id);

            var allWarnings = new List<string>();
            var numberOfAddedItems = 0;
            var allIdsToAdd = form.ContainsKey("addtocart") ? form["addtocart"].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToList()
                : new List<string>();
            foreach (var sci in pageCart.ToList())
            {
                if (allIdsToAdd.Contains(sci.Id))
                {
                    var warnings = await _shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                        sci.ProductId, ShoppingCartType.ShoppingCart,
                        _workContext.CurrentStore.Id, sci.WarehouseId,
                        sci.Attributes, sci.EnteredPrice,
                        sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, true, getRequiredProductWarnings: false);
                    if (!warnings.Any())
                        numberOfAddedItems++;
                    if (_shoppingCartSettings.MoveItemsFromWishlistToCart && //settings enabled
                        !customerGuid.HasValue && 
                        !warnings.Any()) 
                    {
                        await _shoppingCartService.DeleteShoppingCartItem(_workContext.CurrentCustomer, sci);
                    }
                    allWarnings.AddRange(warnings);
                }
            }

            if (numberOfAddedItems > 0)
            {
                //redirect to the shopping cart page

                if (allWarnings.Any())
                {
                    Error(_translationService.GetResource("Wishlist.AddToCart.Error"), true);
                }

                return RedirectToRoute("ShoppingCart");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public virtual async Task<IActionResult> EmailWishlist([FromServices] CaptchaSettings captchaSettings)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) || !_shoppingCartSettings.EmailWishlistEnabled)
                return RedirectToRoute("HomePage");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);

            if (!cart.Any())
                return RedirectToRoute("HomePage");

            var model = new WishlistEmailAFriendModel
            {
                YourEmailAddress = _workContext.CurrentCustomer.Email,
                DisplayCaptcha = captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage
            };
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        public virtual async Task<IActionResult> EmailWishlist(WishlistEmailAFriendModel model, bool captchaValid,
            [FromServices] IMessageProviderService messageProviderService,
            [FromServices] CaptchaSettings captchaSettings)
        {
            if (!await _permissionService.Authorize(StandardPermission.EnableWishlist) || !_shoppingCartSettings.EmailWishlistEnabled)
                return RedirectToRoute("HomePage");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.Wishlist);
            if (!cart.Any())
                return RedirectToRoute("HomePage");

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

                return View(model);
            }

            //If we got this far, something failed, redisplay form
            model.DisplayCaptcha = captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage;
            return View(model);
        }

        #endregion
    }
}