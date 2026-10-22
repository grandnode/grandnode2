using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductRelatedViewModelService(
    IProductService productService,
    IAdminDataScope<Product> scope)
    : IProductRelatedViewModelService
{
    public virtual async Task InsertRelatedProductModel(ProductModel.AddRelatedProductModel model)
    {
        var productId1 = await productService.GetProductById(model.ProductId, true);

        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            if (product == null || !await scope.HasAccess(product)) continue;

            var existingRelatedProducts = productId1.RelatedProducts;
            if (model.ProductId != id)
                if (!existingRelatedProducts.Any(x => x.ProductId2 == id))
                {
                    var related = new RelatedProduct {
                        ProductId2 = id,
                        DisplayOrder = 1
                    };
                    productId1.RelatedProducts.Add(related);
                    await productService.InsertRelatedProduct(related, model.ProductId);
                }
        }
    }

    public virtual async Task UpdateRelatedProductModel(ProductModel.RelatedProductModel model)
    {
        var product1 = await productService.GetProductById(model.ProductId1, true);
        var relatedProduct = product1.RelatedProducts.FirstOrDefault(x => x.Id == model.Id);
        if (relatedProduct == null)
            throw new ArgumentException("No related product found with the specified id");

        var product2 = await productService.GetProductById(relatedProduct.ProductId2);
        if (product2 == null)
            throw new ArgumentException("No product found with the specified id");

        relatedProduct.DisplayOrder = model.DisplayOrder;
        await productService.UpdateRelatedProduct(relatedProduct, model.ProductId1);
    }

    public virtual async Task DeleteRelatedProductModel(ProductModel.RelatedProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1, true);
        var relatedProduct = product.RelatedProducts.FirstOrDefault(x => x.Id == model.Id);
        if (relatedProduct == null)
            throw new ArgumentException("No related product found with the specified id");

        await productService.DeleteRelatedProduct(relatedProduct, model.ProductId1);
    }

    public virtual async Task InsertSimilarProductModel(ProductModel.AddSimilarProductModel model)
    {
        var productId1 = await productService.GetProductById(model.ProductId, true);

        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            // scope.HasAccess: see InsertRelatedProductModel above.
            if (product == null || !await scope.HasAccess(product)) continue;

            var existingSimilarProducts = productId1.SimilarProducts;
            if (model.ProductId != id)
                if (!existingSimilarProducts.Any(x => x.ProductId2 == id))
                {
                    var similar = new SimilarProduct {
                        ProductId1 = model.ProductId,
                        ProductId2 = id,
                        DisplayOrder = 1
                    };
                    productId1.SimilarProducts.Add(similar);
                    await productService.InsertSimilarProduct(similar);
                }
        }
    }

    public virtual async Task UpdateSimilarProductModel(ProductModel.SimilarProductModel model)
    {
        var product1 = await productService.GetProductById(model.ProductId1, true);
        var similarProduct = product1.SimilarProducts.FirstOrDefault(x => x.Id == model.Id);
        if (similarProduct == null)
            throw new ArgumentException("No similar product found with the specified id");

        var product2 = await productService.GetProductById(similarProduct.ProductId2);
        if (product2 == null)
            throw new ArgumentException("No product found with the specified id");

        similarProduct.ProductId1 = model.ProductId1;
        similarProduct.DisplayOrder = model.DisplayOrder;
        await productService.UpdateSimilarProduct(similarProduct);
    }

    public virtual async Task DeleteSimilarProductModel(ProductModel.SimilarProductModel model)
    {
        var product = await productService.GetProductById(model.ProductId1, true);
        var similarProduct = product.SimilarProducts.FirstOrDefault(x => x.Id == model.Id);
        if (similarProduct == null)
            throw new ArgumentException("No similar product found with the specified id");

        similarProduct.ProductId1 = model.ProductId1;
        await productService.DeleteSimilarProduct(similarProduct);
    }

    public virtual async Task InsertCrossSellProductModel(ProductModel.AddCrossSellProductModel model)
    {
        var crossSellProduct = await productService.GetProductById(model.ProductId, true);
        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            // scope.HasAccess: see InsertRelatedProductModel above.
            if (product == null || !await scope.HasAccess(product)) continue;

            if (!crossSellProduct.CrossSellProduct.Any(x => x == id))
                if (model.ProductId != id)
                    await productService.InsertCrossSellProduct(
                        new CrossSellProduct {
                            ProductId1 = model.ProductId,
                            ProductId2 = id
                        });
        }
    }

    public virtual async Task DeleteCrossSellProduct(string productId, string crossSellProductId)
    {
        var crosssell = new CrossSellProduct {
            ProductId1 = productId,
            ProductId2 = crossSellProductId
        };
        await productService.DeleteCrossSellProduct(crosssell);
    }

    public virtual async Task InsertRecommendedProductModel(ProductModel.AddRecommendedProductModel model)
    {
        var mainproduct = await productService.GetProductById(model.ProductId, true);
        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            // scope.HasAccess: see InsertRelatedProductModel above.
            if (product == null || !await scope.HasAccess(product)) continue;

            if (!mainproduct.RecommendedProduct.Any(x => x == id))
                if (model.ProductId != id)
                    await productService.InsertRecommendedProduct(model.ProductId, id);
        }
    }

    public virtual async Task DeleteRecommendedProduct(string productId, string recommendedProductId)
    {
        await productService.DeleteRecommendedProduct(productId, recommendedProductId);
    }
}
