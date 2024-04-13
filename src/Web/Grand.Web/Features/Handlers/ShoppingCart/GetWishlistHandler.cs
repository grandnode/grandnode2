using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Features.Handlers.ShoppingCart;

public class GetWishlistHandler : IRequestHandler<GetWishlist, WishlistModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly LinkGenerator _linkGenerator;
    private readonly MediaSettings _mediaSettings;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly ITaxService _taxService;
    private readonly ITranslationService _translationService;

    public GetWishlistHandler(
        IPermissionService permissionService,
        IProductService productService,
        IProductAttributeFormatter productAttributeFormatter,
        ITranslationService translationService,
        ITaxService taxService,
        IPricingService priceCalculationService,
        IPriceFormatter priceFormatter,
        IPictureService pictureService,
        IShoppingCartValidator shoppingCartValidator,
        LinkGenerator linkGenerator,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings,
        MediaSettings mediaSettings)
    {
        _permissionService = permissionService;
        _productService = productService;
        _productAttributeFormatter = productAttributeFormatter;
        _translationService = translationService;
        _taxService = taxService;
        _pricingService = priceCalculationService;
        _priceFormatter = priceFormatter;
        _pictureService = pictureService;
        _shoppingCartValidator = shoppingCartValidator;
        _linkGenerator = linkGenerator;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<WishlistModel> Handle(GetWishlist request, CancellationToken cancellationToken)
    {
        var model = new WishlistModel {
            EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
            IsEditable = request.IsEditable,
            DisplayAddToCart = await _permissionService.Authorize(StandardPermission.EnableShoppingCart)
        };

        if (!request.Cart.Any())
            return model;

        #region Simple properties

        model.CustomerGuid = request.Customer.CustomerGuid;
        model.CustomerFullname = request.Customer.GetFullName();
        model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnWishList;
        model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;

        //cart warnings
        var cartWarnings = await _shoppingCartValidator.GetShoppingCartWarnings(request.Cart, null, false, false);
        foreach (var warning in cartWarnings)
            model.Warnings.Add(warning);

        #endregion

        #region Cart items

        foreach (var sci in request.Cart)
        {
            var product = await _productService.GetProductById(sci.ProductId);
            if (product == null)
                continue;

            var sename = product.GetSeName(request.Language.Id);
            var cartItemModel = new WishlistModel.ShoppingCartItemModel {
                Id = sci.Id,
                Sku = product.FormatSku(sci.Attributes),
                ProductId = product.Id,
                ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                ProductSeName = sename,
                ProductUrl = _linkGenerator.GetPathByRouteValues("Product", new { SeName = sename }),
                Quantity = sci.Quantity,
                AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes),
                AllowItemEditing = _shoppingCartSettings.AllowCartItemEditing && product.VisibleIndividually
            };

            //allowed quantities
            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
                cartItemModel.AllowedQuantities.Add(new SelectListItem {
                    Text = qty.ToString(),
                    Value = qty.ToString(),
                    Selected = sci.Quantity == qty
                });

            //recurring info
            if (product.IsRecurring)
                cartItemModel.RecurringInfo = string.Format(
                    _translationService.GetResource("ShoppingCart.RecurringPeriod"),
                    product.RecurringCycleLength,
                    product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, request.Language.Id),
                    product.RecurringTotalCycles);

            //unit prices
            if (product.CallForPrice)
            {
                cartItemModel.UnitPrice = _translationService.GetResource("Products.CallForPrice");
            }
            else
            {
                var productprice = await _taxService.GetProductPrice(product,
                    (await _pricingService.GetUnitPrice(sci, product)).unitprice);
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
                var subtotal = await _pricingService.GetSubTotal(sci, product);
                var shoppingCartItemDiscountBase = subtotal.discountAmount;
                var productprices = await _taxService.GetProductPrice(product, subtotal.subTotal);
                cartItemModel.SubTotal = _priceFormatter.FormatPrice(productprices.productprice);

                //display an applied discount amount
                if (shoppingCartItemDiscountBase > 0)
                {
                    shoppingCartItemDiscountBase =
                        (await _taxService.GetProductPrice(product, shoppingCartItemDiscountBase)).productprice;
                    if (shoppingCartItemDiscountBase > 0)
                        cartItemModel.Discount = _priceFormatter.FormatPrice(shoppingCartItemDiscountBase);
                }
            }

            //picture
            if (_shoppingCartSettings.ShowProductImagesOnWishList)
                cartItemModel.Picture = await PrepareCartItemPicture(request, product, sci.Attributes);

            //item warnings
            var itemWarnings = await _shoppingCartValidator.GetShoppingCartItemWarnings(request.Customer, sci, product,
                new ShoppingCartValidatorOptions());
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
        var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService);
        return new PictureModel {
            Id = sciPicture?.Id,
            ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.CartThumbPictureSize),
            Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), product.Name),
            AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"),
                product.Name)
        };
    }
}