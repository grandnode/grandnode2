using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetWishlistHandler : IRequestHandler<GetWishlist, WishlistModel>
    {
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITranslationService _translationService;
        private readonly ITaxService _taxService;
        private readonly IPricingService _pricingService;
        private readonly IAclService _aclService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPictureService _pictureService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;

        public GetWishlistHandler(
            IPermissionService permissionService,
            IShoppingCartService shoppingCartService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter,
            ITranslationService translationService,
            ITaxService taxService,
            IPricingService priceCalculationService,
            IAclService aclService,
            IPriceFormatter priceFormatter,
            IPictureService pictureService,
            IShoppingCartValidator shoppingCartValidator,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings)
        {
            _permissionService = permissionService;
            _shoppingCartService = shoppingCartService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _productAttributeFormatter = productAttributeFormatter;
            _translationService = translationService;
            _taxService = taxService;
            _pricingService = priceCalculationService;
            _aclService = aclService;
            _priceFormatter = priceFormatter;
            _pictureService = pictureService;
            _shoppingCartValidator = shoppingCartValidator;
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<WishlistModel> Handle(GetWishlist request, CancellationToken cancellationToken)
        {
            var model = new WishlistModel
            {
                EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
                IsEditable = request.IsEditable,
                DisplayAddToCart = await _permissionService.Authorize(StandardPermission.EnableShoppingCart),
            };

            if (!request.Cart.Any())
                return model;

            #region Simple properties

            model.CustomerGuid = request.Customer.CustomerGuid;
            model.CustomerFullname = request.Customer.GetFullName();
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnWishList;
            model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;

            //cart warnings
            var cartWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(request.Cart, null, false);
            foreach (var warning in cartWarnings)
                model.Warnings.Add(warning);

            #endregion

            #region Cart items

            foreach (var sci in request.Cart)
            {
                var product = await _productService.GetProductById(sci.ProductId);
                if (!_aclService.Authorize(product, request.Customer))
                    continue;

                var sename = product.GetSeName(request.Language.Id);
                var cartItemModel = new WishlistModel.ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Sku = product.FormatSku(sci.Attributes, _productAttributeParser),
                    ProductId = product.Id,
                    ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                    ProductSeName = sename,
                    ProductUrl = _linkGenerator.GetUriByRouteValues(_httpContextAccessor.HttpContext, "Product", new { SeName = sename }),
                    Quantity = sci.Quantity,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes),
                };

                cartItemModel.AllowItemEditing = _shoppingCartSettings.AllowCartItemEditing && product.ProductTypeId == ProductType.SimpleProduct && (!String.IsNullOrEmpty(cartItemModel.AttributeInfo) || product.IsGiftVoucher) && product.VisibleIndividually;

                //allowed quantities
                var allowedQuantities = product.ParseAllowedQuantities();
                foreach (var qty in allowedQuantities)
                {
                    cartItemModel.AllowedQuantities.Add(new SelectListItem
                    {
                        Text = qty.ToString(),
                        Value = qty.ToString(),
                        Selected = sci.Quantity == qty
                    });
                }

                //recurring info
                if (product.IsRecurring)
                    cartItemModel.RecurringInfo = string.Format(_translationService.GetResource("ShoppingCart.RecurringPeriod"), product.RecurringCycleLength, product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, request.Language.Id));

                //unit prices
                if (product.CallForPrice)
                {
                    cartItemModel.UnitPrice = _translationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    var productprice = await _taxService.GetProductPrice(product, (await _pricingService.GetUnitPrice(sci, product)).unitprice);
                    double taxRate = productprice.taxRate;
                    cartItemModel.UnitPrice = _priceFormatter.FormatPrice(productprice.productprice);
                }
                //subtotal, discount
                if (product.CallForPrice)
                {
                    cartItemModel.SubTotal = _translationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    //sub total
                    var subtotal = await _pricingService.GetSubTotal(sci, product, true);
                    double shoppingCartItemDiscountBase = subtotal.discountAmount;
                    List<ApplyDiscount> scDiscounts = subtotal.appliedDiscounts;
                    var productprices = await _taxService.GetProductPrice(product, subtotal.subTotal);
                    double taxRate = productprices.taxRate;
                    cartItemModel.SubTotal = _priceFormatter.FormatPrice(productprices.productprice);

                    //display an applied discount amount
                    if (shoppingCartItemDiscountBase > 0)
                    {
                        shoppingCartItemDiscountBase = (await _taxService.GetProductPrice(product, shoppingCartItemDiscountBase)).productprice;
                        if (shoppingCartItemDiscountBase > 0)
                        {
                            cartItemModel.Discount = _priceFormatter.FormatPrice(shoppingCartItemDiscountBase);
                        }
                    }
                }

                //picture
                if (_shoppingCartSettings.ShowProductImagesOnWishList)
                {
                    cartItemModel.Picture = await PrepareCartItemPicture(request, product, sci.Attributes);
                }

                //item warnings
                var itemWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(request.Customer, sci, product, new ShoppingCartValidatorOptions());
                foreach (var warning in itemWarnings)
                    cartItemModel.Warnings.Add(warning);

                model.Items.Add(cartItemModel);
            }

            #endregion

            return model;
        }

        private async Task<PictureModel> PrepareCartItemPicture(GetWishlist request,
            Product product, IList<CustomAttribute> attributes)
        {
            var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService, _productAttributeParser);
            return new PictureModel
            {
                Id = sciPicture?.Id,
                ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.CartThumbPictureSize, true),
                Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), product.Name),
                AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"), product.Name),
            };
        }

    }
}
