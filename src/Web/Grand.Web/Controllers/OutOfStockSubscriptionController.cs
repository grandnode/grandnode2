using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Directory;

namespace Grand.Web.Controllers
{
    public partial class OutOfStockSubscriptionController : BasePublicController
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ITranslationService _translationService;
        private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly IMediator _mediator;
        private readonly CustomerSettings _customerSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Constructors

        public OutOfStockSubscriptionController(IProductService productService,
            IWorkContext workContext,
            IGroupService groupService,
            ITranslationService translationService,
            IOutOfStockSubscriptionService outOfStockSubscriptionService,
            IProductAttributeFormatter productAttributeFormatter,
            IStockQuantityService stockQuantityService,
            IMediator mediator,
            CustomerSettings customerSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _productService = productService;
            _workContext = workContext;
            _groupService = groupService;
            _translationService = translationService;
            _outOfStockSubscriptionService = outOfStockSubscriptionService;
            _productAttributeFormatter = productAttributeFormatter;
            _stockQuantityService = stockQuantityService;
            _mediator = mediator;
            _customerSettings = customerSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        // Product details page > out of stock subscribe button
        public virtual async Task<IActionResult> SubscribeButton(string productId, string warehouseId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            var customer = _workContext.CurrentCustomer;
            if (!await _groupService.IsRegistered(customer))
                return Content(_translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"));

            if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock)
                return Content(_translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"));

            warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
               (string.IsNullOrEmpty(warehouseId) ? "" : warehouseId) :
               (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);

            var subscription = await _outOfStockSubscriptionService
                   .FindSubscription(customer.Id, product.Id, null, _workContext.CurrentStore.Id,
                   warehouseId);

            if (subscription != null)
            {
                return Content(_translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable"));
            }
            return Content(_translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"));
        }

        [HttpPost, ActionName("SubscribePopup")]
        public virtual async Task<IActionResult> SubscribePopup(string productId, IFormCollection form)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            var customer = _workContext.CurrentCustomer;

            string warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ?
                form["WarehouseId"].ToString() :
                 product.UseMultipleWarehouses ? _workContext.CurrentStore.DefaultWarehouseId :
                 (string.IsNullOrEmpty(_workContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId);

            if (!await _groupService.IsRegistered(customer))
                return Json(new
                {
                    subscribe = false,
                    buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                    resource = _translationService.GetResource("OutOfStockSubscriptions.OnlyRegistered")
                });

            if ((product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock) &&
                product.BackorderModeId == BackorderMode.NoBackorders &&
                product.AllowOutOfStockSubscriptions &&
                _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId) <= 0)
            {
                var subscription = await _outOfStockSubscriptionService
                    .FindSubscription(customer.Id, product.Id, null, _workContext.CurrentStore.Id, warehouseId);
                if (subscription != null)
                {
                    //subscription already exists
                    //unsubscribe
                    await _outOfStockSubscriptionService.DeleteSubscription(subscription);
                    return Json(new
                    {
                        subscribe = false,
                        buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                        resource = _translationService.GetResource("OutOfStockSubscriptions.Unsubscribed")
                    });

                }

                //subscription does not exist
                //subscribe
                subscription = new OutOfStockSubscription
                {
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    StoreId = _workContext.CurrentStore.Id,
                    WarehouseId = warehouseId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _outOfStockSubscriptionService.InsertSubscription(subscription);
                return Json(new
                {
                    subscribe = true,
                    buttontext = _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable"),
                    resource = _translationService.GetResource("OutOfStockSubscriptions.Subscribed")
                });
            }

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes &&
                product.BackorderModeId == BackorderMode.NoBackorders &&
                product.AllowOutOfStockSubscriptions)
            {
                var attributes = await _mediator.Send(new GetParseProductAttributes() { Product = product, Form = form });
                var subscription = await _outOfStockSubscriptionService
                    .FindSubscription(customer.Id, product.Id, attributes, _workContext.CurrentStore.Id, warehouseId);

                if (subscription != null)
                {
                    //subscription already exists
                    //unsubscribe
                    await _outOfStockSubscriptionService.DeleteSubscription(subscription);
                    return Json(new
                    {
                        subscribe = false,
                        buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                        resource = _translationService.GetResource("OutOfStockSubscriptions.Unsubscribed")
                    });
                }

                //subscription does not exist
                //subscribe
                
                subscription = new OutOfStockSubscription
                {
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    Attributes = attributes,
                    AttributeInfo = !attributes.Any() ? "" : await _productAttributeFormatter.FormatAttributes(product, attributes),
                    StoreId = _workContext.CurrentStore.Id,
                    WarehouseId = warehouseId,
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _outOfStockSubscriptionService.InsertSubscription(subscription);
                return Json(new
                {
                    subscribe = true,
                    buttontext = _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable"),
                    resource = _translationService.GetResource("OutOfStockSubscriptions.Subscribed")
                });
            }

            return Json(new
            {
                subscribe = false,
                buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                resource = _translationService.GetResource("OutOfStockSubscriptions.NotAllowed")
            });
        }


        // My account / Out of stock subscriptions
        public virtual async Task<IActionResult> CustomerSubscriptions(int? pageNumber)
        {
            if (_customerSettings.HideOutOfStockSubscriptionsTab)
            {
                return RedirectToRoute("CustomerInfo");
            }

            int pageIndex = 0;
            if (pageNumber > 0)
            {
                pageIndex = pageNumber.Value - 1;
            }
            var pageSize = 10;

            var customer = _workContext.CurrentCustomer;
            var list = await _outOfStockSubscriptionService.GetAllSubscriptionsByCustomerId(customer.Id,
                _workContext.CurrentStore.Id, pageIndex, pageSize);

            var model = new CustomerOutOfStockSubscriptionsModel();

            foreach (var subscription in list)
            {
                var product = await _productService.GetProductById(subscription.ProductId);
                if (product != null)
                {
                    var subscriptionModel = new CustomerOutOfStockSubscriptionsModel.OutOfStockSubscriptionModel
                    {
                        Id = subscription.Id,
                        ProductId = product.Id,
                        ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        AttributeDescription = !subscription.Attributes.Any() ? "" : await _productAttributeFormatter.FormatAttributes(product, subscription.Attributes),
                        SeName = product.GetSeName(_workContext.WorkingLanguage.Id),
                    };
                    model.Subscriptions.Add(subscriptionModel);
                }
            }
            model.PagerModel.LoadPagedList(list);

            return View(model);
        }
        [HttpPost, ActionName("CustomerSubscriptions")]
        public virtual async Task<IActionResult> CustomerSubscriptionsPOST(IFormCollection formCollection)
        {
            foreach (var key in formCollection.Keys)
            {
                var value = formCollection[key];

                if (value.Equals("on") && key.StartsWith("biss", StringComparison.OrdinalIgnoreCase))
                {
                    var id = key.Replace("biss", "").Trim();
                    var subscription = await _outOfStockSubscriptionService.GetSubscriptionById(id);
                    if (subscription != null && subscription.CustomerId == _workContext.CurrentCustomer.Id)
                    {
                        await _outOfStockSubscriptionService.DeleteSubscription(subscription);
                    }
                }
            }

            return RedirectToRoute("CustomerOutOfStockSubscriptions");
        }

        #endregion
    }
}
