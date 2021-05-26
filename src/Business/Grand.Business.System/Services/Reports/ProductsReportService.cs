using Grand.Business.System.Interfaces.Reports;
using Grand.Business.System.Utilities;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Business.System.Services.Reports
{
    /// <summary>
    /// Product report service 
    /// </summary>
    public class ProductsReportService : IProductsReportService
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;

        #endregion

        #region CTOR

        public ProductsReportService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        #endregion

        /// <summary>
        /// Get "low stock products" report
        /// </summary>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Result</returns>
        public virtual async Task<(IList<Product> products, IList<ProductsAttributeCombination> combinations)> LowStockProducts(string vendorId, string storeId)
        {
            //Track inventory for product
            //simple products
            var query_simple_products = from p in _productRepository.Table
                                        where p.LowStock &&
                                        ((p.ProductTypeId == ProductType.SimpleProduct || p.ProductTypeId == ProductType.BundledProduct)
                                        && p.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                                        select p;

            if (!string.IsNullOrEmpty(vendorId))
                query_simple_products = query_simple_products.Where(x => x.VendorId == vendorId);

            if (!string.IsNullOrEmpty(storeId))
                query_simple_products = query_simple_products.Where(x => x.Stores.Contains(storeId));

            var products = query_simple_products.ToList();

            //Track inventory for product by product attributes
            var query2_1 = from p in _productRepository.Table
                           where
                           p.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes &&
                           (vendorId == "" || p.VendorId == vendorId) &&
                           (storeId == "" || p.Stores.Contains(storeId))
                           from c in p.ProductAttributeCombinations
                           select new ProductsAttributeCombination()
                           {
                               ProductId = p.Id,
                               StockQuantity = c.StockQuantity,
                               Attributes = c.Attributes,
                               AllowOutOfStockOrders = c.AllowOutOfStockOrders,
                               Id = c.Id,
                               Gtin = c.Gtin,
                               Mpn = c.Mpn,
                               NotifyAdminForQuantityBelow = c.NotifyAdminForQuantityBelow,
                               OverriddenPrice = c.OverriddenPrice,
                               Sku = c.Sku
                           };

            var query2_2 = from c in query2_1
                           where c.StockQuantity <= 0
                           select c;

            var combinations = query2_2.ToList();

            return await Task.FromResult((products, combinations));
        }
    }
}
