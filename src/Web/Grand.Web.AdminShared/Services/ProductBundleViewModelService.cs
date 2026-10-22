using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductBundleViewModelService(
    IProductService productService,
    IAdminDataScope<Product> scope)
    : IProductBundleViewModelService
{
    public virtual async Task InsertBundleProductModel(ProductModel.AddBundleProductModel model)
    {
        var productId1 = await productService.GetProductById(model.ProductId, true);

        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            // scope.HasAccess: see InsertRelatedProductModel above.
            if (product == null || !await scope.HasAccess(product)) continue;

            var existingBundleProducts = productId1.BundleProducts;
            if (model.ProductId != id)
                if (!existingBundleProducts.Any(x => x.ProductId == id))
                {
                    var bundle = new BundleProduct {
                        ProductId = id,
                        DisplayOrder = 1,
                        Quantity = 1
                    };
                    productId1.BundleProducts.Add(bundle);
                    await productService.InsertBundleProduct(bundle, model.ProductId);
                }
        }
    }

    public virtual async Task UpdateBundleProductModel(ProductModel.BundleProductModel model)
    {
        var product = await productService.GetProductById(model.ProductBundleId, true);
        var bundleProduct = product.BundleProducts.FirstOrDefault(x => x.Id == model.Id);
        if (bundleProduct == null)
            throw new ArgumentException("No bundle product found with the specified id");

        var product2 = await productService.GetProductById(bundleProduct.ProductId);
        if (product2 == null)
            throw new ArgumentException("No product found with the specified id");

        bundleProduct.ProductId = model.ProductId;
        bundleProduct.Quantity = model.Quantity > 0 ? model.Quantity : 1;
        bundleProduct.DisplayOrder = model.DisplayOrder;
        await productService.UpdateBundleProduct(bundleProduct, model.ProductBundleId);
    }

    public virtual async Task DeleteBundleProductModel(ProductModel.BundleProductModel model)
    {
        var product = await productService.GetProductById(model.ProductBundleId, true);
        var bundleProduct = product.BundleProducts.FirstOrDefault(x => x.Id == model.Id);
        if (bundleProduct == null)
            throw new ArgumentException("No bundle product found with the specified id");

        await productService.DeleteBundleProduct(bundleProduct, model.ProductBundleId);
    }
}
