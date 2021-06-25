using Grand.Business.Catalog.Events.Models;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Catalog.Queries.Models;
using Grand.Business.Common.Interfaces.Security;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Product service
    /// </summary>
    public partial class ProductService : IProductService
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public ProductService(ICacheBase cacheBase,
            IRepository<Product> productRepository,
            IWorkContext workContext,
            IMediator mediator,
            IAclService aclService
            )
        {
            _cacheBase = cacheBase;
            _productRepository = productRepository;
            _workContext = workContext;
            _mediator = mediator;
            _aclService = aclService;
        }

        #endregion

        #region Methods

        #region Products

        /// <summary>
        /// Gets all products displayed on the home page
        /// </summary>
        /// <returns>Products</returns>
        public virtual async Task<IList<string>> GetAllProductsDisplayedOnHomePage()
        {
            var query = _productRepository.Table.Where(x => x.Published && x.ShowOnHomePage && x.VisibleIndividually)
                        .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Select(x => x.Id);

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets all products displayed on the best seller
        /// </summary>
        /// <returns>Products</returns>
        public virtual async Task<IList<string>> GetAllProductsDisplayedOnBestSeller()
        {
            var query = _productRepository.Table.Where(x => x.Published && x.BestSeller && x.VisibleIndividually)
                        .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                        .Select(x => x.Id);

            var products = await Task.FromResult(query.ToList());

            return products;
        }

        /// <summary>
        /// Gets product
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="fromDB">get data from db (not from cache)</param>
        /// <returns>Product</returns>
        public virtual async Task<Product> GetProductById(string productId, bool fromDB = false)
        {
            if (string.IsNullOrEmpty(productId))
                return null;

            if (fromDB)
                return await _productRepository.GetByIdAsync(productId);

            var key = string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId);
            return await _cacheBase.GetAsync(key, () => _productRepository.GetByIdAsync(productId));
        }

        /// <summary>
        /// Gets product for order
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>Product</returns>
        public virtual async Task<Product> GetProductByIdIncludeArch(string productId)
        {
            if (String.IsNullOrEmpty(productId))
                return null;
            var product = await GetProductById(productId);
            if (product == null)
                product = await _mediator.Send(new GetProductArchByIdQuery() { Id = productId });

            return product;
        }


        /// <summary>
        /// Get products by identifiers
        /// </summary>
        /// <param name="productIds">Product identifiers</param>
        /// <returns>Products</returns>
        public virtual async Task<IList<Product>> GetProductsByIds(string[] productIds, bool showHidden = false)
        {
            if (productIds == null || productIds.Length == 0)
                return new List<Product>();

            var products = new List<Product>();
            foreach (string id in productIds)
            {
                var product = await GetProductById(id);
                if (product != null && (showHidden || (_aclService.Authorize(product, _workContext.CurrentCustomer) && _aclService.Authorize(product, _workContext.CurrentStore.Id) && (product.IsAvailable()))))
                    products.Add(product);
            }
            return products;
        }

        /// <summary>
        /// Gets products by discount
        /// </summary>
        /// <param name="discountId">Product identifiers</param>
        /// <returns>Products</returns>
        public virtual async Task<IPagedList<Product>> GetProductsByDiscount(string discountId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from c in _productRepository.Table
                        where c.AppliedDiscounts.Any(x => x == discountId)
                        select c;

            return await PagedList<Product>.Create(query, pageIndex, pageSize);
        }


        /// <summary>
        /// Inserts a product
        /// </summary>
        /// <param name="product">Product</param>
        public virtual async Task InsertProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //insert
            await _productRepository.InsertAsync(product);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(product);
        }

        /// <summary>
        /// Updates the product
        /// </summary>
        /// <param name="product">Product</param>
        public virtual async Task UpdateProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var oldProduct = await _productRepository.GetByIdAsync(product.Id);
            //update
            var update = UpdateBuilder<Product>.Create()
                .Set(x => x.AdditionalShippingCharge, product.AdditionalShippingCharge)
                .Set(x => x.AdminComment, product.AdminComment)
                .Set(x => x.AllowOutOfStockSubscriptions, product.AllowOutOfStockSubscriptions)
                .Set(x => x.AllowCustomerReviews, product.AllowCustomerReviews)
                .Set(x => x.AllowedQuantities, product.AllowedQuantities)
                .Set(x => x.ApprovedRatingSum, product.ApprovedRatingSum)
                .Set(x => x.ApprovedTotalReviews, product.ApprovedTotalReviews)
                .Set(x => x.AutoAddRequiredProducts, product.AutoAddRequiredProducts)
                .Set(x => x.AvailableEndDateTimeUtc, product.AvailableEndDateTimeUtc)
                .Set(x => x.AvailableForPreOrder, product.AvailableForPreOrder)
                .Set(x => x.AvailableStartDateTimeUtc, product.AvailableStartDateTimeUtc)
                .Set(x => x.BackorderModeId, product.BackorderModeId)
                .Set(x => x.BasepriceAmount, product.BasepriceAmount)
                .Set(x => x.BasepriceBaseAmount, product.BasepriceBaseAmount)
                .Set(x => x.BasepriceBaseUnitId, product.BasepriceBaseUnitId)
                .Set(x => x.BasepriceEnabled, product.BasepriceEnabled)
                .Set(x => x.BasepriceUnitId, product.BasepriceUnitId)
                .Set(x => x.CallForPrice, product.CallForPrice)
                .Set(x => x.CatalogPrice, product.CatalogPrice)
                .Set(x => x.CreatedOnUtc, product.CreatedOnUtc)
                .Set(x => x.EnteredPrice, product.EnteredPrice)
                .Set(x => x.CustomerGroups, product.CustomerGroups)
                .Set(x => x.DeliveryDateId, product.DeliveryDateId)
                .Set(x => x.DisableBuyButton, product.DisableBuyButton)
                .Set(x => x.DisableWishlistButton, product.DisableWishlistButton)
                .Set(x => x.DisplayOrder, product.DisplayOrder)
                .Set(x => x.DisplayOrderCategory, product.DisplayOrderCategory)
                .Set(x => x.DisplayOrderBrand, product.DisplayOrderBrand)
                .Set(x => x.DisplayOrderCollection, product.DisplayOrderCollection)
                .Set(x => x.StockAvailability, product.StockAvailability)
                .Set(x => x.DisplayStockQuantity, product.DisplayStockQuantity)
                .Set(x => x.DownloadActivationTypeId, product.DownloadActivationTypeId)
                .Set(x => x.DownloadExpirationDays, product.DownloadExpirationDays)
                .Set(x => x.DownloadId, product.DownloadId)
                .Set(x => x.Flag, product.Flag)
                .Set(x => x.FullDescription, product.FullDescription)
                .Set(x => x.GiftVoucherTypeId, product.GiftVoucherTypeId)
                .Set(x => x.Gtin, product.Gtin)
                .Set(x => x.HasSampleDownload, product.HasSampleDownload)
                .Set(x => x.HasUserAgreement, product.HasUserAgreement)
                .Set(x => x.Height, product.Height)
                .Set(x => x.IncBothDate, product.IncBothDate)
                .Set(x => x.IsDownload, product.IsDownload)
                .Set(x => x.IsFreeShipping, product.IsFreeShipping)
                .Set(x => x.IsGiftVoucher, product.IsGiftVoucher)
                .Set(x => x.IsRecurring, product.IsRecurring)
                .Set(x => x.IsShipEnabled, product.IsShipEnabled)
                .Set(x => x.IsTaxExempt, product.IsTaxExempt)
                .Set(x => x.IsTele, product.IsTele)
                .Set(x => x.Length, product.Length)
                .Set(x => x.LimitedToStores, product.LimitedToStores)
                .Set(x => x.Locales, product.Locales)
                .Set(x => x.LowStockActivityId, product.LowStockActivityId)
                .Set(x => x.ManageInventoryMethodId, product.ManageInventoryMethodId)
                .Set(x => x.Mpn, product.Mpn)
                .Set(x => x.MarkAsNew, product.MarkAsNew)
                .Set(x => x.MarkAsNewStartDateTimeUtc, product.MarkAsNewStartDateTimeUtc)
                .Set(x => x.MarkAsNewEndDateTimeUtc, product.MarkAsNewEndDateTimeUtc)
                .Set(x => x.MaxEnteredPrice, product.MaxEnteredPrice)
                .Set(x => x.MaxNumberOfDownloads, product.MaxNumberOfDownloads)
                .Set(x => x.MetaDescription, product.MetaDescription)
                .Set(x => x.MetaKeywords, product.MetaKeywords)
                .Set(x => x.MetaTitle, product.MetaTitle)
                .Set(x => x.MinEnteredPrice, product.MinEnteredPrice)
                .Set(x => x.MinStockQuantity, product.MinStockQuantity)
                .Set(x => x.LowStock, ((product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity - product.ReservedQuantity) || product.StockQuantity - product.ReservedQuantity <= 0))
                .Set(x => x.Name, product.Name)
                .Set(x => x.NotApprovedRatingSum, product.NotApprovedRatingSum)
                .Set(x => x.NotApprovedTotalReviews, product.NotApprovedTotalReviews)
                .Set(x => x.NotifyAdminForQuantityBelow, product.NotifyAdminForQuantityBelow)
                .Set(x => x.NotReturnable, product.NotReturnable)
                .Set(x => x.OnSale, product.OnSale)
                .Set(x => x.OldPrice, product.OldPrice)
                .Set(x => x.OrderMaximumQuantity, product.OrderMaximumQuantity)
                .Set(x => x.OrderMinimumQuantity, product.OrderMinimumQuantity)
                .Set(x => x.OverGiftAmount, product.OverGiftAmount)
                .Set(x => x.ParentGroupedProductId, product.ParentGroupedProductId)
                .Set(x => x.PreOrderDateTimeUtc, product.PreOrderDateTimeUtc)
                .Set(x => x.Price, product.Price)
                .Set(x => x.ProductCost, product.ProductCost)
                .Set(x => x.ProductLayoutId, product.ProductLayoutId)
                .Set(x => x.ProductTypeId, product.ProductTypeId)
                .Set(x => x.Published, product.Published)
                .Set(x => x.RecurringCycleLength, product.RecurringCycleLength)
                .Set(x => x.RecurringCyclePeriodId, product.RecurringCyclePeriodId)
                .Set(x => x.RecurringTotalCycles, product.RecurringTotalCycles)
                .Set(x => x.RequiredProductIds, product.RequiredProductIds)
                .Set(x => x.RequireOtherProducts, product.RequireOtherProducts)
                .Set(x => x.SampleDownloadId, product.SampleDownloadId)
                .Set(x => x.SeName, product.SeName)
                .Set(x => x.ShipSeparately, product.ShipSeparately)
                .Set(x => x.ShortDescription, product.ShortDescription)
                .Set(x => x.ShowOnHomePage, product.ShowOnHomePage)
                .Set(x => x.BestSeller, product.BestSeller)
                .Set(x => x.Sku, product.Sku)
                .Set(x => x.StartPrice, product.StartPrice)
                .Set(x => x.StockQuantity, product.StockQuantity)
                .Set(x => x.ReservedQuantity, product.ReservedQuantity)
                .Set(x => x.Stores, product.Stores)
                .Set(x => x.LimitedToGroups, product.LimitedToGroups)
                .Set(x => x.TaxCategoryId, product.TaxCategoryId)
                .Set(x => x.UnitId, product.UnitId)
                .Set(x => x.UnlimitedDownloads, product.UnlimitedDownloads)
                .Set(x => x.UseMultipleWarehouses, product.UseMultipleWarehouses)
                .Set(x => x.UserAgreementText, product.UserAgreementText)
                .Set(x => x.BrandId, product.BrandId)
                .Set(x => x.VendorId, product.VendorId)
                .Set(x => x.VisibleIndividually, product.VisibleIndividually)
                .Set(x => x.WarehouseId, product.WarehouseId)
                .Set(x => x.Weight, product.Weight)
                .Set(x => x.Width, product.Width)
                .Set(x => x.UserFields, product.UserFields)
                .Set(x => x.UpdatedOnUtc, DateTime.UtcNow);

            await _productRepository.UpdateOneAsync(x => x.Id == product.Id, update);

            if (oldProduct.AdditionalShippingCharge != product.AdditionalShippingCharge ||
                oldProduct.IsFreeShipping != product.IsFreeShipping ||
                oldProduct.IsGiftVoucher != product.IsGiftVoucher ||
                oldProduct.IsShipEnabled != product.IsShipEnabled ||
                oldProduct.IsTaxExempt != product.IsTaxExempt ||
                oldProduct.IsRecurring != product.IsRecurring
                )
            {

                await _mediator.Publish(new UpdateProductOnCartEvent(product));
            }

            //raise event 
            if (!oldProduct.Published && product.Published)
                await _mediator.Publish(new ProductPublishEvent(product));

            if (oldProduct.Published && !product.Published)
                await _mediator.Publish(new ProductUnPublishEvent(product));

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_PATTERN);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_GROUP_PATTERN);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_HOMEPAGE_PATTERN);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_CUSTOMER_TAG_PATTERN);

            //event notification
            await _mediator.EntityUpdated(product);
        }
        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="product">Product</param>
        public virtual async Task DeleteProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            //deleted product
            await _productRepository.DeleteAsync(product);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(product);
        }

        public virtual async Task UpdateMostView(Product product)
        {
            await _productRepository.UpdateField(product.Id, x => x.Viewed, product.Viewed + 1);
        }

        public virtual async Task UpdateSold(Product product, int qty)
        {
            await _productRepository.UpdateField(product.Id, x => x.Sold, product.Sold + qty);
        }

        public virtual async Task UnpublishProduct(Product product)
        {
            var update = UpdateBuilder<Product>.Create()
                    .Set(x => x.Published, false)
                    .Set(x => x.UpdatedOnUtc, DateTime.UtcNow);

            await _productRepository.UpdateOneAsync(x => x.Id == product.Id, update);

            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event
            await _mediator.Publish(new ProductUnPublishEvent(product));

            //event notification
            await _mediator.EntityUpdated(product);
        }

        /// <summary>
        /// Get (visible) product number in certain category
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="categoryIds">Category identifiers</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="ignoreAcl">Ignore acl</param>
        /// <param name="ignoreStore">Ignore store</param>
        /// <returns>Product number</returns>
        public virtual int GetCategoryProductNumber(Customer customer, IList<string> categoryIds = null, string storeId = "", bool ignoreAcl = true, bool ignoreStore = true)
        {
            //validate "categoryIds" parameter
            if (categoryIds != null && categoryIds.Contains(""))
                categoryIds.Remove("");

            var query = from p in _productRepository.Table
                        select p;

            query = query.Where(p => p.Published && p.VisibleIndividually);

            ////category filtering
            if (categoryIds != null && categoryIds.Any())
            {
                query = query.Where(p => p.ProductCategories.Any(x => categoryIds.Contains(x.CategoryId)));
            }

            if (!ignoreAcl)
            {
                //ACL (access control list)
                var allowedCustomerGroupsIds = customer.GetCustomerGroupIds();
                query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
            }

            if (!string.IsNullOrEmpty(storeId) && !ignoreStore)
            {
                //Limited to stores rules
                query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        select p;
            }

            return Convert.ToInt32(query.Count());

        }

        /// <summary>
        /// Search products
        /// </summary>
        /// <param name="filterableSpecificationAttributeOptionIds">The specification attribute option identifiers applied to loaded products (all pages)</param>
        /// <param name="loadFilterableSpecificationAttributeOptionIds">A value indicating whether we should load the specification attribute option identifiers applied to loaded products (all pages)</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="categoryIds">Category identifiers</param>
        /// <param name="brandId">Brand ident</param>
        /// <param name="collectionId">Collection identifier; "" to load all records</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <param name="warehouseId">Warehouse identifier; "" to load all records</param>
        /// <param name="productType">Product type; "" to load all records</param>
        /// <param name="visibleIndividuallyOnly">A values indicating whether to load only products marked as "visible individually"; "false" to load all records; "true" to load "visible individually" only</param>
        /// <param name="featuredProducts">A value indicating whether loaded products are marked as featured (relates only to categories and collections). 0 to load featured products only, 1 to load not featured products only, null to load all products</param>
        /// <param name="priceMin">Minimum price; null to load all records</param>
        /// <param name="priceMax">Maximum price; null to load all records</param>
        /// <param name="productTag">Product tag name; "" to load all records</param>
        /// <param name="keywords">Keywords</param>
        /// <param name="searchDescriptions">A value indicating whether to search by a specified "keyword" in product descriptions</param>
        /// <param name="searchSku">A value indicating whether to search by a specified "keyword" in product SKU</param>
        /// <param name="searchProductTags">A value indicating whether to search by a specified "keyword" in product tags</param>
        /// <param name="languageId">Language identifier (search for text searching)</param>
        /// <param name="filteredSpecs">Filtered product specification identifiers</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="overridePublished">
        /// null - process "Published" property according to "showHidden" parameter
        /// true - load only "Published" products
        /// false - load only "Unpublished" products
        /// </param>
        /// <returns>Products</returns>
        public virtual async Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)> SearchProducts(
            bool loadFilterableSpecificationAttributeOptionIds = false,
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            IList<string> categoryIds = null,
            string brandId = "",
            string collectionId = "",
            string storeId = "",
            string vendorId = "",
            string warehouseId = "",
            ProductType? productType = null,
            bool visibleIndividuallyOnly = false,
            bool markedAsNewOnly = false,
            bool? showOnHomePage = null,
            bool? featuredProducts = null,
            double? priceMin = null,
            double? priceMax = null,
            string productTag = "",
            string keywords = null,
            bool searchDescriptions = false,
            bool searchSku = true,
            bool searchProductTags = false,
            string languageId = "",
            IList<string> filteredSpecs = null,
            ProductSortingEnum orderBy = ProductSortingEnum.Position,
            bool showHidden = false,
            bool? overridePublished = null)
        {

            var model = await _mediator.Send(new GetSearchProductsQuery() {
                Customer = _workContext.CurrentCustomer,
                LoadFilterableSpecificationAttributeOptionIds = loadFilterableSpecificationAttributeOptionIds,
                PageIndex = pageIndex,
                PageSize = pageSize,
                CategoryIds = categoryIds,
                BrandId = brandId,
                CollectionId = collectionId,
                StoreId = storeId,
                VendorId = vendorId,
                WarehouseId = warehouseId,
                ProductType = productType,
                VisibleIndividuallyOnly = visibleIndividuallyOnly,
                MarkedAsNewOnly = markedAsNewOnly,
                ShowOnHomePage = showOnHomePage,
                FeaturedProducts = featuredProducts,
                PriceMin = priceMin,
                PriceMax = priceMax,
                ProductTag = productTag,
                Keywords = keywords,
                SearchDescriptions = searchDescriptions,
                SearchSku = searchSku,
                SearchProductTags = searchProductTags,
                LanguageId = languageId,
                FilteredSpecs = filteredSpecs,
                OrderBy = orderBy,
                ShowHidden = showHidden,
                OverridePublished = overridePublished
            });

            return model;
        }

        /// <summary>
        /// Gets products by product attribute
        /// </summary>
        /// <param name="productAttributeId">Product attribute identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Products</returns>
        public virtual async Task<IPagedList<Product>> GetProductsByProductAtributeId(string productAttributeId,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _productRepository.Table
                        select p;
            query = query.Where(x => x.ProductAttributeMappings.Any(y => y.ProductAttributeId == productAttributeId));
            query = query.OrderBy(x => x.Name);

            return await PagedList<Product>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets associated products
        /// </summary>
        /// <param name="parentGroupedProductId">Parent product identifier (used with grouped products)</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <returns>Products</returns>
        public virtual async Task<IList<Product>> GetAssociatedProducts(string parentGroupedProductId,
            string storeId = "", string vendorId = "", bool showHidden = false)
        {

            var query = from p in _productRepository.Table
                        select p;

            query = query.Where(p => p.ParentGroupedProductId == parentGroupedProductId);

            if (!showHidden)
            {
                query = query.Where(p => p.Published);
            }
            if (!showHidden)
            {
                var nowUtc = DateTime.UtcNow;
                //available dates
                query = query.Where(p =>
                    (p.AvailableStartDateTimeUtc == null || p.AvailableStartDateTimeUtc < nowUtc) &&
                    (p.AvailableEndDateTimeUtc == null || p.AvailableEndDateTimeUtc > nowUtc));

            }
            //vendor filtering
            if (!string.IsNullOrEmpty(vendorId))
            {
                query = query.Where(p => p.VendorId == vendorId);
            }

            var products = query.OrderBy(x => x.DisplayOrder).ToList();

            //ACL mapping
            if (!showHidden)
            {
                products = products.Where(x => _aclService.Authorize(x, _workContext.CurrentCustomer)).ToList();
            }
            //Store acl
            if (!showHidden && !string.IsNullOrEmpty(storeId))
            {
                products = products.Where(x => _aclService.Authorize(x, storeId)).ToList();
            }

            return await Task.FromResult(products);
        }

        /// <summary>
        /// Gets a product by SKU
        /// </summary>
        /// <param name="sku">SKU</param>
        /// <returns>Product</returns>
        public virtual async Task<Product> GetProductBySku(string sku)
        {
            if (string.IsNullOrEmpty(sku))
                return null;

            sku = sku.Trim();
            return await Task.FromResult(_productRepository.Table.Where(x => x.Sku == sku).FirstOrDefault());            
        }

        public virtual async Task UpdateAssociatedProduct(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var update = UpdateBuilder<Product>.Create()
                .Set(x => x.DisplayOrder, product.DisplayOrder)
                .Set(x => x.ParentGroupedProductId, product.ParentGroupedProductId);

            await _productRepository.UpdateManyAsync(x => x.Id == product.Id, update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);

        }

        #endregion

        #region Related products

        /// <summary>
        /// Insert a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>

        public virtual async Task InsertRelatedProduct(RelatedProduct relatedProduct, string productId)
        {
            if (relatedProduct == null)
                throw new ArgumentNullException(nameof(relatedProduct));

            await _productRepository.AddToSet(productId, x => x.RelatedProducts, relatedProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(relatedProduct);
        }
        /// <summary>
        /// Deletes a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteRelatedProduct(RelatedProduct relatedProduct, string productId)
        {
            if (relatedProduct == null)
                throw new ArgumentNullException(nameof(relatedProduct));

            await _productRepository.PullFilter(productId, x => x.RelatedProducts, z => z.Id, relatedProduct.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(relatedProduct);
        }

        /// <summary>
        /// Updates a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateRelatedProduct(RelatedProduct relatedProduct, string productId)
        {
            if (relatedProduct == null)
                throw new ArgumentNullException(nameof(relatedProduct));

            await _productRepository.UpdateToSet(productId, x => x.RelatedProducts, z => z.Id, relatedProduct.Id, relatedProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(relatedProduct);
        }

        #endregion

        #region Similar products

        public virtual async Task InsertSimilarProduct(SimilarProduct similarProduct)
        {
            if (similarProduct == null)
                throw new ArgumentNullException(nameof(similarProduct));

            await _productRepository.AddToSet(similarProduct.ProductId1, x => x.SimilarProducts, similarProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, similarProduct.ProductId1));

            //event notification
            await _mediator.EntityInserted(similarProduct);
        }

        /// <summary>
        /// Updates a similar product
        /// </summary>
        /// <param name="similarProduct">Similar product</param>
        public virtual async Task UpdateSimilarProduct(SimilarProduct similarProduct)
        {
            if (similarProduct == null)
                throw new ArgumentNullException(nameof(similarProduct));

            await _productRepository.UpdateToSet(similarProduct.ProductId1, x => x.SimilarProducts, z => z.Id, similarProduct.Id, similarProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, similarProduct.ProductId1));

            //event notification
            await _mediator.EntityUpdated(similarProduct);
        }
        /// <summary>
        /// Deletes a similar product
        /// </summary>
        /// <param name="similarProduct">Similar product</param>
        public virtual async Task DeleteSimilarProduct(SimilarProduct similarProduct)
        {
            if (similarProduct == null)
                throw new ArgumentNullException(nameof(similarProduct));

            await _productRepository.PullFilter(similarProduct.ProductId1, x => x.SimilarProducts, z => z.Id, similarProduct.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, similarProduct.ProductId1));

            //event notification
            await _mediator.EntityDeleted(similarProduct);
        }

        #endregion

        #region Bundle product

        /// <summary>
        /// Inserts a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        public virtual async Task InsertBundleProduct(BundleProduct bundleProduct, string productBundleId)
        {
            if (bundleProduct == null)
                throw new ArgumentNullException(nameof(bundleProduct));

            await _productRepository.AddToSet(productBundleId, x => x.BundleProducts, bundleProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productBundleId));

            //event notification
            await _mediator.EntityInserted(bundleProduct);

        }

        /// <summary>
        /// Updates a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        public virtual async Task UpdateBundleProduct(BundleProduct bundleProduct, string productBundleId)
        {
            if (bundleProduct == null)
                throw new ArgumentNullException(nameof(bundleProduct));

            await _productRepository.UpdateToSet(productBundleId, x => x.BundleProducts, z => z.Id, bundleProduct.Id, bundleProduct);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productBundleId));

            //event notification
            await _mediator.EntityUpdated(bundleProduct);

        }
        /// <summary>
        /// Deletes a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        public virtual async Task DeleteBundleProduct(BundleProduct bundleProduct, string productBundleId)
        {
            if (bundleProduct == null)
                throw new ArgumentNullException(nameof(bundleProduct));

            await _productRepository.PullFilter(productBundleId, x => x.BundleProducts, z => z.Id, bundleProduct.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productBundleId));

            //event notification
            await _mediator.EntityDeleted(bundleProduct);
        }

        #endregion

        #region Cross-sell products

        /// <summary>
        /// Inserts a cross-sell product
        /// </summary>
        /// <param name="crossSellProduct">Cross-sell product</param>
        public virtual async Task InsertCrossSellProduct(CrossSellProduct crossSellProduct)
        {
            if (crossSellProduct == null)
                throw new ArgumentNullException(nameof(crossSellProduct));

            await _productRepository.AddToSet(crossSellProduct.ProductId1, x => x.CrossSellProduct, crossSellProduct.ProductId2);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, crossSellProduct.ProductId1));

            //event notification
            await _mediator.EntityInserted(crossSellProduct);
        }

        /// <summary>
        /// Deletes a cross-sell product
        /// </summary>
        /// <param name="crossSellProduct">Cross-sell identifier</param>
        public virtual async Task DeleteCrossSellProduct(CrossSellProduct crossSellProduct)
        {
            if (crossSellProduct == null)
                throw new ArgumentNullException(nameof(crossSellProduct));

            await _productRepository.Pull(crossSellProduct.ProductId1, x => x.CrossSellProduct, crossSellProduct.ProductId2);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, crossSellProduct.ProductId1));

            //event notification
            await _mediator.EntityDeleted(crossSellProduct);
        }

        /// <summary>
        /// Gets a cross-sells
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="numberOfProducts">Number of products to return</param>
        /// <returns>Cross-sells</returns>
        public virtual async Task<IList<Product>> GetCrossSellProductsByShoppingCart(IList<ShoppingCartItem> cart, int numberOfProducts)
        {
            var result = new List<Product>();

            if (numberOfProducts == 0)
                return result;

            if (cart == null || !cart.Any())
                return result;

            var cartProductIds = new List<string>();
            foreach (var sci in cart)
            {
                string prodId = sci.ProductId;
                if (!cartProductIds.Contains(prodId))
                    cartProductIds.Add(prodId);
            }

            foreach (var sci in cart)
            {
                var product = await GetProductById(sci.ProductId);
                if (product == null)
                    continue;

                var crossSells = product.CrossSellProduct;
                foreach (var crossSell in crossSells)
                {
                    //validate that this product is not added to result yet
                    //validate that this product is not in the cart
                    if (result.Find(p => p.Id == crossSell) == null &&
                        !cartProductIds.Contains(crossSell))
                    {
                        var productToAdd = await GetProductById(crossSell);
                        //validate product
                        if (productToAdd == null || !productToAdd.Published
                             || !_aclService.Authorize(productToAdd, _workContext.CurrentCustomer) || !_aclService.Authorize(productToAdd, _workContext.CurrentStore.Id)
                             || !productToAdd.IsAvailable())
                            continue;

                        //add a product to result
                        result.Add(productToAdd);
                        if (result.Count >= numberOfProducts)
                            return result;
                    }
                }
            }
            return result;
        }

        #endregion

        #region Recommmended products

        /// <summary>
        /// Inserts a recommended product
        /// </summary>
        /// <param name="recommendedProduct">Recommended product</param>
        public virtual async Task InsertRecommendedProduct(string productId, string recommendedProductId)
        {
            if (productId == null)
                throw new ArgumentNullException(nameof(productId));

            if (recommendedProductId == null)
                throw new ArgumentNullException(nameof(recommendedProductId));

            await _productRepository.AddToSet(productId, x => x.RecommendedProduct, recommendedProductId);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
            
        }

        /// <summary>
        /// Deletes a recommended product
        /// </summary>
        /// <param name="recommendedProduct">Recommended identifier</param>
        public virtual async Task DeleteRecommendedProduct(string productId, string recommendedProductId)
        {
            if (productId == null)
                throw new ArgumentNullException(nameof(productId));

            if (recommendedProductId == null)
                throw new ArgumentNullException(nameof(recommendedProductId));

            await _productRepository.Pull(productId, x => x.RecommendedProduct, recommendedProductId);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
        }

        #endregion

        #region Tier prices


        /// <summary>
        /// Inserts a tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertTierPrice(TierPrice tierPrice, string productId)
        {
            if (tierPrice == null)
                throw new ArgumentNullException(nameof(tierPrice));

            await _productRepository.AddToSet(productId, x => x.TierPrices, tierPrice);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(tierPrice);
        }

        /// <summary>
        /// Updates the tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateTierPrice(TierPrice tierPrice, string productId)
        {
            if (tierPrice == null)
                throw new ArgumentNullException(nameof(tierPrice));

            await _productRepository.UpdateToSet(productId, x => x.TierPrices, z => z.Id, tierPrice.Id, tierPrice);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(tierPrice);
        }
        /// <summary>
        /// Deletes a tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteTierPrice(TierPrice tierPrice, string productId)
        {
            if (tierPrice == null)
                throw new ArgumentNullException(nameof(tierPrice));

            await _productRepository.PullFilter(productId, x => x.TierPrices, x => x.Id, tierPrice.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(tierPrice);
        }

        #endregion

        #region Product prices

        /// <summary>
        /// Inserts a product price
        /// </summary>
        /// <param name="productPrice">Product price</param>
        public virtual async Task InsertProductPrice(ProductPrice productPrice)
        {
            if (productPrice == null)
                throw new ArgumentNullException(nameof(productPrice));

            await _productRepository.AddToSet(productPrice.ProductId, x => x.ProductPrices, productPrice);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productPrice.ProductId));

            //event notification
            await _mediator.EntityInserted(productPrice);
        }

        /// <summary>
        /// Updates the product price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        public virtual async Task UpdateProductPrice(ProductPrice productPrice)
        {
            if (productPrice == null)
                throw new ArgumentNullException(nameof(productPrice));

            await _productRepository.UpdateToSet(productPrice.ProductId, x => x.ProductPrices, z => z.Id, productPrice.Id, productPrice);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productPrice.ProductId));

            //event notification
            await _mediator.EntityUpdated(productPrice);
        }

        /// <summary>
        /// Deletes a product price
        /// </summary>
        /// <param name="productPrice">Product price</param>
        public virtual async Task DeleteProductPrice(ProductPrice productPrice)
        {
            if (productPrice == null)
                throw new ArgumentNullException(nameof(productPrice));

            await _productRepository.PullFilter(productPrice.ProductId, x => x.ProductPrices, x => x.Id, productPrice.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productPrice.ProductId));

            //event notification
            await _mediator.EntityDeleted(productPrice);
        }

        #endregion

        #region Product pictures       
        /// <summary>
        /// Inserts a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductPicture(ProductPicture productPicture, string productId)
        {
            if (productPicture == null)
                throw new ArgumentNullException(nameof(productPicture));

            await _productRepository.AddToSet(productId, x => x.ProductPictures, productPicture);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productPicture);
        }

        /// <summary>
        /// Updates a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateProductPicture(ProductPicture productPicture, string productId)
        {
            if (productPicture == null)
                throw new ArgumentNullException(nameof(productPicture));

            await _productRepository.UpdateToSet(productId, x => x.ProductPictures, z => z.Id, productPicture.Id, productPicture);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productPicture);
        }
        /// <summary>
        /// Deletes a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductPicture(ProductPicture productPicture, string productId)
        {
            if (productPicture == null)
                throw new ArgumentNullException(nameof(productPicture));

            await _productRepository.PullFilter(productId, x => x.ProductPictures, x => x.Id, productPicture.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productPicture);
        }
        #endregion

        #region Product warehouse inventory        
        /// <summary>
        /// Insert product warehouse inwentory
        /// </summary>
        /// <param name="pwi"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public virtual async Task InsertProductWarehouseInventory(ProductWarehouseInventory pwi, string productId)
        {
            if (pwi == null)
                throw new ArgumentNullException(nameof(pwi));

            await _productRepository.AddToSet(productId, x => x.ProductWarehouseInventory, pwi);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(pwi);
        }
        /// <summary>
        /// Update product warehouse inventory
        /// </summary>
        /// <param name="pwi"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public virtual async Task UpdateProductWarehouseInventory(ProductWarehouseInventory pwi, string productId)
        {
            if (pwi == null)
                throw new ArgumentNullException(nameof(pwi));

            await _productRepository.UpdateToSet(productId, x => x.ProductWarehouseInventory, z => z.Id, pwi.Id, pwi);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
            //event notification
            await _mediator.EntityUpdated(pwi);
        }
        /// <summary>
        /// Deletes a ProductWarehouseInventory
        /// </summary>
        /// <param name="pwi">ProductWarehouseInventory</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductWarehouseInventory(ProductWarehouseInventory pwi, string productId)
        {
            if (pwi == null)
                throw new ArgumentNullException(nameof(pwi));

            await _productRepository.PullFilter(productId, x => x.ProductWarehouseInventory, x => x.Id, pwi.Id);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

        }
        #endregion

        #region Discount

        public virtual async Task DeleteDiscount(string discountId, string productId)
        {
            if (string.IsNullOrEmpty(discountId))
                throw new ArgumentNullException(nameof(discountId));

            await _productRepository.Pull(productId, x => x.AppliedDiscounts, discountId);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
        }

        public virtual async Task InsertDiscount(string discountId, string productId)
        {
            if (string.IsNullOrEmpty(discountId))
                throw new ArgumentNullException(nameof(discountId));

            await _productRepository.AddToSet(productId, x => x.AppliedDiscounts, discountId);
            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
        }

        #endregion

        #endregion
    }
}
