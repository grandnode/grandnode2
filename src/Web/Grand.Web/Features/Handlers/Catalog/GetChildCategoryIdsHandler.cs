﻿using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetChildCategoryIdsHandler : IRequestHandler<GetChildCategoryIds, IList<string>>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICategoryService _categoryService;

        public GetChildCategoryIdsHandler(ICacheBase cacheBase, ICategoryService categoryService)
        {
            _cacheBase = cacheBase;
            _categoryService = categoryService;
        }

        public async Task<IList<string>> Handle(GetChildCategoryIds request, CancellationToken cancellationToken)
        {
            return await GetChildCategoryIds(request);
        }

        private async Task<List<string>> GetChildCategoryIds(GetChildCategoryIds request)
        {
            var cacheKey = string.Format(CacheKeyConst.CATEGORY_CHILD_IDENTIFIERS_MODEL_KEY,
                request.ParentCategoryId,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            return await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var categoriesIds = new List<string>();
                var categories = await _categoryService.GetAllCategoriesByParentCategoryId(request.ParentCategoryId);
                foreach (var category in categories)
                {
                    categoriesIds.Add(category.Id);
                    request.ParentCategoryId = category.Id;
                    categoriesIds.AddRange(await GetChildCategoryIds(request));
                }
                return categoriesIds;
            });
        }
    }
}
