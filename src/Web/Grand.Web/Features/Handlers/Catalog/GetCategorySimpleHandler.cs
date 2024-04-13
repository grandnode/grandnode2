using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCategorySimpleHandler : IRequestHandler<GetCategorySimple, IList<CategorySimpleModel>>
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly ICacheBase _cacheBase;
    private readonly CatalogSettings _catalogSettings;
    private readonly ICategoryService _categoryService;
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;

    public GetCategorySimpleHandler(
        ICacheBase cacheBase,
        ICategoryService categoryService,
        IPictureService pictureService,
        IProductService productService,
        IMediator mediator,
        MediaSettings mediaSettings,
        CatalogSettings catalogSettings,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _categoryService = categoryService;
        _pictureService = pictureService;
        _productService = productService;
        _mediator = mediator;
        _mediaSettings = mediaSettings;
        _catalogSettings = catalogSettings;
        _accessControlConfig = accessControlConfig;
    }

    public async Task<IList<CategorySimpleModel>> Handle(GetCategorySimple request, CancellationToken cancellationToken)
    {
        return await PrepareCategorySimpleModels(request);
    }

    private async Task<List<CategorySimpleModel>> PrepareCategorySimpleModels(GetCategorySimple request)
    {
        var currentCategory = await _categoryService.GetCategoryById(request.CurrentCategoryId);
        var cacheKey = string.Format(CacheKeyConst.CATEGORY_ALL_MODEL_KEY,
            request.Language.Id,
            request.Store.Id,
            string.Join(",", request.Customer.GetCustomerGroupIds()),
            request.CurrentCategoryId);

        return await _cacheBase.GetAsync(cacheKey, async () =>
        {
            var categories = new List<Category>();

            async Task PrepareCategories(string categoryId)
            {
                var parentCategories = await _categoryService.GetAllCategories(categoryId, storeId: request.Store.Id);
                if (parentCategories.Any())
                {
                    categories.AddRange(parentCategories);
                    var parent =
                        await _categoryService.GetCategoryById(parentCategories.FirstOrDefault()!.ParentCategoryId);
                    if (parent != null)
                        await PrepareCategories(parent.ParentCategoryId);
                }
            }

            if (currentCategory != null)
            {
                var currentCategories =
                    await _categoryService.GetAllCategories(currentCategory.Id, storeId: request.Store.Id);
                categories.AddRange(currentCategories);
                await PrepareCategories(currentCategory.ParentCategoryId);
            }
            else
            {
                await PrepareCategories("");
            }

            return await PrepareCategorySimpleModels(request, "", categories);
        });
    }

    private async Task<List<CategorySimpleModel>> PrepareCategorySimpleModels(GetCategorySimple request,
        string rootCategoryId,
        IEnumerable<Category> allCategories, bool loadSubCategories = true)
    {
        var result = new List<CategorySimpleModel>();
        if (allCategories == null) return result;

        var categories = allCategories.Where(c => c.ParentCategoryId == rootCategoryId).ToList();
        foreach (var category in categories)
        {
            var picture = await _pictureService.GetPictureById(category.PictureId);
            var categoryModel = new CategorySimpleModel {
                Id = category.Id,
                Name = category.GetTranslation(x => x.Name, request.Language.Id),
                SeName = category.GetSeName(request.Language.Id),
                IncludeInMenu = category.IncludeInMenu,
                Flag = category.GetTranslation(x => x.Flag, request.Language.Id),
                FlagStyle = category.FlagStyle,
                Icon = category.Icon,
                ImageUrl = await _pictureService.GetPictureUrl(picture, _mediaSettings.CategoryThumbPictureSize),
                UserFields = category.UserFields
            };

            //product number for each category
            if (_catalogSettings.ShowCategoryProductNumber)
            {
                var categoryIds = new List<string> { category.Id };
                //include subcategories
                if (_catalogSettings.ShowCategoryProductNumberIncludingSubcategories)
                    categoryIds.AddRange(await _mediator.Send(new GetChildCategoryIds
                        { Customer = request.Customer, Store = request.Store, ParentCategoryId = category.Id }));
                categoryModel.NumberOfProducts = _productService.GetCategoryProductNumber(request.Customer, categoryIds,
                    request.Store.Id, _accessControlConfig.IgnoreAcl, _accessControlConfig.IgnoreStoreLimitations);
            }

            if (loadSubCategories)
            {
                var subCategories = await PrepareCategorySimpleModels(request, category.Id, allCategories);
                categoryModel.SubCategories.AddRange(subCategories);
            }

            result.Add(categoryModel);
        }

        return result;
    }
}