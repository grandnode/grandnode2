using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Products
{
    /// <summary>
    /// Product service
    /// </summary>
    public partial interface IProductService
    {
        #region Products

        /// <summary>
        /// Gets all products displayed on the home page
        /// </summary>
        /// <returns>Products</returns>
        Task<IList<string>> GetAllProductsDisplayedOnHomePage();

        /// <summary>
        /// Gets all products displayed on the best seller
        /// </summary>
        /// <returns>Products</returns>
        Task<IList<string>> GetAllProductsDisplayedOnBestSeller();

        /// <summary>
        /// Gets product
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="fromDB">get data from db (not from cache)</param>
        /// <returns>Product</returns>
        Task<Product> GetProductById(string productId, bool fromDB = false);

        /// <summary>
        /// Gets product from product or product deleted
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>Product</returns>
        Task<Product> GetProductByIdIncludeArch(string productId);

        /// <summary>
        /// Gets products by identifier
        /// </summary>
        /// <param name="productIds">Product identifiers</param>
        /// <param name="showHidden">Show hidden</param>
        /// <returns>Products</returns>
        Task<IList<Product>> GetProductsByIds(string[] productIds, bool showHidden = false);

        /// <summary>
        /// Gets products by discount
        /// </summary>
        /// <param name="discountId">Product identifiers</param>
        /// <returns>Products</returns>
        Task<IPagedList<Product>> GetProductsByDiscount(string discountId, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Inserts a product
        /// </summary>
        /// <param name="product">Product</param>
        Task InsertProduct(Product product);

        /// <summary>
        /// Updates the product
        /// </summary>
        /// <param name="product">Product</param>
        Task UpdateProduct(Product product);

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="product">Product</param>
        Task DeleteProduct(Product product);

        /// <summary>
        /// Updates most view on the product
        /// </summary>
        /// <param name="product">Product</param>
        Task UpdateMostView(Product product);

        /// <summary>
        /// Updates best sellers on the product
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="qty">Count</param>
        Task UpdateSold(Product product, int qty);

        /// <summary>
        /// Set product as unpublished
        /// </summary>
        /// <param name="product"></param>
        Task UnpublishProduct(Product product);

        /// <summary>
        /// Get (visible) product number in certain category
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="categoryIds">Category identifiers</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="ignoreAcl">Ignore acl</param>
        /// <param name="ignoreStore">Ignore store</param>
        /// <returns>Product number</returns>
        int GetCategoryProductNumber(Customer customer, IList<string> categoryIds = null, string storeId = "", bool ignoreAcl = true, bool ignoreStore = true);

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
        /// <param name="showOnHomePage">A value indicating whether loaded products show on homepage</param>
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
        Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)> SearchProducts(
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
            bool? overridePublished = null);

        /// <summary>
        /// Gets products by product attribute
        /// </summary>
        /// <param name="productAttributeId">Product attribute identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Products</returns>
        Task<IPagedList<Product>> GetProductsByProductAtributeId(string productAttributeId,
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets associated products
        /// </summary>
        /// <param name="parentGroupedProductId">Parent product identifier (used with grouped products)</param>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="vendorId">Vendor identifier; "" to load all records</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Products</returns>
        Task<IList<Product>> GetAssociatedProducts(string parentGroupedProductId,
            string storeId = "", string vendorId = "", bool showHidden = false);

        /// <summary>
        /// Update product associated 
        /// </summary>
        /// <param name="product">Product</param>
        Task UpdateAssociatedProduct(Product product);

        /// <summary>
        /// Gets a product by SKU
        /// </summary>
        /// <param name="sku">SKU</param>
        /// <returns>Product</returns>
        Task<Product> GetProductBySku(string sku);

        #endregion

        #region Related products

        /// <summary>
        /// Inserts a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>
        Task InsertRelatedProduct(RelatedProduct relatedProduct, string productId);

        /// <summary>
        /// Updates a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>
        Task UpdateRelatedProduct(RelatedProduct relatedProduct, string productId);

        /// <summary>
        /// Deletes a related product
        /// </summary>
        /// <param name="relatedProduct">Related product</param>
        /// <param name="productId">Product ident</param>
        Task DeleteRelatedProduct(RelatedProduct relatedProduct, string productId);

        #endregion

        #region Similar products

        /// <summary>
        /// Inserts a similar product
        /// </summary>
        /// <param name="similarProduct">Similar product</param>
        Task InsertSimilarProduct(SimilarProduct similarProduct);

        /// <summary>
        /// Updates a similar product
        /// </summary>
        /// <param name="similarProduct">Similar product</param>
        Task UpdateSimilarProduct(SimilarProduct similarProduct);

        /// <summary>
        /// Deletes a similar product
        /// </summary>
        /// <param name="similarProduct">Similar product</param>
        Task DeleteSimilarProduct(SimilarProduct similarProduct);

        #endregion

        #region Cross-sell products

        /// <summary>
        /// Inserts a cross-sell product
        /// </summary>
        /// <param name="crossSellProduct">Cross-sell product</param>
        Task InsertCrossSellProduct(CrossSellProduct crossSellProduct);

        /// <summary>
        /// Deletes a cross-sell product
        /// </summary>
        /// <param name="crossSellProduct">Cross-sell</param>
        Task DeleteCrossSellProduct(CrossSellProduct crossSellProduct);

        /// <summary>
        /// Gets a cross-sells
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="numberOfProducts">Number of products to return</param>
        /// <returns>Cross-sells</returns>
        Task<IList<Product>> GetCrossSellProductsByShoppingCart(IList<ShoppingCartItem> cart, int numberOfProducts);

        #endregion

        #region Recommmended products

        /// <summary>
        /// Inserts a recommended product
        /// </summary>
        /// <param name="recommendedProduct">Recommended product</param>
        Task InsertRecommendedProduct(string productId, string recommendedProductId);

        /// <summary>
        /// Deletes a recommended product
        /// </summary>
        /// <param name="recommendedProduct">Recommended identifier</param>
        Task DeleteRecommendedProduct(string productId, string recommendedProductId);

        #endregion

        #region Bundle products

        /// <summary>
        /// Inserts a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        Task InsertBundleProduct(BundleProduct bundleProduct, string productBundleId);

        /// <summary>
        /// Updates a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        Task UpdateBundleProduct(BundleProduct bundleProduct, string productBundleId);

        /// <summary>
        /// Deletes a bundle product
        /// </summary>
        /// <param name="bundleProduct">Bundle product</param>
        /// <param name="productBundleId">Product bundle ident</param>
        Task DeleteBundleProduct(BundleProduct bundleProduct, string productBundleId);
        #endregion

        #region Tier prices

        /// <summary>
        /// Inserts a tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        Task InsertTierPrice(TierPrice tierPrice, string productId);

        /// <summary>
        /// Updates the tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        Task UpdateTierPrice(TierPrice tierPrice, string productId);

        /// <summary>
        /// Deletes a tier price
        /// </summary>
        /// <param name="tierPrice">Tier price</param>
        /// <param name="productId">Product ident</param>
        Task DeleteTierPrice(TierPrice tierPrice, string productId);


        #endregion

        #region Product prices

        /// <summary>
        /// Inserts a product price
        /// </summary>
        /// <param name="productPrice">Product price</param>
        Task InsertProductPrice(ProductPrice productPrice);

        /// <summary>
        /// Updates the product price
        /// </summary>
        /// <param name="productPrice">Product price</param>
        Task UpdateProductPrice(ProductPrice productPrice);

        /// <summary>
        /// Deletes a product price
        /// </summary>
        /// <param name="productPrice">Product price</param>
        Task DeleteProductPrice(ProductPrice productPrice);

        #endregion

        #region Product pictures

        /// <summary>
        /// Inserts a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductPicture(ProductPicture productPicture, string productId);

        /// <summary>
        /// Updates a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        Task UpdateProductPicture(ProductPicture productPicture, string productId);

        /// <summary>
        /// Deletes a product picture
        /// </summary>
        /// <param name="productPicture">Product picture</param>
        /// <param name="productId">Product ident</param>
        Task DeleteProductPicture(ProductPicture productPicture, string productId);

        #endregion

        #region Product warehouse inventory

       
        /// <summary>
        /// Insert a ProductWarehouseInventory
        /// </summary>
        /// <param name="pwi">ProductWarehouseInventory</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductWarehouseInventory(ProductWarehouseInventory pwi, string productId);

        /// <summary>
        /// Update a ProductWarehouseInventory
        /// </summary>
        /// <param name="pwi">ProductWarehouseInventory</param>
        /// <param name="productId">Product ident</param>
        Task UpdateProductWarehouseInventory(ProductWarehouseInventory pwi, string productId);

        /// <summary>
        /// Deletes a ProductWarehouseInventory
        /// </summary>
        /// <param name="pwi">ProductWarehouseInventory</param>
        /// <param name="productId">Product ident</param>
        Task DeleteProductWarehouseInventory(ProductWarehouseInventory pwi, string productId);


        #endregion

        #region Discounts

        Task InsertDiscount(string discountId, string productId);
        Task DeleteDiscount(string discountId, string productId);

        #endregion
    }
}
