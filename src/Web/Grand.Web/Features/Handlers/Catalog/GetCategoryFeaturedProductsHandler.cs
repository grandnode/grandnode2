using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCategoryFeaturedProductsHandler : IRequestHandler<GetCategoryFeaturedProducts, IList<CategoryModel>>
{
    private readonly ICacheBase _cacheBase;
    private readonly CatalogSettings _catalogSettings;
    private readonly ICategoryService _categoryService;
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;

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

    public async Task<IList<CategoryModel>> Handle(GetCategoryFeaturedProducts request,
        CancellationToken cancellationToken)
    {
        var categoriesCacheKey = string.Format(CacheKeyConst.CATEGORY_FEATURED_PRODUCTS_HOMEPAGE_KEY,
            string.Join(",", request.Customer.GetCustomerGroupIds()), request.Store.Id,
            request.Language.Id);

        var model = await _cacheBase.GetAsync(categoriesCacheKey, async () =>
        {
            var catlistmodel = new List<CategoryModel>();
            foreach (var x in await _categoryService.GetAllCategoriesFeaturedProductsOnHomePage())
            {
                var catModel = x.ToModel(request.Language);
                //prepare picture model
                var picture = !string.IsNullOrEmpty(x.PictureId)
                    ? await _pictureService.GetPictureById(x.PictureId)
                    : null;
                catModel.PictureModel = new PictureModel {
                    Id = x.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                    ImageUrl =
                        await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                    Style = picture?.Style,
                    ExtraField = picture?.ExtraField,
                    //"title" attribute
                    Title =
                        picture != null &&
                        !string.IsNullOrEmpty(picture.GetTranslation(z => z.TitleAttribute, request.Language.Id))
                            ? picture.GetTranslation(z => z.TitleAttribute, request.Language.Id)
                            : string.Format(_translationService.GetResource("Media.Category.ImageLinkTitleFormat"),
                                x.Name),
                    //"alt" attribute
                    AlternateText =
                        picture != null &&
                        !string.IsNullOrEmpty(picture.GetTranslation(z => z.AltAttribute, request.Language.Id))
                            ? picture.GetTranslation(z => z.AltAttribute, request.Language.Id)
                            : string.Format(_translationService.GetResource("Media.Category.ImageAlternateTextFormat"),
                                x.Name)
                };

                catlistmodel.Add(catModel);
            }

            return catlistmodel;
        });


        foreach (var item in model)
        {
            //We cache a value indicating whether we have featured products
            IPagedList<Product> featuredProducts = null;
            var cacheKey = string.Format(CacheKeyConst.CATEGORY_HAS_FEATURED_PRODUCTS_KEY, item.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()), request.Store.Id);

            var hasFeaturedProductsCache = await _cacheBase.GetAsync<bool?>(cacheKey, async () =>
            {
                featuredProducts = (await _mediator.Send(new GetSearchProductsQuery {
                    PageSize = _catalogSettings.LimitOfFeaturedProducts,
                    CategoryIds = new List<string> { item.Id },
                    Customer = request.Customer,
                    StoreId = request.Store.Id,
                    VisibleIndividuallyOnly = true,
                    FeaturedProducts = true
                }, cancellationToken)).products;
                return featuredProducts.Any();
            });

            if (hasFeaturedProductsCache.HasValue && hasFeaturedProductsCache.Value && featuredProducts == null)
                //cache indicates that the category has featured products
                featuredProducts = (await _mediator.Send(new GetSearchProductsQuery {
                    PageSize = _catalogSettings.LimitOfFeaturedProducts,
                    CategoryIds = new List<string> { item.Id },
                    Customer = request.Customer,
                    StoreId = request.Store.Id,
                    VisibleIndividuallyOnly = true,
                    FeaturedProducts = true
                }, cancellationToken)).products;
            if (featuredProducts != null && featuredProducts.Any())
                item.FeaturedProducts = (await _mediator.Send(new GetProductOverview {
                    PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                    Products = featuredProducts
                }, cancellationToken)).ToList();
        }

        return model;
    }
}