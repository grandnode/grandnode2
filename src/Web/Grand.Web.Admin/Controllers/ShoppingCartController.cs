using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Models.ShoppingCart;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.CurrentCarts)]
    public partial class ShoppingCartController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IStoreService _storeService;
        private readonly ITaxService _taxService;
        private readonly IPricingService _pricingService;
        private readonly ITranslationService _translationService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IProductService _productService;
        #endregion

        #region Constructors

        public ShoppingCartController(ICustomerService customerService,
            IDateTimeService dateTimeService,
            IPriceFormatter priceFormatter,
            IStoreService storeService,
            ITaxService taxService,
            IPricingService priceCalculationService,
            ITranslationService translationService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductService productService)
        {
            _customerService = customerService;
            _dateTimeService = dateTimeService;
            _priceFormatter = priceFormatter;
            _storeService = storeService;
            _taxService = taxService;
            _pricingService = priceCalculationService;
            _translationService = translationService;
            _productAttributeFormatter = productAttributeFormatter;
            _productService = productService;
        }

        #endregion

        #region Methods

        //shopping carts
        public IActionResult CurrentCarts() => View();

        [HttpPost]
        public async Task<IActionResult> CurrentCarts(DataSourceRequest command)
        {
            var customers = await _customerService.GetAllCustomers(
                loadOnlyWithShoppingCart: true,
                sct: ShoppingCartType.ShoppingCart,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize,
                orderBySelector: x => x.LastUpdateCartDateUtc);

            var gridModel = new DataSourceResult
            {
                Data = customers.Select(x => new ShoppingCartModel
                {
                    CustomerId = x.Id,
                    CustomerEmail = !string.IsNullOrEmpty(x.Email) ? x.Email : _translationService.GetResource("Admin.Customers.Guest"),
                    TotalItems = x.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart).Sum(y => y.Quantity)
                }),
                Total = customers.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetCartDetails(string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            var cart = customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart).ToList();
            var items = new List<ShoppingCartItemModel>();
            foreach (var sci in cart)
            {
                var store = await _storeService.GetStoreById(sci.StoreId);
                var product = await _productService.GetProductById(sci.ProductId);
                var sciModel = new ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Store = store != null ? store.Shortcut : "Unknown",
                    ProductId = sci.ProductId,
                    Quantity = sci.Quantity,
                    ProductName = product.Name,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes, customer),
                    UnitPrice = _priceFormatter.FormatPrice((await _taxService.GetProductPrice(product, (await _pricingService.GetUnitPrice(sci, product)).unitprice)).productprice),
                    Total = _priceFormatter.FormatPrice((await _taxService.GetProductPrice(product, (await _pricingService.GetSubTotal(sci, product)).subTotal)).productprice),
                    UpdatedOn = _dateTimeService.ConvertToUserTime(sci.UpdatedOnUtc, DateTimeKind.Utc)
                };
                items.Add(sciModel);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = cart.Count
            };
            return Json(gridModel);
        }

        //wishlists
        [HttpPost]
        public async Task<IActionResult> CurrentWishlists(DataSourceRequest command)
        {
            var customers = await _customerService.GetAllCustomers(
                loadOnlyWithShoppingCart: true,
                sct: ShoppingCartType.Wishlist,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize,
                orderBySelector: x => x.LastUpdateWishListDateUtc);

            var gridModel = new DataSourceResult
            {
                Data = customers.Select(x => new ShoppingCartModel
                {
                    CustomerId = x.Id,
                    CustomerEmail = !string.IsNullOrEmpty(x.Email) ? x.Email : _translationService.GetResource("Admin.Customers.Guest"),
                    TotalItems = x.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.Wishlist).Sum(y => y.Quantity)
                }),
                Total = customers.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetWishlistDetails(string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            var cart = customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == ShoppingCartType.Wishlist).ToList();
            var items = new List<ShoppingCartItemModel>();
            foreach (var sci in cart)
            {
                var store = await _storeService.GetStoreById(sci.StoreId);
                var product = await _productService.GetProductById(sci.ProductId);
                var sciModel = new ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Store = store != null ? store.Shortcut : "Unknown",
                    ProductId = sci.ProductId,
                    Quantity = sci.Quantity,
                    ProductName = product.Name,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes, customer),
                    UnitPrice = _priceFormatter.FormatPrice((await _taxService.GetProductPrice(product, (await _pricingService.GetUnitPrice(sci, product)).unitprice)).productprice),
                    Total = _priceFormatter.FormatPrice((await _taxService.GetProductPrice(product, (await _pricingService.GetSubTotal(sci, product)).subTotal)).productprice),
                    UpdatedOn = _dateTimeService.ConvertToUserTime(sci.UpdatedOnUtc, DateTimeKind.Utc)
                };
                items.Add(sciModel);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = cart.Count
            };

            return Json(gridModel);
        }

        #endregion
    }
}
