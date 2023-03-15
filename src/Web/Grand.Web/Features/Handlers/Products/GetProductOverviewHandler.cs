﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductOverviewHandler : IRequestHandler<GetProductOverview, IEnumerable<ProductOverviewModel>>
    {
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly IPricingService _pricingService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPictureService _pictureService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMediator _mediator;
        private readonly LinkGenerator _linkGenerator;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetProductOverviewHandler(
            IPermissionService permissionService,
            IWorkContext workContext,
            ITranslationService translationService,
            IProductService productService,
            IBrandService brandService,
            IPricingService priceCalculationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IPictureService pictureService,
            IDateTimeService dateTimeService,
            IMediator mediator,
            LinkGenerator linkGenerator,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings)
        {
            _permissionService = permissionService;
            _workContext = workContext;
            _translationService = translationService;
            _productService = productService;
            _brandService = brandService;
            _pricingService = priceCalculationService;
            _taxService = taxService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _pictureService = pictureService;
            _dateTimeService = dateTimeService;
            _mediator = mediator;
            _linkGenerator = linkGenerator;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
        }

        public async Task<IEnumerable<ProductOverviewModel>> Handle(GetProductOverview request,
            CancellationToken cancellationToken)
        {
            if (request.Products == null)
                throw new ArgumentNullException(nameof(request.Products));

            var displayPrices =
                await _permissionService.Authorize(StandardPermission.DisplayPrices, _workContext.CurrentCustomer);
            var enableShoppingCart =
                await _permissionService.Authorize(StandardPermission.EnableShoppingCart, _workContext.CurrentCustomer);
            var enableWishlist =
                await _permissionService.Authorize(StandardPermission.EnableWishlist, _workContext.CurrentCustomer);
            var pictureSize = request.ProductThumbPictureSize ?? _mediaSettings.ProductThumbPictureSize;
            var priceIncludesTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var res = new Dictionary<string, string> {
                {
                    "Products.CallForPrice",
                    _translationService.GetResource("Products.CallForPrice", _workContext.WorkingLanguage.Id)
                }, {
                    "Products.PriceRangeFrom",
                    _translationService.GetResource("Products.PriceRangeFrom", _workContext.WorkingLanguage.Id)
                }, {
                    "Media.Product.ImageLinkTitleFormat",
                    _translationService.GetResource("Media.Product.ImageLinkTitleFormat",
                        _workContext.WorkingLanguage.Id)
                }, {
                    "Media.Product.ImageAlternateTextFormat",
                    _translationService.GetResource("Media.Product.ImageAlternateTextFormat",
                        _workContext.WorkingLanguage.Id)
                }
            };

            var tasks = new List<Task<ProductOverviewModel>>();

            foreach (var product in request.Products)
            {
                tasks.Add(GetProductOverviewModel(product, request, displayPrices, enableShoppingCart, enableWishlist,
                    pictureSize, priceIncludesTax, res));
            }

            var result = await Task.WhenAll(tasks);
            return result;
        }


        private async Task<ProductOverviewModel> GetProductOverviewModel(Product product, GetProductOverview request,
            bool displayPrices, bool enableShoppingCart, bool enableWishlist, int pictureSize, bool priceIncludesTax,
            Dictionary<string, string> res)
        {
            var model = await PrepareProductOverviewModel(product);
            //price
            if (request.PreparePriceModel)
            {
                model.ProductPrice = await PreparePriceModel(product, res, request.ForceRedirectionAfterAddingToCart,
                    enableShoppingCart, displayPrices, enableWishlist, priceIncludesTax);
            }

            //picture
            if (request.PreparePictureModel)
            {
                var pictureModels = await PreparePictureModel(product, model.Name, res, pictureSize);
                model.DefaultPictureModel = pictureModels.FirstOrDefault();
                if (pictureModels.Count > 1) model.SecondPictureModel = pictureModels.ElementAtOrDefault(1);
            }

            //specs
            if (request.PrepareSpecificationAttributes && product.ProductSpecificationAttributes.Any())
            {
                model.SpecificationAttributeModels = await _mediator.Send(new GetProductSpecification
                    { Language = _workContext.WorkingLanguage, Product = product });
            }

            //attributes
            model.ProductAttributeModels = await PrepareAttributesModel(product);

            //reviews
            model.ReviewOverviewModel = await _mediator.Send(new GetProductReviewOverview
                { Product = product, Language = _workContext.WorkingLanguage, Store = _workContext.CurrentStore });

            return model;
        }

        private async Task<ProductOverviewModel> PrepareProductOverviewModel(Product product)
        {
            var sename = product.GetSeName(_workContext.WorkingLanguage.Id);
            var model = new ProductOverviewModel {
                Id = product.Id,
                Name = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                ShortDescription = product.GetTranslation(x => x.ShortDescription, _workContext.WorkingLanguage.Id),
                FullDescription = product.GetTranslation(x => x.FullDescription, _workContext.WorkingLanguage.Id),
                SeName = sename,
                Url = _linkGenerator.GetPathByRouteValues("Product", new { SeName = sename }),
                ProductType = product.ProductTypeId,
                Sku = product.Sku,
                Gtin = product.Gtin,
                Flag = product.Flag,
                Mpn = product.Mpn,
                BrandName = string.IsNullOrEmpty(product.BrandId)
                    ? ""
                    : (await _brandService.GetBrandById(product.BrandId))?.GetTranslation(x => x.Name,
                        _workContext.WorkingLanguage.Id),
                IsFreeShipping = product.IsFreeShipping,
                LowStock = product.LowStock,
                ShowSku = _catalogSettings.ShowSkuOnCatalogPages,
                TaxDisplayType = _workContext.TaxDisplayType,
                EndTime = product.AvailableEndDateTimeUtc,
                EndTimeLocalTime = product.AvailableEndDateTimeUtc.HasValue
                    ? _dateTimeService.ConvertToUserTime(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc)
                    : new DateTime?(),
                ShowQty = _catalogSettings.DisplayQuantityOnCatalogPages,
                UserFields = product.UserFields,
                MarkAsNew = product.MarkAsNew &&
                            (!product.MarkAsNewStartDateTimeUtc.HasValue ||
                             product.MarkAsNewStartDateTimeUtc.Value < DateTime.UtcNow) &&
                            (!product.MarkAsNewEndDateTimeUtc.HasValue ||
                             product.MarkAsNewEndDateTimeUtc.Value > DateTime.UtcNow)
            };
            return model;
        }

        private async Task<ProductOverviewModel.ProductPriceModel> PreparePriceModel(Product product,
            IReadOnlyDictionary<string, string> res,
            bool forceRedirectionAfterAddingToCart, bool enableShoppingCart, bool displayPrices, bool enableWishlist,
            bool priceIncludesTax)
        {
            #region Prepare product price

            var priceModel = new ProductOverviewModel.ProductPriceModel {
                ForceRedirectionAfterAddingToCart = forceRedirectionAfterAddingToCart
            };

            switch (product.ProductTypeId)
            {
                case ProductType.GroupedProduct:
                {
                    #region Grouped product

                    var associatedProducts =
                        await _productService.GetAssociatedProducts(product.Id, _workContext.CurrentStore.Id);

                    //add to cart button (ignore "DisableBuyButton" property for grouped products)
                    priceModel.DisableBuyButton = !enableShoppingCart || !displayPrices;

                    //add to wishlist button (ignore "DisableWishlistButton" property for grouped products)
                    priceModel.DisableWishlistButton = !enableWishlist || !displayPrices;

                    //compare products
                    priceModel.DisableAddToCompareListButton = !_catalogSettings.CompareProductsEnabled;

                    //catalog price, not used in views, but it's for front developer
                    if (product.CatalogPrice > 0)
                    {
                        var catalogPrice =
                            await _currencyService.ConvertFromPrimaryStoreCurrency(product.CatalogPrice,
                                _workContext.WorkingCurrency);
                        priceModel.CatalogPrice = _priceFormatter.FormatPrice(catalogPrice,
                            _workContext.WorkingCurrency, _workContext.WorkingLanguage, priceIncludesTax);
                    }

                    switch (associatedProducts.Count)
                    {
                        case 0:
                        {
                        }
                            break;
                        default:
                        {
                            //we have at least one associated product
                            //compare products
                            priceModel.DisableAddToCompareListButton = !_catalogSettings.CompareProductsEnabled;
                            if (displayPrices)
                            {
                                //find a minimum possible price
                                double? minPossiblePrice = null;
                                Product minPriceProduct = null;
                                foreach (var associatedProduct in associatedProducts)
                                {
                                    //calculate for the maximum quantity (in case if we have tier prices)
                                    var tmpPrice = (await _pricingService.GetFinalPrice(associatedProduct,
                                        _workContext.CurrentCustomer, _workContext.WorkingCurrency, 0, true,
                                        int.MaxValue)).finalPrice;
                                    if (minPossiblePrice.HasValue && !(tmpPrice < minPossiblePrice.Value)) continue;
                                    minPriceProduct = associatedProduct;
                                    minPossiblePrice = tmpPrice;
                                }

                                if (minPriceProduct is { EnteredPrice: false })
                                {
                                    if (minPriceProduct.CallForPrice)
                                    {
                                        priceModel.OldPrice = null;
                                        priceModel.Price = res["Products.CallForPrice"];
                                    }
                                    else if (minPossiblePrice.HasValue)
                                    {
                                        //calculate prices
                                        var finalPrice = (await _taxService.GetProductPrice(minPriceProduct,
                                                minPossiblePrice.Value, priceIncludesTax, _workContext.CurrentCustomer))
                                            .productprice;

                                        priceModel.OldPrice = null;
                                        priceModel.Price = string.Format(res["Products.PriceRangeFrom"],
                                            _priceFormatter.FormatPrice(finalPrice, _workContext.WorkingCurrency,
                                                _workContext.WorkingLanguage, priceIncludesTax));
                                        priceModel.PriceValue = finalPrice;

                                        //PAngV base price (used in Germany)
                                        if (product.BasepriceEnabled)
                                            priceModel.BasePricePAngV = await _mediator.Send(new GetFormatBasePrice {
                                                Currency = _workContext.WorkingCurrency, Product = product,
                                                ProductPrice = finalPrice
                                            });
                                    }
                                    else
                                    {
                                        //Actually it's not possible (we presume that minimalPrice always has a value)
                                        //We never should get here
                                        Debug.WriteLine("Cannot calculate minPrice for product #{0}", product.Id);
                                    }
                                }
                            }
                            else
                            {
                                //hide prices
                                priceModel.OldPrice = null;
                                priceModel.Price = null;
                            }
                        }
                            break;
                    }

                    #endregion
                }
                    break;
                case ProductType.SimpleProduct:
                case ProductType.Reservation:
                default:
                {
                    #region Simple product

                    //add to cart button
                    priceModel.DisableBuyButton = product.DisableBuyButton || !enableShoppingCart || !displayPrices;

                    //add to wishlist button
                    priceModel.DisableWishlistButton =
                        product.DisableWishlistButton || !enableWishlist || !displayPrices;
                    //compare products
                    priceModel.DisableAddToCompareListButton = !_catalogSettings.CompareProductsEnabled;

                    //pre-order
                    if (product.AvailableForPreOrder)
                    {
                        priceModel.AvailableForPreOrder = !product.PreOrderDateTimeUtc.HasValue ||
                                                          product.PreOrderDateTimeUtc.Value >= DateTime.UtcNow;
                        priceModel.PreOrderDateTimeUtc = product.PreOrderDateTimeUtc;
                    }

                    //catalog price, not used in views, but it's for front developer
                    if (product.CatalogPrice > 0)
                    {
                        var catalogPrice =
                            await _currencyService.ConvertFromPrimaryStoreCurrency(product.CatalogPrice,
                                _workContext.WorkingCurrency);
                        priceModel.CatalogPrice = _priceFormatter.FormatPrice(catalogPrice,
                            _workContext.WorkingCurrency, _workContext.WorkingLanguage, priceIncludesTax);
                    }

                    //start price for product auction
                    if (product.StartPrice > 0)
                    {
                        var startPrice =
                            await _currencyService.ConvertFromPrimaryStoreCurrency(product.StartPrice,
                                _workContext.WorkingCurrency);
                        priceModel.StartPrice = _priceFormatter.FormatPrice(startPrice, _workContext.WorkingCurrency,
                            _workContext.WorkingLanguage, priceIncludesTax);
                        priceModel.StartPriceValue = startPrice;
                    }

                    //highest bid for product auction
                    if (product.HighestBid > 0)
                    {
                        var highestBid =
                            await _currencyService.ConvertFromPrimaryStoreCurrency(product.HighestBid,
                                _workContext.WorkingCurrency);
                        priceModel.HighestBid = _priceFormatter.FormatPrice(highestBid, _workContext.WorkingCurrency,
                            _workContext.WorkingLanguage, priceIncludesTax);
                        priceModel.HighestBidValue = highestBid;
                    }

                    //prices
                    if (displayPrices)
                    {
                        if (!product.EnteredPrice)
                        {
                            if (product.CallForPrice)
                            {
                                //call for price
                                priceModel.OldPrice = null;
                                priceModel.Price = res["Products.CallForPrice"];
                            }
                            else
                            {
                                //prices

                                //calculate for the maximum quantity (in case if we have tier prices)
                                var infoprice = await _pricingService.GetFinalPrice(product,
                                    _workContext.CurrentCustomer, _workContext.WorkingCurrency, 0, true, int.MaxValue);

                                priceModel.AppliedDiscounts = infoprice.appliedDiscounts;
                                priceModel.PreferredTierPrice = infoprice.preferredTierPrice;

                                var minPossiblePrice = infoprice.finalPrice;

                                var oldPriceBase = (await _taxService.GetProductPrice(product, product.OldPrice,
                                    priceIncludesTax, _workContext.CurrentCustomer)).productprice;
                                var finalPrice = (await _taxService.GetProductPrice(product, minPossiblePrice,
                                    priceIncludesTax, _workContext.CurrentCustomer)).productprice;

                                var oldPrice =
                                    await _currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase,
                                        _workContext.WorkingCurrency);

                                //do we have tier prices configured?
                                var tierPrices = new List<TierPrice>();
                                if (product.TierPrices.Any())
                                {
                                    tierPrices.AddRange(product.TierPrices.OrderBy(tp => tp.Quantity)
                                        .FilterByStore(_workContext.CurrentStore.Id)
                                        .FilterByCurrency(_workContext.WorkingCurrency.CurrencyCode)
                                        .FilterForCustomer(_workContext.CurrentCustomer)
                                        .FilterByDate()
                                        .RemoveDuplicatedQuantities());
                                }

                                //When there is just one tier (with  qty 1), 
                                //there are no actual savings in the list.
                                var displayFromMessage = tierPrices.Any() &&
                                                         !(tierPrices.Count == 1 && tierPrices[0].Quantity <= 1);
                                if (displayFromMessage)
                                {
                                    priceModel.OldPrice = null;
                                    priceModel.Price = string.Format(res["Products.PriceRangeFrom"],
                                        _priceFormatter.FormatPrice(finalPrice, _workContext.WorkingCurrency,
                                            _workContext.WorkingLanguage, priceIncludesTax));
                                    priceModel.PriceValue = finalPrice;
                                }
                                else
                                {
                                    if (finalPrice != oldPriceBase && oldPriceBase != 0)
                                    {
                                        priceModel.OldPrice = _priceFormatter.FormatPrice(oldPrice,
                                            _workContext.WorkingCurrency, _workContext.WorkingLanguage,
                                            priceIncludesTax);
                                        priceModel.OldPriceValue = oldPrice;
                                        priceModel.Price = _priceFormatter.FormatPrice(finalPrice,
                                            _workContext.WorkingCurrency, _workContext.WorkingLanguage,
                                            priceIncludesTax);
                                        priceModel.PriceValue = finalPrice;
                                    }
                                    else
                                    {
                                        priceModel.OldPrice = null;
                                        priceModel.Price = _priceFormatter.FormatPrice(finalPrice,
                                            _workContext.WorkingCurrency, _workContext.WorkingLanguage,
                                            priceIncludesTax);
                                        priceModel.PriceValue = finalPrice;
                                    }
                                }

                                if (product.ProductTypeId == ProductType.Reservation)
                                {
                                    //rental product
                                    priceModel.OldPrice =
                                        _priceFormatter.FormatReservationProductPeriod(product, priceModel.OldPrice);
                                    priceModel.Price =
                                        _priceFormatter.FormatReservationProductPeriod(product, priceModel.Price);
                                }

                                //PAngV base price (used in Germany)
                                if (product.BasepriceEnabled)
                                    priceModel.BasePricePAngV = await _mediator.Send(new GetFormatBasePrice {
                                        Currency = _workContext.WorkingCurrency, Product = product,
                                        ProductPrice = finalPrice
                                    });
                            }
                        }
                    }
                    else
                    {
                        //hide prices
                        priceModel.OldPrice = null;
                        priceModel.Price = null;
                    }

                    #endregion
                }
                    break;
            }

            return priceModel;

            #endregion
        }

        private async Task<IList<PictureModel>> PreparePictureModel(Product product, string name,
            IReadOnlyDictionary<string, string> res, int pictureSize)
        {
            #region Prepare product picture

            var result = new List<PictureModel>();

            async Task<PictureModel> PreparePictureModel(ProductPicture productpicture)
            {
                var picture =
                    productpicture != null ? await _pictureService.GetPictureById(productpicture.PictureId) : null;

                productpicture ??= new ProductPicture();

                var pictureModel = new PictureModel {
                    Id = productpicture.PictureId,
                    ImageUrl = await _pictureService.GetPictureUrl(productpicture.PictureId, pictureSize),
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(productpicture.PictureId),
                    Style = picture?.Style,
                    ExtraField = picture?.ExtraField,
                    //"title" attribute
                    Title =
                        picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute,
                            _workContext.WorkingLanguage.Id))
                            ? picture.GetTranslation(x => x.TitleAttribute, _workContext.WorkingLanguage.Id)
                            : string.Format(res["Media.Product.ImageLinkTitleFormat"], name),
                    //"alt" attribute
                    AlternateText =
                        picture != null &&
                        !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute,
                            _workContext.WorkingLanguage.Id))
                            ? picture.GetTranslation(x => x.AltAttribute, _workContext.WorkingLanguage.Id)
                            : string.Format(res["Media.Product.ImageAlternateTextFormat"], name)
                };

                return pictureModel;
            }

            //prepare picture model
            result.Add(await PreparePictureModel(product.ProductPictures.MinBy(x => x.DisplayOrder)));

            //prepare second picture model
            if (!_catalogSettings.SecondPictureOnCatalogPages) return result;

            var secondPicture = product.ProductPictures.OrderBy(x => x.DisplayOrder).Skip(1).Take(1)
                .FirstOrDefault();
            if (secondPicture != null)
                result.Add(await PreparePictureModel(secondPicture));
            
            return result;

            #endregion
        }

        private async Task<IList<ProductOverviewModel.ProductAttributeModel>> PrepareAttributesModel(Product product)
        {
            var result = new List<ProductOverviewModel.ProductAttributeModel>();
            if (!product.ProductAttributeMappings.Any(x => x.ShowOnCatalogPage)) return result;


            foreach (var attribute in product.ProductAttributeMappings.Where(x => x.ShowOnCatalogPage)
                         .OrderBy(x => x.DisplayOrder))
            {
                var pa = await _mediator.Send(new GetProductAttribute { Id = attribute.ProductAttributeId });
                if (pa == null) continue;

                var productAttributeModel = new ProductOverviewModel.ProductAttributeModel {
                    Name = pa.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    SeName = pa.SeName,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId,
                    UserFields = pa.UserFields
                };
                foreach (var item in attribute.ProductAttributeValues)
                {
                    var value = new ProductOverviewModel.ProductAttributeValueModel {
                        Name = item.Name,
                        ColorSquaresRgb = item.ColorSquaresRgb
                    };
                    //"image square" picture (with with "image squares" attribute type only)
                    if (!string.IsNullOrEmpty(item.ImageSquaresPictureId))
                    {
                        var pm = new PictureModel();
                        pm = new PictureModel {
                            Id = item.ImageSquaresPictureId,
                            FullSizeImageUrl = await _pictureService.GetPictureUrl(item.ImageSquaresPictureId),
                            ImageUrl = await _pictureService.GetPictureUrl(item.ImageSquaresPictureId,
                                _mediaSettings.ImageSquarePictureSize)
                        };
                        value.ImageSquaresPictureModel = pm;
                    }

                    //picture of a product attribute value
                    if (!string.IsNullOrEmpty(item.PictureId))
                    {
                        var pm = new PictureModel();
                        pm = new PictureModel {
                            Id = item.PictureId,
                            FullSizeImageUrl = await _pictureService.GetPictureUrl(item.PictureId),
                            ImageUrl = await _pictureService.GetPictureUrl(item.PictureId, 50)
                        };
                        value.PictureModel = pm;
                    }

                    productAttributeModel.Values.Add(value);
                }

                result.Add(productAttributeModel);
            }

            return result;
        }
    }
}