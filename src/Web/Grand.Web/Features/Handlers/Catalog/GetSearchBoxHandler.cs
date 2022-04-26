using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetSearchBoxHandler : IRequestHandler<GetSearchBox, SearchBoxModel>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICategoryService _categoryService;
        private readonly ITranslationService _translationService;
        private readonly CatalogSettings _catalogSettings;

        public GetSearchBoxHandler(
            ICacheBase cacheBase,
            ICategoryService categoryService,
            ITranslationService translationService,
            CatalogSettings catalogSettings)
        {
            _cacheBase = cacheBase;
            _categoryService = categoryService;
            _translationService = translationService;
            _catalogSettings = catalogSettings;
        }

        public async Task<SearchBoxModel> Handle(GetSearchBox request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.CATEGORY_ALL_SEARCHBOX,
                request.Language.Id, request.Store.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()));

            return await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var searchbocategories = await _categoryService.GetAllCategoriesSearchBox();

                var availableCategories = new List<SelectListItem>();
                if (searchbocategories.Any())
                {
                    availableCategories.Add(new SelectListItem { Text = _translationService.GetResource("Common.All"), Value = "" });
                    foreach (var s in searchbocategories)
                        availableCategories.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
                }

                var model = new SearchBoxModel {
                    AutoCompleteEnabled = _catalogSettings.ProductSearchAutoCompleteEnabled,
                    ShowProductImagesInSearchAutoComplete = _catalogSettings.ShowProductImagesInSearchAutoComplete,
                    SearchTermMinimumLength = _catalogSettings.ProductSearchTermMinimumLength,
                    AvailableCategories = availableCategories
                };

                return model;
            });

        }
    }
}
