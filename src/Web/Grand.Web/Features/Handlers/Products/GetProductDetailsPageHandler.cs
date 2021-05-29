using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductDetailsPageHandler : IRequestHandler<GetProductDetailsPage, ProductDetailsModel>
    {
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly ITranslationService _translationService;
        private readonly IProductService _productService;
        private readonly IPricingService _pricingService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IMeasureService _measureService;
        private readonly ICacheBase _cacheBase;
        private readonly IPictureService _pictureService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly IWarehouseService _warehouseService;
        private readonly IDeliveryDateService _deliveryDateService;
        private readonly IBrandService _brandService;
        private readonly IVendorService _vendorService;
        private readonly ICategoryService _categoryService;
        private readonly IProductTagService _productTagService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICollectionService _collectionService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IDownloadService _downloadService;
        private readonly IProductReservationService _productReservationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMediator _mediator;

        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly SeoSettings _seoSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public GetProductDetailsPageHandler(
            IPermissionService permissionService,
            IWorkContext workContext,
            ITranslationService translationService,
            IProductService productService,
            IPricingService priceCalculationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IMeasureService measureService,
            ICacheBase cacheBase,
            IPictureService pictureService,
            IProductAttributeParser productAttributeParser,
            IStockQuantityService stockQuantityService,
            IWarehouseService warehouseService,
            IDeliveryDateService deliveryDateService,
            IBrandService brandService,
            IVendorService vendorService,
            ICategoryService categoryService,
            IProductTagService productTagService,
            IProductAttributeService productAttributeService,
            ICollectionService collectionService,
            IDateTimeService dateTimeService,
            IDownloadService downloadService,
            IProductReservationService productReservationService,
            IHttpContextAccessor httpContextAccessor,
            IMediator mediator,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            SeoSettings seoSettings,
            VendorSettings vendorSettings,
            CaptchaSettings captchaSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _permissionService = permissionService;
            _workContext = workContext;
            _translationService = translationService;
            _productService = productService;
            _pricingService = priceCalculationService;
            _taxService = taxService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _measureService = measureService;
            _cacheBase = cacheBase;
            _pictureService = pictureService;
            _productAttributeParser = productAttributeParser;
            _stockQuantityService = stockQuantityService;
            _warehouseService = warehouseService;
            _deliveryDateService = deliveryDateService;
            _brandService = brandService;
            _vendorService = vendorService;
            _categoryService = categoryService;
            _productTagService = productTagService;
            _productAttributeService = productAttributeService;
            _collectionService = collectionService;
            _dateTimeService = dateTimeService;
            _downloadService = downloadService;
            _productReservationService = productReservationService;
            _httpContextAccessor = httpContextAccessor;
            _mediator = mediator;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
            _seoSettings = seoSettings;
            _vendorSettings = vendorSettings;
            _captchaSettings = captchaSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<ProductDetailsModel> Handle(GetProductDetailsPage request, CancellationToken cancellationToken)
        {
            return await PrepareProductDetailsModel(request.Store, request.Product, request.UpdateCartItem, request.IsAssociatedProduct);
        }

        private async Task<ProductDetailsModel> PrepareProductDetailsModel(Store store, Product product, ShoppingCartItem updateCartItem, bool isAssociatedProduct)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var model = await PrepareStandardProperties(product, updateCartItem);

            #region Brand details

            model.BrandModel = await PrepareBrandBriefInfoModel(product);

            #endregion

            #region Vendor details

            if (_vendorSettings.ShowVendorOnProductDetailsPage)
            {
                model.VendorModel = await PrepareVendorBriefInfoModel(product);
                if (model.VendorModel != null)
                    model.ShowVendor = true;
            }

            #endregion

            #region Page sharing

            if (_catalogSettings.ShowShareButton && !string.IsNullOrEmpty(_catalogSettings.PageShareCode))
            {
                var shareCode = _catalogSettings.PageShareCode;
                if (store.SslEnabled)
                {
                    //need to change the addthis link to be https linked when the page is, so that the page doesnt ask about mixed mode when viewed in https...
                    shareCode = shareCode.Replace("http://", "https://");
                }
                model.PageShareCode = shareCode;
            }

            #endregion

            #region Out of stock subscriptions

            if ((product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock
                || product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes) &&
                product.BackorderModeId == BackorderMode.NoBackorders &&
                product.AllowOutOfStockSubscriptions &&
                _stockQuantityService.GetTotalStockQuantity(product, warehouseId: _workContext.CurrentStore.DefaultWarehouseId) <= 0)
            {
                //out of stock
                model.DisplayOutOfStockSubscription = true;
            }

            #endregion

            #region Breadcrumb

            //do not prepare this model for the associated products. anyway it's not used
            if (_catalogSettings.CategoryBreadcrumbEnabled && !isAssociatedProduct)
            {
                model.Breadcrumb = await PrepareProductBreadcrumbModel(product);
            }

            #endregion

            #region Product tags

            //do not prepare this model for the associated products. anyway it's not used
            if (!isAssociatedProduct)
            {
                model.ProductTags = await PrepareProductTagModel(product);
            }

            #endregion

            #region Pictures

            model.DefaultPictureZoomEnabled = _mediaSettings.DefaultPictureZoomEnabled;
            //default picture
            var defaultPictureSize = isAssociatedProduct ?
                _mediaSettings.AssociatedProductPictureSize :
                _mediaSettings.ProductDetailsPictureSize;
            //prepare picture models
            var cachedPictures = await PrepareProductPictureModel(product, defaultPictureSize, isAssociatedProduct, model.Name);
            model.DefaultPictureModel = cachedPictures.defaultPictureModel;
            model.PictureModels = cachedPictures.pictureModels;

            #endregion

            #region Product price

            model.ProductPrice = await PrepareProductPriceModel(product);

            #endregion

            #region 'Add to cart' model

            model.AddToCart = await PrepareAddToCartModel(product, updateCartItem);

            #endregion

            #region Gift voucher

            model.GiftVoucher = PrepareGiftVoucherModel(product, updateCartItem);

            #endregion

            #region Product attributes

            model.ProductAttributes = await PrepareProductAttributeModel(product, defaultPictureSize, updateCartItem);

            #endregion 

            #region Product specifications

            model.ProductSpecifications = await _mediator.Send(new GetProductSpecification()
            {
                Language = _workContext.WorkingLanguage,
                Product = product
            });

            #endregion

            #region Product review overview

            model.ProductReviewOverview = await _mediator.Send(new GetProductReviewOverview()
            {
                Product = product,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            #endregion

            #region Tier prices

            if (product.TierPrices.Any() && await _permissionService.Authorize(StandardPermission.DisplayPrices))
            {
                model.TierPrices = await PrepareProductTierPriceModel(product);
            }

            #endregion

            #region Collections

            string collectionsCacheKey = string.Format(CacheKeyConst.PRODUCT_COLLECTIONS_MODEL_KEY,
                product.Id,
                _workContext.WorkingLanguage.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
                model.ProductCollections = await _cacheBase.GetAsync(collectionsCacheKey, async () =>
                {
                    var listCollection = new List<CollectionModel>();
                    foreach (var item in product.ProductCollections.OrderBy(x=>x.DisplayOrder))
                    {
                        var collect = (await _collectionService.GetCollectionById(item.CollectionId)).ToModel(_workContext.WorkingLanguage);
                        listCollection.Add(collect);
                    }
                    return listCollection;
                });

            #endregion

            #region Associated products

            if (product.ProductTypeId == ProductType.GroupedProduct)
            {
                //ensure no circular references
                if (!isAssociatedProduct)
                {
                    var associatedProducts = await _productService.GetAssociatedProducts(product.Id, _workContext.CurrentStore.Id);
                    foreach (var associatedProduct in associatedProducts)
                        model.AssociatedProducts.Add(await PrepareProductDetailsModel(store, associatedProduct, null, true));
                }
            }

            #endregion

            #region Product reservations

            await PrepareProductReservation(model, product);

            #endregion Product reservations

            #region Product Bundle

            if (product.ProductTypeId == ProductType.BundledProduct)
            {
                model.ProductBundleModels = await PrepareProductBundleModel(product);
            }
            #endregion

            #region Auctions

            model.StartPrice = product.StartPrice;
            model.HighestBidValue = product.HighestBid;
            model.AddToCart.IsAuction = product.ProductTypeId == ProductType.Auction;
            model.EndTime = product.AvailableEndDateTimeUtc;
            model.EndTimeLocalTime = product.AvailableEndDateTimeUtc.HasValue ? _dateTimeService.ConvertToUserTime(product.AvailableEndDateTimeUtc.Value, DateTimeKind.Utc) : new DateTime?();

            model.AuctionEnded = product.AuctionEnded;

            #endregion

            return model;
        }


        private async Task<ProductDetailsModel> PrepareStandardProperties(Product product, ShoppingCartItem updateCartItem)
        {
            #region Standard properties

            var warehouseId = updateCartItem != null ? updateCartItem.WarehouseId : _workContext.CurrentStore.DefaultWarehouseId;

            var model = new ProductDetailsModel
            {
                Id = product.Id,
                ProductType = product.ProductTypeId,
                Name = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                ShortDescription = product.GetTranslation(x => x.ShortDescription, _workContext.WorkingLanguage.Id),
                FullDescription = product.GetTranslation(x => x.FullDescription, _workContext.WorkingLanguage.Id),
                Flag = product.Flag,
                MetaKeywords = product.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id),
                MetaDescription = product.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id),
                MetaTitle = product.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id),
                SeName = product.GetSeName(_workContext.WorkingLanguage.Id),
                ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage,
                Sku = product.Sku,
                ShowMpn = _catalogSettings.ShowMpn,
                FreeShippingNotificationEnabled = _catalogSettings.ShowFreeShippingNotification,
                Mpn = product.Mpn,
                ShowGtin = _catalogSettings.ShowGtin,
                Gtin = product.Gtin,
                StockAvailability = _stockQuantityService.FormatStockMessage(product, warehouseId, null),
                UserFields = product.UserFields,
                HasSampleDownload = product.IsDownload && product.HasSampleDownload,
                DisplayDiscontinuedMessage =
                    (!product.Published && _catalogSettings.DisplayDiscontinuedMessageForUnpublishedProducts) ||
                    (product.ProductTypeId == ProductType.Auction && product.AuctionEnded) ||
                    (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow),
                CompareProductsEnabled = _catalogSettings.CompareProductsEnabled,
                AllowToSelectWarehouse = _shoppingCartSettings.AllowToSelectWarehouse,
                IsShipEnabled = product.IsShipEnabled,
                AdditionalShippingCharge = product.AdditionalShippingCharge,
                NotReturnable = product.NotReturnable,
                EmailAFriendEnabled = _catalogSettings.EmailAFriendEnabled,
                AskQuestionEnabled = _catalogSettings.AskQuestionEnabled,
                AskQuestionOnProduct = _catalogSettings.AskQuestionOnProduct
            };

            //automatically generate product description?
            if (_seoSettings.GenerateProductMetaDescription && String.IsNullOrEmpty(model.MetaDescription))
            {
                //based on short description
                model.MetaDescription = model.ShortDescription;
            }
            //warehouse
            if (model.AllowToSelectWarehouse)
            {
                foreach (var warehouse in await _warehouseService.GetAllWarehouses())
                {
                    var productwarehouse = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
                    model.ProductWarehouses.Add(new ProductDetailsModel.ProductWarehouseModel
                    {
                        Use = productwarehouse != null,
                        StockQuantity = productwarehouse?.StockQuantity ?? 0,
                        ReservedQuantity = productwarehouse?.ReservedQuantity ?? 0,
                        WarehouseId = warehouse.Id,
                        Name = warehouse.Name,
                        Selected = updateCartItem != null && updateCartItem?.WarehouseId == warehouse.Id
                    });
                }
            }
            //shipping info
            if (product.IsShipEnabled)
            {
                model.IsFreeShipping = product.IsFreeShipping;
                //delivery date
                if (!string.IsNullOrEmpty(product.DeliveryDateId))
                {
                    var deliveryDate = await _deliveryDateService.GetDeliveryDateById(product.DeliveryDateId);
                    if (deliveryDate != null)
                    {
                        model.DeliveryDate = deliveryDate.GetTranslation(dd => dd.Name, _workContext.WorkingLanguage.Id);
                        model.DeliveryColorSquaresRgb = deliveryDate.ColorSquaresRgb;
                    }
                }
            }
            //additional shipping charge
            if (model.AdditionalShippingCharge > 0)
                model.AdditionalShippingChargeStr = _priceFormatter.FormatPrice((await _taxService.GetShippingPrice(model.AdditionalShippingCharge, _workContext.CurrentCustomer)).shippingPrice);

            //ask question us on the product
            if (model.AskQuestionOnProduct)
                model.ProductAskQuestion = await PrepareProductAskQuestionSimpleModel(product);

            //store name
            model.CurrentStoreName = _workContext.CurrentStore.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);
            
            return model;

            #endregion
        }

        private async Task<ProductAskQuestionSimpleModel> PrepareProductAskQuestionSimpleModel(Product product)
        {
            var customer = _workContext.CurrentCustomer;

            var model = new ProductAskQuestionSimpleModel
            {
                Id = product.Id,
                AskQuestionEmail = customer.Email,
                AskQuestionFullName = customer.GetFullName(),
                AskQuestionPhone = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone),
                AskQuestionMessage = "",
                DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnAskQuestionPage
            };
            return await Task.FromResult(model);
        }
        private async Task<BrandBriefInfoModel> PrepareBrandBriefInfoModel(Product product)
        {
            if (!string.IsNullOrEmpty(product.BrandId))
            {
                var brand = await _brandService.GetBrandById(product.BrandId);
                if (brand != null && brand.Published)
                {
                    return new BrandBriefInfoModel
                    {
                        Id = brand.Id,
                        Name = brand.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        SeName = brand.GetSeName(_workContext.WorkingLanguage.Id),
                    };
                }
            }
            return null;
        }
        private async Task<VendorBriefInfoModel> PrepareVendorBriefInfoModel(Product product)
        {
            if (!string.IsNullOrEmpty(product.VendorId))
            {
                var vendor = await _vendorService.GetVendorById(product.VendorId);
                if (vendor != null && !vendor.Deleted && vendor.Active)
                {
                    return new VendorBriefInfoModel
                    {
                        Id = vendor.Id,
                        Name = vendor.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        SeName = vendor.GetSeName(_workContext.WorkingLanguage.Id),
                    };
                }
            }
            return null;
        }

        private async Task<ProductDetailsModel.ProductBreadcrumbModel> PrepareProductBreadcrumbModel(Product product)
        {
            var breadcrumbCacheKey = string.Format(CacheKeyConst.PRODUCT_BREADCRUMB_MODEL_KEY,
                product.Id,
                _workContext.WorkingLanguage.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerGroupIds()),
                _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(breadcrumbCacheKey, async () =>
            {
                var breadcrumbModel = new ProductDetailsModel.ProductBreadcrumbModel
                {

                    Enabled = _catalogSettings.CategoryBreadcrumbEnabled,
                    ProductId = product.Id,
                    ProductName = product.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    ProductSeName = product.GetSeName(_workContext.WorkingLanguage.Id)
                };
                var productCategories = product.ProductCategories;
                if (productCategories.Any())
                {
                    var category = await _categoryService.GetCategoryById(productCategories.OrderBy(x => x.DisplayOrder).FirstOrDefault().CategoryId);
                    if (category != null)
                    {
                        foreach (var catBr in await _categoryService.GetCategoryBreadCrumb(category))
                        {
                            breadcrumbModel.CategoryBreadcrumb.Add(new CategorySimpleModel
                            {
                                Id = catBr.Id,
                                Name = catBr.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                                SeName = catBr.GetSeName(_workContext.WorkingLanguage.Id),
                                IncludeInMenu = catBr.IncludeInMenu
                            });
                        }
                    }
                }
                return breadcrumbModel;
            });
        }

        private async Task<IList<ProductTagModel>> PrepareProductTagModel(Product product)
        {
            var productTagsCacheKey = string.Format(CacheKeyConst.PRODUCTTAG_BY_PRODUCT_MODEL_KEY, product.Id, _workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(productTagsCacheKey, async () =>
            {
                var tags = new List<ProductTagModel>();
                foreach (var item in product.ProductTags)
                {
                    var tag = await _productTagService.GetProductTagByName(item);
                    if (tag != null)
                    {
                        tags.Add(new ProductTagModel()
                        {
                            Id = tag.Id,
                            Name = tag.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                            SeName = tag.SeName,
                            ProductCount = await _productTagService.GetProductCount(tag.Id, _workContext.CurrentStore.Id)
                        });
                    }
                }
                return tags;
            });
        }

        private async Task<(PictureModel defaultPictureModel, List<PictureModel> pictureModels)> PrepareProductPictureModel(Product product, int defaultPictureSize, bool isAssociatedProduct, string name)
        {
            var defaultPicture = product.ProductPictures.OrderBy(x => x.DisplayOrder).FirstOrDefault();
            if (defaultPicture == null)
                defaultPicture = new ProductPicture();

            var picture = await _pictureService.GetPictureById(defaultPicture.PictureId);
            
            var defaultPictureModel = new PictureModel
            {
                Id = defaultPicture.PictureId,
                ImageUrl = await _pictureService.GetPictureUrl(defaultPicture.PictureId, defaultPictureSize, !isAssociatedProduct),
                FullSizeImageUrl = await _pictureService.GetPictureUrl(defaultPicture.PictureId, 0, !isAssociatedProduct),
            };
            //"title" attribute
            defaultPictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute)) ?
                picture.TitleAttribute :
                string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat.Details"), name);
            //"alt" attribute
            defaultPictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute)) ?
                picture.AltAttribute :
                string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat.Details"), name);

            //all pictures
            var pictureModels = new List<PictureModel>();
            foreach (var productPicture in product.ProductPictures.OrderBy(x => x.DisplayOrder))
            {
                picture = await _pictureService.GetPictureById(productPicture.PictureId);
                if (picture != null)
                {
                    var pictureModel = new PictureModel
                    {
                        Id = productPicture.PictureId,
                        ThumbImageUrl = await _pictureService.GetPictureUrl(productPicture.PictureId, _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage),
                        ImageUrl = await _pictureService.GetPictureUrl(productPicture.PictureId, _mediaSettings.ProductDetailsPictureSize),
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(productPicture.PictureId),
                        Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat.Details"), name),
                        AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat.Details"), name),
                    };
                    //"title" attribute
                    pictureModel.Title = !string.IsNullOrEmpty(picture.TitleAttribute) ?
                        picture.TitleAttribute :
                        string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat.Details"), name);
                    //"alt" attribute
                    pictureModel.AlternateText = !string.IsNullOrEmpty(picture.AltAttribute) ?
                       picture.AltAttribute :
                       string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat.Details"), name);

                    pictureModels.Add(pictureModel);
                }
            }
            return (defaultPictureModel, pictureModels);

        }

        private async Task<ProductDetailsModel.ProductPriceModel> PrepareProductPriceModel(Product product)
        {
            var displayPrices = await _permissionService.Authorize(StandardPermission.DisplayPrices);
            var model = new ProductDetailsModel.ProductPriceModel();
            model.ProductId = product.Id;
            if (displayPrices)
            {
                model.HidePrices = false;
                if (product.EnteredPrice)
                {
                    model.EnteredPrice = true;
                }
                else
                {
                    if (product.CallForPrice)
                    {
                        model.CallForPrice = true;
                    }
                    else
                    {
                        var oldproductprice = await _taxService.GetProductPrice(product, product.OldPrice);
                        double oldPriceBase = oldproductprice.productprice;
                        double finalPriceWithoutDiscount = (await (_taxService.GetProductPrice(product, (await _pricingService.GetFinalPrice(product, _workContext.CurrentCustomer, _workContext.WorkingCurrency, includeDiscounts: false)).finalPrice))).productprice;

                        var appliedPrice = (await _pricingService.GetFinalPrice(product, _workContext.CurrentCustomer, _workContext.WorkingCurrency, includeDiscounts: true));
                        double finalPriceWithDiscount = (await _taxService.GetProductPrice(product, appliedPrice.finalPrice)).productprice;
                        double oldPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase, _workContext.WorkingCurrency);

                        if (finalPriceWithoutDiscount != oldPrice && oldPrice > 0)
                            model.OldPrice = _priceFormatter.FormatPrice(oldPrice);

                        model.Price = _priceFormatter.FormatPrice(finalPriceWithoutDiscount);
                        if (appliedPrice.appliedDiscounts.Any())
                            model.AppliedDiscounts = appliedPrice.appliedDiscounts;
                        if (appliedPrice.preferredTierPrice != null)
                            model.PreferredTierPrice = appliedPrice.preferredTierPrice;

                        if (product.CatalogPrice > 0)
                        {
                            var catalogPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.CatalogPrice, _workContext.WorkingCurrency);
                            model.CatalogPrice = _priceFormatter.FormatPrice(catalogPrice);
                        }

                        if (finalPriceWithoutDiscount != finalPriceWithDiscount)
                            model.PriceWithDiscount = _priceFormatter.FormatPrice(finalPriceWithDiscount);

                        model.PriceValue = finalPriceWithDiscount;

                        if (product.BasepriceEnabled)
                            model.BasePricePAngV = await _mediator.Send(new GetFormatBasePrice() { Currency = _workContext.WorkingCurrency, Product = product, ProductPrice = finalPriceWithDiscount });

                        //currency code
                        model.CurrencyCode = _workContext.WorkingCurrency.CurrencyCode;

                        //reservation
                        if (product.ProductTypeId == ProductType.Reservation)
                        {
                            model.IsReservation = true;
                            var priceStr = _priceFormatter.FormatPrice(finalPriceWithDiscount);
                            model.ReservationPrice = _priceFormatter.FormatReservationProductPeriod(product, priceStr);
                        }
                        //auction
                        if (product.ProductTypeId == ProductType.Auction)
                        {
                            model.IsAuction = true;
                            double highestBid = await _currencyService.ConvertFromPrimaryStoreCurrency(product.HighestBid, _workContext.WorkingCurrency);
                            model.HighestBid = _priceFormatter.FormatPrice(highestBid);
                            model.HighestBidValue = highestBid;
                            model.DisableBuyButton = product.DisableBuyButton;
                            double startPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.StartPrice, _workContext.WorkingCurrency);
                            model.StartPrice = _priceFormatter.FormatPrice(startPrice);
                            model.StartPriceValue = startPrice;
                        }
                    }
                }
            }
            else
            {
                model.HidePrices = true;
                model.OldPrice = null;
                model.CatalogPrice = null;
                model.Price = null;
            }
            return model;
        }

        private async Task<ProductDetailsModel.AddToCartModel> PrepareAddToCartModel(Product product, ShoppingCartItem updatecartitem = null)
        {
            var model = new ProductDetailsModel.AddToCartModel();
            model.ProductId = product.Id;
            if (updatecartitem != null)
            {
                model.UpdatedShoppingCartItemId = updatecartitem.Id;
                model.UpdateShoppingCartItemType = updatecartitem.ShoppingCartTypeId;
            }

            //quantity
            model.EnteredQuantity = updatecartitem != null ? updatecartitem.Quantity : product.OrderMinimumQuantity;
            model.MeasureUnit = !string.IsNullOrEmpty(product.UnitId) ? (await _measureService.GetMeasureUnitById(product.UnitId)).Name : string.Empty;

            //allowed quantities
            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
            {
                model.AllowedQuantities.Add(new SelectListItem
                {
                    Text = qty.ToString(),
                    Value = qty.ToString(),
                    Selected = updatecartitem != null && updatecartitem.Quantity == qty
                });
            }
            //minimum quantity notification
            if (product.OrderMinimumQuantity > 1)
            {
                model.MinimumQuantityNotification = string.Format(_translationService.GetResource("Products.MinimumQuantityNotification"), product.OrderMinimumQuantity);
            }
            //'add to cart', 'add to wishlist' buttons
            model.DisableBuyButton = product.DisableBuyButton || !await _permissionService.Authorize(StandardPermission.EnableShoppingCart);
            model.DisableWishlistButton = product.DisableWishlistButton || !await _permissionService.Authorize(StandardPermission.EnableWishlist);
            if (!await _permissionService.Authorize(StandardPermission.DisplayPrices))
            {
                model.DisableBuyButton = true;
                model.DisableWishlistButton = true;
            }
            //pre-order
            if (product.AvailableForPreOrder)
            {
                model.AvailableForPreOrder = !product.PreOrderDateTimeUtc.HasValue ||
                    product.PreOrderDateTimeUtc.Value >= DateTime.UtcNow;
                model.PreOrderDateTimeUtc = product.PreOrderDateTimeUtc;
            }

            //customer entered price
            model.EnteredPrice = product.EnteredPrice;
            if (model.EnteredPrice)
            {
                double minimumCustomerEnteredPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.MinEnteredPrice, _workContext.WorkingCurrency);
                double maximumCustomerEnteredPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(product.MaxEnteredPrice, _workContext.WorkingCurrency);

                model.CustomerEnteredPrice = updatecartitem != null && updatecartitem.EnteredPrice.HasValue ? updatecartitem.EnteredPrice.Value : minimumCustomerEnteredPrice;
                model.CustomerEnteredPriceRange = string.Format(_translationService.GetResource("Products.EnterProductPrice.Range"),
                    _priceFormatter.FormatPrice(minimumCustomerEnteredPrice, false),
                    _priceFormatter.FormatPrice(maximumCustomerEnteredPrice, false));
            }
            return model;
        }

        private ProductDetailsModel.GiftVoucherModel PrepareGiftVoucherModel(Product product, ShoppingCartItem updatecartitem = null)
        {
            var model = new ProductDetailsModel.GiftVoucherModel();
            model.IsGiftVoucher = product.IsGiftVoucher;
            if (model.IsGiftVoucher)
            {
                model.GiftVoucherType = product.GiftVoucherTypeId;

                if (updatecartitem == null)
                {
                    model.SenderName = _workContext.CurrentCustomer.GetFullName();
                    model.SenderEmail = _workContext.CurrentCustomer.Email;
                }
                else
                {
                    string giftVoucherRecipientName, giftVoucherRecipientEmail, giftVoucherSenderName, giftVoucherSenderEmail, giftVoucherMessage;
                    _productAttributeParser.GetGiftVoucherAttribute(updatecartitem.Attributes,
                        out giftVoucherRecipientName, out giftVoucherRecipientEmail,
                        out giftVoucherSenderName, out giftVoucherSenderEmail, out giftVoucherMessage);

                    model.RecipientName = giftVoucherRecipientName;
                    model.RecipientEmail = giftVoucherRecipientEmail;
                    model.SenderName = giftVoucherSenderName;
                    model.SenderEmail = giftVoucherSenderEmail;
                    model.Message = giftVoucherMessage;
                }
            }
            return model;
        }

        private async Task<IList<ProductDetailsModel.ProductAttributeModel>> PrepareProductAttributeModel(Product product, int defaultPictureSize, ShoppingCartItem updatecartitem = null)
        {
            var model = new List<ProductDetailsModel.ProductAttributeModel>();

            //performance optimization
            //We cache a value indicating whether a product has attributes
            IList<ProductAttributeMapping> productAttributeMapping = product.ProductAttributeMappings.ToList();

            foreach (var attribute in productAttributeMapping.OrderBy(x => x.DisplayOrder))
            {
                var productAttribute = await _productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
                if (productAttribute == null)
                    continue;

                var attributeModel = new ProductDetailsModel.ProductAttributeModel
                {
                    Id = attribute.Id,
                    ProductId = product.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = productAttribute.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    Description = productAttribute.GetTranslation(x => x.Description, _workContext.WorkingLanguage.Id),
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId,
                    DefaultValue = updatecartitem != null ? null : attribute.DefaultValue,
                    HasCondition = attribute.ConditionAttribute.Any()
                };
                if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                var urlselectedValues = !string.IsNullOrEmpty(productAttribute.SeName) ? _httpContextAccessor.HttpContext.Request.Query[productAttribute.SeName].ToList() : new List<string>();

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.ProductAttributeValues;
                    foreach (var attributeValue in attributeValues.OrderBy(x => x.DisplayOrder))
                    {
                        var preselected = attributeValue.IsPreSelected;
                        if (urlselectedValues.Any())
                            preselected = urlselectedValues.Contains(attributeValue.Name);

                        //Product Attribute Value - stock availability - support only for some conditions to show 
                        var stockAvailability = string.Empty;
                        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes
                            && product.ProductAttributeCombinations.Any()
                            && product.ProductAttributeMappings.Count == 1)
                        {
                            var customattributes = _productAttributeParser.AddProductAttribute(null, attribute, attributeValue.Id);
                            stockAvailability = _stockQuantityService.FormatStockMessage(product, string.Empty, customattributes);
                        }
                        var valueModel = new ProductDetailsModel.ProductAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                            ColorSquaresRgb = attributeValue.ColorSquaresRgb, //used with "Color squares" attribute type
                            IsPreSelected = preselected,
                            StockAvailability = stockAvailability,
                        };
                        attributeModel.Values.Add(valueModel);

                        //display price if allowed
                        var displayPrices = await _permissionService.Authorize(StandardPermission.DisplayPrices);
                        if (displayPrices)
                        {
                            double attributeValuePriceAdjustment = await _pricingService.GetProductAttributeValuePriceAdjustment(attributeValue);
                            var productprice = await _taxService.GetProductPrice(product, attributeValuePriceAdjustment);
                            double taxRate = productprice.taxRate;
                            if (productprice.productprice > 0)
                                valueModel.PriceAdjustment = "+" + _priceFormatter.FormatPrice(productprice.productprice, false);
                            else if (productprice.productprice < 0)
                                valueModel.PriceAdjustment = "-" + _priceFormatter.FormatPrice(-productprice.productprice, false);

                            valueModel.PriceAdjustmentValue = productprice.productprice;
                        }

                        //"image square" picture (with with "image squares" attribute type only)
                        if (!string.IsNullOrEmpty(attributeValue.ImageSquaresPictureId))
                        {
                            var pm = new PictureModel();
                            if (attributeValue.ImageSquaresPictureId != null)
                            {
                                pm = new PictureModel
                                {
                                    Id = attributeValue.ImageSquaresPictureId,
                                    FullSizeImageUrl = await _pictureService.GetPictureUrl(attributeValue.ImageSquaresPictureId),
                                    ImageUrl = await _pictureService.GetPictureUrl(attributeValue.ImageSquaresPictureId, _mediaSettings.ImageSquarePictureSize)
                                };
                            }
                            valueModel.ImageSquaresPictureModel = pm;
                        }

                        //picture of a product attribute value
                        if (!string.IsNullOrEmpty(attributeValue.PictureId))
                        {
                            var pm = new PictureModel();
                            if (attributeValue.PictureId != null)
                            {
                                pm = new PictureModel
                                {
                                    Id = attributeValue.PictureId,
                                    FullSizeImageUrl = await _pictureService.GetPictureUrl(attributeValue.PictureId),
                                    ImageUrl = await _pictureService.GetPictureUrl(attributeValue.PictureId, defaultPictureSize)
                                };
                            }
                            valueModel.PictureModel = pm;
                        }
                    }
                }

                //set already selected attributes (if we're going to update the existing shopping cart item)
                if (updatecartitem != null)
                {
                    switch (attribute.AttributeControlTypeId)
                    {
                        case AttributeControlType.DropdownList:
                        case AttributeControlType.RadioList:
                        case AttributeControlType.Checkboxes:
                        case AttributeControlType.ColorSquares:
                        case AttributeControlType.ImageSquares:
                            {
                                if (updatecartitem.Attributes != null && updatecartitem.Attributes.Any())
                                {
                                    //clear default selection
                                    foreach (var item in attributeModel.Values)
                                        item.IsPreSelected = false;

                                    //select new values
                                    var selectedValues = _productAttributeParser.ParseProductAttributeValues(product, updatecartitem.Attributes);
                                    foreach (var attributeValue in selectedValues)
                                        foreach (var item in attributeModel.Values)
                                            if (attributeValue.Id == item.Id)
                                                item.IsPreSelected = true;

                                }
                            }
                            break;
                        case AttributeControlType.ReadonlyCheckboxes:
                            {
                                //do nothing
                                //values are already pre-set
                            }
                            break;
                        case AttributeControlType.TextBox:
                        case AttributeControlType.MultilineTextbox:
                            {
                                if (updatecartitem.Attributes != null && updatecartitem.Attributes.Any())
                                {
                                    var enteredText = _productAttributeParser.ParseValues(updatecartitem.Attributes, attribute.Id);
                                    if (enteredText.Any())
                                        attributeModel.DefaultValue = enteredText[0];
                                }
                            }
                            break;
                        case AttributeControlType.Datepicker:
                            {
                                //keep in mind my that the code below works only in the current culture
                                var selectedDateStr = _productAttributeParser.ParseValues(updatecartitem.Attributes, attribute.Id);
                                if (selectedDateStr.Any())
                                {
                                    DateTime selectedDate;
                                    if (DateTime.TryParseExact(selectedDateStr[0], "D", CultureInfo.CurrentCulture,
                                                           DateTimeStyles.None, out selectedDate))
                                    {
                                        //successfully parsed
                                        attributeModel.SelectedDay = selectedDate.Day;
                                        attributeModel.SelectedMonth = selectedDate.Month;
                                        attributeModel.SelectedYear = selectedDate.Year;
                                    }
                                }

                            }
                            break;
                        case AttributeControlType.FileUpload:
                            {
                                if (updatecartitem.Attributes != null && updatecartitem.Attributes.Any())
                                {
                                    var downloadGuidStr = _productAttributeParser.ParseValues(updatecartitem.Attributes, attribute.Id).FirstOrDefault();
                                    Guid downloadGuid;
                                    Guid.TryParse(downloadGuidStr, out downloadGuid);
                                    var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                                    if (download != null)
                                        attributeModel.DefaultValue = download.DownloadGuid.ToString();
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

                model.Add(attributeModel);
            }
            return model;
        }

        private async Task<IList<ProductDetailsModel.TierPriceModel>> PrepareProductTierPriceModel(Product product)
        {
            var model = new List<ProductDetailsModel.TierPriceModel>();
            foreach (var tierPrice in product.TierPrices.OrderBy(x => x.Quantity)
                    .FilterByStore(_workContext.CurrentStore.Id)
                    .FilterByCurrency(_workContext.WorkingCurrency.CurrencyCode)
                    .FilterForCustomer(_workContext.CurrentCustomer)
                    .FilterByDate()
                    .RemoveDuplicatedQuantities())
            {
                var tier = new ProductDetailsModel.TierPriceModel();
                var priceBase = await _taxService.GetProductPrice(product, (await _pricingService.GetFinalPrice(product,
                                       _workContext.CurrentCustomer, _workContext.WorkingCurrency,
                                       0, _catalogSettings.DisplayTierPricesWithDiscounts, tierPrice.Quantity)).finalPrice);
                tier.Quantity = tierPrice.Quantity;
                tier.Price = _priceFormatter.FormatPrice(priceBase.productprice, false);
                model.Add(tier);
            }
            return model;
        }

        private async Task PrepareProductReservation(ProductDetailsModel model, Product product)
        {
            if (product.ProductTypeId == ProductType.Reservation)
            {
                model.AddToCart.IsReservation = true;
                var reservations = await _productReservationService.GetProductReservationsByProductId(product.Id, true, null);
                var inCart = _workContext.CurrentCustomer.ShoppingCartItems.Where(x => !string.IsNullOrEmpty(x.ReservationId)).ToList();
                foreach (var cartItem in inCart)
                {
                    var matching = reservations.Where(x => x.Id == cartItem.ReservationId);
                    if (matching.Any())
                    {
                        reservations.Remove(matching.First());
                    }
                }

                if (reservations.Any())
                {
                    var first = reservations.Where(x => x.Date >= DateTime.UtcNow).OrderBy(x => x.Date).FirstOrDefault();
                    if (first != null)
                    {
                        model.StartDate = first.Date;
                    }
                    else
                    {
                        model.StartDate = DateTime.UtcNow;
                    }
                }

                model.IntervalUnit = product.IntervalUnitId;
                model.Interval = product.Interval;
                model.IncBothDate = product.IncBothDate;

                var list = reservations.GroupBy(x => x.Parameter).ToList().Select(x => x.Key);
                foreach (var item in list)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        model.Parameters.Add(new SelectListItem { Text = item, Value = item });
                    }
                }

                if (model.Parameters.Any())
                {
                    model.Parameters.Insert(0, new SelectListItem { Text = "", Value = "" });
                }
            }
        }

        private async Task<IList<ProductDetailsModel.ProductBundleModel>> PrepareProductBundleModel(Product product)
        {
            var model = new List<ProductDetailsModel.ProductBundleModel>();
            var displayPrices = await _permissionService.Authorize(StandardPermission.DisplayPrices);

            foreach (var bundle in product.BundleProducts.OrderBy(x => x.DisplayOrder))
            {
                var p1 = await _productService.GetProductById(bundle.ProductId);
                if (p1 != null && p1.Published && p1.IsAvailable())
                {
                    var bundleProduct = new ProductDetailsModel.ProductBundleModel()
                    {
                        ProductId = p1.Id,
                        Name = p1.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        ShortDescription = p1.GetTranslation(x => x.ShortDescription, _workContext.WorkingLanguage.Id),
                        SeName = p1.GetSeName(_workContext.WorkingLanguage.Id),
                        Sku = p1.Sku,
                        Mpn = p1.Mpn,
                        Gtin = p1.Gtin,
                        Quantity = bundle.Quantity,
                        UserFields = p1.UserFields
                    };
                    if (displayPrices)
                    {
                        var productprice = await _taxService.GetProductPrice(p1, (await _pricingService.GetFinalPrice(p1, _workContext.CurrentCustomer, _workContext.WorkingCurrency, includeDiscounts: true)).finalPrice);
                        double taxRateBundle = productprice.taxRate;
                        double finalPriceWithDiscountBase = productprice.productprice;
                        bundleProduct.Price = _priceFormatter.FormatPrice(productprice.productprice);
                        bundleProduct.PriceValue = productprice.productprice;
                    }

                    var productPicture = p1.ProductPictures.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                    if (productPicture == null)
                        productPicture = new ProductPicture();

                    var picture = await _pictureService.GetPictureById(productPicture.PictureId);

                    var pictureModel = new PictureModel
                    {
                        Id = productPicture.PictureId,
                        ImageUrl = await _pictureService.GetPictureUrl(productPicture.PictureId, _mediaSettings.ProductBundlePictureSize),
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(productPicture.PictureId)
                    };
                    //"title" attribute
                    pictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute)) ?
                        picture.TitleAttribute :
                        string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat.Details"), p1.Name);
                    //"alt" attribute
                    pictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute)) ?
                        picture.AltAttribute :
                        string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat.Details"), p1.Name);

                    bundleProduct.DefaultPictureModel = pictureModel;

                    bundleProduct.ProductAttributes = await PrepareProductAttributeModel(p1, _mediaSettings.ProductBundlePictureSize);

                    model.Add(bundleProduct);
                }
            }
            return model;
        }
    }
}
