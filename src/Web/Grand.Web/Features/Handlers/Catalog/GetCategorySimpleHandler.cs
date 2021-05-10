using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.SharedKernel.Extensions;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCategorySimpleHandler : IRequestHandler<GetCategorySimple, IList<CategorySimpleModel>>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICategoryService _categoryService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IMediator _mediator;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;

        public GetCategorySimpleHandler(
            ICacheBase cacheBase,
            ICategoryService categoryService,
            IPictureService pictureService,
            IProductService productService,
            IMediator mediator,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings)
        {
            _cacheBase = cacheBase;
            _categoryService = categoryService;
            _pictureService = pictureService;
            _productService = productService;
            _mediator = mediator;
            _mediaSettings = mediaSettings;
            _catalogSettings = catalogSettings;
        }

        public async Task<IList<CategorySimpleModel>> Handle(GetCategorySimple request, CancellationToken cancellationToken)
        {
            return await PrepareCategorySimpleModels(request);
        }

        private async Task<List<CategorySimpleModel>> PrepareCategorySimpleModels(GetCategorySimple request)
        {
            var currentCategory = await _categoryService.GetCategoryById(request.CurrentCategoryId);
            string cacheKey = string.Format(CacheKeyConst.CATEGORY_ALL_MODEL_KEY,
                request.Language.Id,
                request.Store.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.CurrentCategoryId);

            return await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var categories = new List<Category>();

                async Task PrepareCategories(string categoryId)
                {
                    var parentCategories = await _categoryService.GetAllCategories(parentId: categoryId);
                    if (parentCategories.Any())
                    {
                        categories.AddRange(parentCategories);
                        var parent = await _categoryService.GetCategoryById(parentCategories.FirstOrDefault().ParentCategoryId);
                        if (parent != null)
                            await PrepareCategories(parent.ParentCategoryId);
                    }

                }
                if (currentCategory != null)
                {
                    var currentCategories = await _categoryService.GetAllCategories(parentId: currentCategory.Id);
                    categories.AddRange(currentCategories);
                    await PrepareCategories(currentCategory.ParentCategoryId);
                }
                else
                    await PrepareCategories("");

                return await PrepareCategorySimpleModels(request, "", true, categories.ToList());
            });
        }

        private async Task<List<CategorySimpleModel>> PrepareCategorySimpleModels(GetCategorySimple request, string rootCategoryId,
            bool loadSubCategories = true, List<Category> allCategories = null)
        {
            var result = new List<CategorySimpleModel>();

            var categories = allCategories.Where(c => c.ParentCategoryId == rootCategoryId).ToList();
            foreach (var category in categories)
            {
                var picture = await _pictureService.GetPictureById(category.PictureId);
                var categoryModel = new CategorySimpleModel
                {
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
                    var categoryIds = new List<string>();
                    categoryIds.Add(category.Id);
                    //include subcategories
                    if (_catalogSettings.ShowCategoryProductNumberIncludingSubcategories)
                        categoryIds.AddRange(await _mediator.Send(new GetChildCategoryIds() { Customer = request.Customer, Store = request.Store, ParentCategoryId = category.Id }));
                    categoryModel.NumberOfProducts = _productService.GetCategoryProductNumber(request.Customer, categoryIds, request.Store.Id, CommonHelper.IgnoreAcl, CommonHelper.IgnoreStoreLimitations);
                }
                if (loadSubCategories)
                {
                    var subCategories = await PrepareCategorySimpleModels(request, category.Id, loadSubCategories, allCategories);
                    categoryModel.SubCategories.AddRange(subCategories);
                }
                result.Add(categoryModel);
            }

            return result;
        }
    }
}
