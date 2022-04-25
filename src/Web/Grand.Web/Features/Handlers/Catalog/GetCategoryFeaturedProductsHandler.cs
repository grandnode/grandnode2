using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
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
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Queries.Catalog;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCategoryFeaturedProductsHandler : IRequestHandler<GetCategoryFeaturedProducts, IList<CategoryModel>>
    {
        private readonly ICategoryService _categoryService;
        private readonly ICacheBase _cacheBase;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly IMediator _mediator;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetCategoryFeaturedProductsHandler(
            ICategoryService categoryService,
            ICacheBase cacheBase,
            IPictureService pictureService,
            ITranslationService translationService,
            IMediator mediator,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings)
        {
            _categoryService = categoryService;
            _cacheBase = cacheBase;
            _pictureService = pictureService;
            _translationService = translationService;
            _mediator = mediator;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
        }

        public async Task<IList<CategoryModel>> Handle(GetCategoryFeaturedProducts request, CancellationToken cancellationToken)
        {
            string categoriesCacheKey = string.Format(CacheKeyConst.CATEGORY_FEATURED_PRODUCTS_HOMEPAGE_KEY,
                string.Join(",", request.Customer.GetCustomerGroupIds()), request.Store.Id,
                request.Language.Id);

            var model = await _cacheBase.GetAsync(categoriesCacheKey, async () =>
            {
                var catlistmodel = new List<CategoryModel>();
                foreach (var x in await _categoryService.GetAllCategoriesFeaturedProductsOnHomePage())
                {
                    var catModel = x.ToModel(request.Language);
                    //prepare picture model
                    var picture = !string.IsNullOrEmpty(x.PictureId) ? await _pictureService.GetPictureById(x.PictureId) : null;
                    catModel.PictureModel = new PictureModel {
                        Id = x.PictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                        Style = picture?.Style,
                        ExtraField = picture?.ExtraField
                    };
                    //"title" attribute
                    catModel.PictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))) ?
                        picture.GetTranslation(x => x.TitleAttribute, request.Language.Id) :
                        string.Format(_translationService.GetResource("Media.Category.ImageLinkTitleFormat"), x.Name);
                    //"alt" attribute
                    catModel.PictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))) ?
                        picture.GetTranslation(x => x.AltAttribute, request.Language.Id) :
                        string.Format(_translationService.GetResource("Media.Category.ImageAlternateTextFormat"), x.Name);

                    catlistmodel.Add(catModel);
                }
                return catlistmodel;
            });


            foreach (var item in model)
            {
                //We cache a value indicating whether we have featured products
                IPagedList<Product> featuredProducts = null;
                string cacheKey = string.Format(CacheKeyConst.CATEGORY_HAS_FEATURED_PRODUCTS_KEY, item.Id,
                    string.Join(",", request.Customer.GetCustomerGroupIds()), request.Store.Id);

                var hasFeaturedProductsCache = await _cacheBase.GetAsync<bool?>(cacheKey, async () =>
                {
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery() {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        CategoryIds = new List<string> { item.Id },
                        Customer = request.Customer,
                        StoreId = request.Store.Id,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                    return featuredProducts.Any();
                });

                if (hasFeaturedProductsCache.Value && featuredProducts == null)
                {
                    //cache indicates that the category has featured products
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery() {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        CategoryIds = new List<string> { item.Id },
                        Customer = request.Customer,
                        StoreId = request.Store.Id,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                }
                if (featuredProducts != null && featuredProducts.Any())
                {
                    item.FeaturedProducts = (await _mediator.Send(new GetProductOverview() {
                        PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                        Products = featuredProducts,
                    })).ToList();
                }
            }

            return model;
        }
    }
}
