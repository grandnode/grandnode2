using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class OutOfStockSubscriptionController : BasePublicController
{
    #region Constructors

    public OutOfStockSubscriptionController(IProductService productService,
        IContextAccessor contextAccessor,
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
        _contextAccessor = contextAccessor;
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

    #region Fields

    private readonly IProductService _productService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly IMediator _mediator;
    private readonly CustomerSettings _customerSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion

    #region Methods

    [HttpGet]
    // Product details page > out of stock subscribe button
    public virtual async Task<IActionResult> SubscribeButton(string productId, string warehouseId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (!await _groupService.IsRegistered(customer))
            return Content(_translationService.GetResource("OutOfStockSubscriptions.OnlyRegistered"));

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock)
            return Content(_translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"));

        warehouseId = _shoppingCartSettings.AllowToSelectWarehouse
            ? string.IsNullOrEmpty(warehouseId) ? "" : warehouseId
            : string.IsNullOrEmpty(_contextAccessor.StoreContext.CurrentStore.DefaultWarehouseId)
                ? product.WarehouseId
                : _contextAccessor.StoreContext.CurrentStore.DefaultWarehouseId;

        var subscription = await _outOfStockSubscriptionService
            .FindSubscription(customer.Id, product.Id, null, _contextAccessor.StoreContext.CurrentStore.Id,
                warehouseId);

        return Content(subscription != null
            ? _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable")
            : _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"));
    }

    [HttpPost]
    public virtual async Task<IActionResult> SubscribePopup(ProductModel model)
    {
        var product = await _productService.GetProductById(model.ProductId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ? model.WarehouseId :
            product.UseMultipleWarehouses ? _contextAccessor.StoreContext.CurrentStore.DefaultWarehouseId :
            string.IsNullOrEmpty(_contextAccessor.StoreContext.CurrentStore.DefaultWarehouseId) ? product.WarehouseId :
            _contextAccessor.StoreContext.CurrentStore.DefaultWarehouseId;

        if (!await _groupService.IsRegistered(customer))
            return Json(new {
                subscribe = false,
                buttontext = _translationService.GetResource("OutOfStockSubscriptions.OnlyRegistered"),
                resource = _translationService.GetResource("OutOfStockSubscriptions.OnlyRegisteredText")
            });

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId) <= 0)
        {
            var subscription = await _outOfStockSubscriptionService
                .FindSubscription(customer.Id, product.Id, null, _contextAccessor.StoreContext.CurrentStore.Id, warehouseId);
            if (subscription != null)
            {
                //subscription already exists
                //unsubscribe
                await _outOfStockSubscriptionService.DeleteSubscription(subscription);
                return Json(new {
                    subscribe = false,
                    buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                    resource = _translationService.GetResource("OutOfStockSubscriptions.Unsubscribed")
                });
            }

            //subscription does not exist
            //subscribe
            subscription = new OutOfStockSubscription {
                CustomerId = customer.Id,
                ProductId = product.Id,
                StoreId = _contextAccessor.StoreContext.CurrentStore.Id,
                WarehouseId = warehouseId
            };
            await _outOfStockSubscriptionService.InsertSubscription(subscription);
            return Json(new {
                subscribe = true,
                buttontext = _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable"),
                resource = _translationService.GetResource("OutOfStockSubscriptions.Subscribed")
            });
        }

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes ||
            product.BackorderModeId != BackorderMode.NoBackorders ||
            !product.AllowOutOfStockSubscriptions)
            return Json(new {
                subscribe = false,
                buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                resource = _translationService.GetResource("OutOfStockSubscriptions.NotAllowed")
            });

        var attributes = await _mediator.Send(new GetParseProductAttributes
            { Product = product, Attributes = model.Attributes });
        var subscriptionAttributes = await _outOfStockSubscriptionService
            .FindSubscription(customer.Id, product.Id, attributes, _contextAccessor.StoreContext.CurrentStore.Id, warehouseId);

        if (subscriptionAttributes != null)
        {
            //subscription already exists
            //unsubscribe
            await _outOfStockSubscriptionService.DeleteSubscription(subscriptionAttributes);
            return Json(new {
                subscribe = false,
                buttontext = _translationService.GetResource("OutOfStockSubscriptions.NotifyMeWhenAvailable"),
                resource = _translationService.GetResource("OutOfStockSubscriptions.Unsubscribed")
            });
        }

        subscriptionAttributes = new OutOfStockSubscription {
            CustomerId = customer.Id,
            ProductId = product.Id,
            Attributes = attributes,
            AttributeInfo = !attributes.Any()
                ? ""
                : await _productAttributeFormatter.FormatAttributes(product, attributes),
            StoreId = _contextAccessor.StoreContext.CurrentStore.Id,
            WarehouseId = warehouseId
        };

        await _outOfStockSubscriptionService.InsertSubscription(subscriptionAttributes);
        return Json(new {
            subscribe = true,
            buttontext = _translationService.GetResource("OutOfStockSubscriptions.DeleteNotifyWhenAvailable"),
            resource = _translationService.GetResource("OutOfStockSubscriptions.Subscribed")
        });
    }


    // My account / Out of stock subscriptions
    [HttpGet]
    public virtual async Task<IActionResult> CustomerSubscriptions(int? pageNumber)
    {
        if (_customerSettings.HideOutOfStockSubscriptionsTab) return RedirectToRoute("CustomerInfo");

        var pageIndex = 0;
        if (pageNumber > 0) pageIndex = pageNumber.Value - 1;

        const int pageSize = 10;

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var list = await _outOfStockSubscriptionService.GetAllSubscriptionsByCustomerId(customer.Id,
            _contextAccessor.StoreContext.CurrentStore.Id, pageIndex, pageSize);

        var model = new CustomerOutOfStockSubscriptionsModel();

        foreach (var subscription in list)
        {
            var product = await _productService.GetProductById(subscription.ProductId);
            if (product == null) continue;
            var subscriptionModel = new CustomerOutOfStockSubscriptionsModel.OutOfStockSubscriptionModel {
                Id = subscription.Id,
                ProductId = product.Id,
                ProductName = product.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id),
                AttributeDescription = !subscription.Attributes.Any()
                    ? ""
                    : await _productAttributeFormatter.FormatAttributes(product, subscription.Attributes),
                SeName = product.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id)
            };
            model.Subscriptions.Add(subscriptionModel);
        }

        model.PagerModel.LoadPagedList(list);

        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> CustomerSubscriptions(string[] subscriptions)
    {
        foreach (var id in subscriptions)
        {
            var subscription = await _outOfStockSubscriptionService.GetSubscriptionById(id);
            if (subscription != null && subscription.CustomerId == _contextAccessor.WorkContext.CurrentCustomer.Id)
                await _outOfStockSubscriptionService.DeleteSubscription(subscription);
        }

        return RedirectToRoute("CustomerOutOfStockSubscriptions");
    }

    #endregion
}