using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductCollectionViewModelService(
    IProductService productService,
    ICollectionService collectionService,
    IProductCollectionService productCollectionService)
    : IProductCollectionViewModelService
{
    public virtual async Task<IList<ProductModel.ProductCollectionModel>> PrepareProductCollectionModel(Product product)
    {
        var items = new List<ProductModel.ProductCollectionModel>();
        foreach (var x in product.ProductCollections.OrderBy(x => x.DisplayOrder))
            items.Add(new ProductModel.ProductCollectionModel {
                Id = x.Id,
                Collection = (await collectionService.GetCollectionById(x.CollectionId)).Name,
                ProductId = product.Id,
                CollectionId = x.CollectionId,
                IsFeaturedProduct = x.IsFeaturedProduct,
                DisplayOrder = x.DisplayOrder
            });
        return items;
    }

    public virtual async Task InsertProductCollection(ProductModel.ProductCollectionModel model)
    {
        var collectionId = model.CollectionId;
        var product = await productService.GetProductById(model.ProductId, true);
        if (product != null && product.ProductCollections.All(x => x.CollectionId != collectionId))
        {
            var productCollection = new ProductCollection {
                CollectionId = collectionId,
                DisplayOrder = model.DisplayOrder,
                IsFeaturedProduct = model.IsFeaturedProduct
            };
            await productCollectionService.InsertProductCollection(productCollection, model.ProductId);
        }
    }

    public virtual async Task UpdateProductCollection(ProductModel.ProductCollectionModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);
        var productCollection = product.ProductCollections.FirstOrDefault(x => x.Id == model.Id);
        if (productCollection == null)
            throw new ArgumentException("No product collection mapping found with the specified id");

        if (product.ProductCollections.Any(x => x.Id != model.Id && x.CollectionId == model.CollectionId))
            throw new ArgumentException("This collection is already mapped with this product");

        productCollection.CollectionId = model.CollectionId;
        productCollection.DisplayOrder = model.DisplayOrder;
        productCollection.IsFeaturedProduct = model.IsFeaturedProduct;

        await productCollectionService.UpdateProductCollection(productCollection, product.Id);
    }

    public virtual async Task DeleteProductCollection(string id, string productId)
    {
        var product = await productService.GetProductById(productId, true);
        var productCollection = product.ProductCollections.FirstOrDefault(x => x.Id == id);
        if (productCollection == null)
            throw new ArgumentException("No product collection mapping found with the specified id");

        await productCollectionService.DeleteProductCollection(productCollection, product.Id);
    }
}
