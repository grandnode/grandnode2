using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCollectionFeaturedProductsHandler : IRequestHandler<GetCollectionFeaturedProducts, IList<CollectionModel>>
    {
        private readonly IMediator _mediator;
        private readonly ICollectionService _collectionService;
        private readonly ICacheBase _cacheBase;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetCollectionFeaturedProductsHandler(
            IMediator mediator,
            ICollectionService collectionService,
            ICacheBase cacheBase,
            IPictureService pictureService,
            ITranslationService translationService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings)
        {
            _mediator = mediator;
            _collectionService = collectionService;
            _cacheBase = cacheBase;
            _pictureService = pictureService;
            _translationService = translationService;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
        }

        public async Task<IList<CollectionModel>> Handle(GetCollectionFeaturedProducts request, CancellationToken cancellationToken)
        {
            string manufCacheKey = string.Format(CacheKeyConst.COLLECTION_FEATURED_PRODUCT_HOMEPAGE_KEY,
                            string.Join(",", request.Customer.GetCustomerGroupIds()), request.Store.Id,
                            request.Language.Id);

            var model = await _cacheBase.GetAsync(manufCacheKey, async () =>
            {
                var manufList = new List<CollectionModel>();
                var manufmodel = await _collectionService.GetAllCollectionFeaturedProductsOnHomePage();
                foreach (var x in manufmodel)
                {
                    var manModel = x.ToModel(request.Language);
                    //prepare picture model
                    manModel.PictureModel = new PictureModel
                    {
                        Id = x.PictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                        Title = string.Format(_translationService.GetResource("Media.Category.ImageLinkTitleFormat"), manModel.Name),
                        AlternateText = string.Format(_translationService.GetResource("Media.Category.ImageAlternateTextFormat"), manModel.Name)
                    };
                    manufList.Add(manModel);
                }
                return manufList;
            });

            foreach (var item in model)
            {
                //We cache a value indicating whether we have featured products
                IPagedList<Product> featuredProducts = null;

                string cacheKey = string.Format(CacheKeyConst.COLLECTION_HAS_FEATURED_PRODUCTS_KEY,
                    item.Id,
                    string.Join(",", request.Customer.GetCustomerGroupIds()),
                    request.Store.Id);

                var hasFeaturedProductsCache = await _cacheBase.GetAsync<bool?>(cacheKey, async () =>
                {
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery()
                    {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        CollectionId = item.Id,
                        Customer = request.Customer,
                        StoreId = request.Store.Id,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                    return featuredProducts.Any();
                });

                if (hasFeaturedProductsCache.Value && featuredProducts == null)
                {
                    //cache indicates that the collection has featured products
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery()
                    {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        Customer = request.Customer,
                        CollectionId = item.Id,
                        StoreId = request.Store.Id,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                }
                if (featuredProducts != null && featuredProducts.Any())
                {
                    item.FeaturedProducts = (await _mediator.Send(new GetProductOverview()
                    {
                        Products = featuredProducts,
                    })).ToList();
                }
            }
            return model;
        }
    }
}
