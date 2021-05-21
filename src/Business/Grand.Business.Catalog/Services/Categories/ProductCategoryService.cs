using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Categories
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        public ProductCategoryService(
            IRepository<Product> productRepository,
            ICacheBase cacheBase,
            IWorkContext workContext,
            IMediator mediator)
        {
            _productRepository = productRepository;
            _cacheBase = cacheBase;
            _workContext = workContext;
            _mediator = mediator;
        }

        /// <summary>
        /// Gets product category mapping collection
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Product a category mapping collection</returns>
        public virtual async Task<IPagedList<ProductsCategory>> GetProductCategoriesByCategoryId(string categoryId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            if (String.IsNullOrEmpty(categoryId))
                return new PagedList<ProductsCategory>(new List<ProductsCategory>(), pageIndex, pageSize);

            string key = string.Format(CacheKey.PRODUCTCATEGORIES_ALLBYCATEGORYID_KEY, showHidden, categoryId, pageIndex, pageSize, _workContext.CurrentCustomer.Id, _workContext.CurrentStore.Id);
            return await _cacheBase.GetAsync(key, () =>
            {
                var query = _productRepository.Table.Where(x => x.ProductCategories.Any(y => y.CategoryId == categoryId));

                if (!showHidden && (!CommonHelper.IgnoreAcl || !CommonHelper.IgnoreStoreLimitations))
                {
                    if (!CommonHelper.IgnoreAcl)
                    {
                        //Limited to customer groups
                        var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                        query = from p in query
                                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                                select p;
                    }
                    if (!CommonHelper.IgnoreStoreLimitations)
                    {
                        //Limited to stores
                        var currentStoreId = _workContext.CurrentStore.Id;
                        query = from p in query
                                where !p.LimitedToStores || p.Stores.Contains(currentStoreId)
                                select p;

                    }


                }
                var query_productCategories = from prod in query
                                              from pc in prod.ProductCategories
                                              select new ProductsCategory
                                              {
                                                  CategoryId = pc.CategoryId,
                                                  DisplayOrder = pc.DisplayOrder,
                                                  Id = pc.Id,
                                                  ProductId = prod.Id,
                                                  IsFeaturedProduct = pc.IsFeaturedProduct,
                                              };

                query_productCategories = from pm in query_productCategories
                                          where pm.CategoryId == categoryId
                                          orderby pm.DisplayOrder
                                          select pm;

                return Task.FromResult(new PagedList<ProductsCategory>(query_productCategories, pageIndex, pageSize));
            });
        }


        /// <summary>
        /// Inserts a product category
        /// </summary>
        /// <param name="productCategory">>Product category mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductCategory(ProductCategory productCategory, string productId)
        {
            if (productCategory == null)
                throw new ArgumentNullException(nameof(productCategory));

            await _productRepository.AddToSet(productId, x => x.ProductCategories, productCategory);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productCategory);
        }

        /// <summary>
        /// Updates the associated product category
        /// </summary>
        /// <param name="productCategory">>Product category mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateProductCategory(ProductCategory productCategory, string productId)
        {
            if (productCategory == null)
                throw new ArgumentNullException(nameof(productCategory));

            await _productRepository.UpdateToSet(productId, x => x.ProductCategories, z => z.Id, productCategory.Id, productCategory);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productCategory);
        }
        /// <summary>
        /// Deletes a associated product category
        /// </summary>
        /// <param name="productCategory">Product category</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductCategory(ProductCategory productCategory, string productId)
        {
            if (productCategory == null)
                throw new ArgumentNullException(nameof(productCategory));

            await _productRepository.PullFilter(productId, x => x.ProductCategories, z => z.Id, productCategory.Id);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCATEGORIES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productCategory);

        }

    }
}
