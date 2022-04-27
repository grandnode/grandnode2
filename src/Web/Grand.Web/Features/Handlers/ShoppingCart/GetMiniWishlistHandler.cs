using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Media;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetMiniWishlistHandler : IRequestHandler<GetMiniWishlist, MiniWishlistModel>
    {
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ITranslationService _translationService;
        private readonly ITaxService _taxService;
        private readonly IPricingService _pricingService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPictureService _pictureService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MediaSettings _mediaSettings;

        public GetMiniWishlistHandler(
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter,
            ITranslationService translationService,
            ITaxService taxService,
            IPricingService priceCalculationService,
            IPriceFormatter priceFormatter,
            IPictureService pictureService,
            IHttpContextAccessor httpContextAccessor,
            LinkGenerator linkGenerator,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings)
        {
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _productAttributeFormatter = productAttributeFormatter;
            _translationService = translationService;
            _taxService = taxService;
            _pricingService = priceCalculationService;
            _priceFormatter = priceFormatter;
            _pictureService = pictureService;
            _httpContextAccessor = httpContextAccessor;
            _linkGenerator = linkGenerator;
            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<MiniWishlistModel> Handle(GetMiniWishlist request, CancellationToken cancellationToken)
        {
            var model = new MiniWishlistModel {
                EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
                ShowProductImages = _shoppingCartSettings.ShowProductImagesOnWishList,
                TotalProducts = request.Cart.Count
            };

            if (!request.Cart.Any())
                return model;

            #region Cart items

            foreach (var sci in request.Cart)
            {
                var product = await _productService.GetProductById(sci.ProductId);
                if (product == null)
                    continue;

                var sename = product.GetSeName(request.Language.Id);
                var cartItemModel = new MiniWishlistModel.WishlistItemModel {
                    Id = sci.Id,
                    Sku = product.FormatSku(sci.Attributes, _productAttributeParser),
                    ProductId = product.Id,
                    ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                    ProductSeName = sename,
                    ProductUrl = _linkGenerator.GetUriByRouteValues(_httpContextAccessor.HttpContext, "Product", new { SeName = sename }),
                    Quantity = sci.Quantity,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes),
                };

                //unit prices
                if (product.CallForPrice)
                {
                    cartItemModel.UnitPrice = _translationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    var productprice = await _taxService.GetProductPrice(product, (await _pricingService.GetUnitPrice(sci, product)).unitprice);

                    cartItemModel.UnitPrice = _priceFormatter.FormatPrice(productprice.productprice);
                    cartItemModel.UnitPriceValue = productprice.productprice;
                    cartItemModel.TaxRate = productprice.taxRate;
                }

                //picture
                if (_shoppingCartSettings.ShowProductImagesOnWishList)
                {
                    cartItemModel.Picture = await PrepareCartItemPicture(product, sci.Attributes);
                }


                model.Items.Add(cartItemModel);
            }

            #endregion

            return model;
        }

        private async Task<PictureModel> PrepareCartItemPicture(
            Product product, IList<CustomAttribute> attributes)
        {
            var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService, _productAttributeParser);
            return new PictureModel {
                Id = sciPicture?.Id,
                ImageUrl = await _pictureService.GetPictureUrl(sciPicture, _mediaSettings.MiniCartThumbPictureSize, true),
                Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), product.Name),
                AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"), product.Name),
            };
        }

    }
}
