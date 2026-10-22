using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductCategoryViewModelService(
    IProductService productService,
    ICategoryService categoryService,
    IProductCategoryService productCategoryService)
    : IProductCategoryViewModelService
{
    public virtual async Task<IList<ProductModel.ProductCategoryModel>> PrepareProductCategoryModel(Product product)
    {
        var productCategories = product.ProductCategories.OrderBy(x => x.DisplayOrder);
        var items = new List<ProductModel.ProductCategoryModel>();
        foreach (var x in productCategories)
        {
            var category = await categoryService.GetCategoryById(x.CategoryId);
            items.Add(new ProductModel.ProductCategoryModel {
                Id = x.Id,
                Category = await categoryService.GetFormattedBreadCrumb(category),
                ProductId = product.Id,
                CategoryId = x.CategoryId,
                IsFeaturedProduct = x.IsFeaturedProduct,
                DisplayOrder = x.DisplayOrder
            });
        }

        return items;
    }

    public virtual async Task InsertProductCategoryModel(ProductModel.ProductCategoryModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);
        if (product != null && product.ProductCategories.All(x => x.CategoryId != model.CategoryId))
        {
            var productCategory = new ProductCategory {
                CategoryId = model.CategoryId,
                DisplayOrder = model.DisplayOrder,
                IsFeaturedProduct = model.IsFeaturedProduct
            };
            await productCategoryService.InsertProductCategory(productCategory, product.Id);
        }
    }

    public virtual async Task UpdateProductCategoryModel(ProductModel.ProductCategoryModel model)
    {
        var product = await productService.GetProductById(model.ProductId, true);
        var productCategory = product.ProductCategories.FirstOrDefault(x => x.Id == model.Id);
        if (productCategory == null)
            throw new ArgumentException("No product category mapping found with the specified id");

        if (product.ProductCategories.Any(x => x.Id != model.Id && x.CategoryId == model.CategoryId))
            throw new ArgumentException("This category is already mapped with this product");

        productCategory.CategoryId = model.CategoryId;
        productCategory.DisplayOrder = model.DisplayOrder;
        productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
        await productCategoryService.UpdateProductCategory(productCategory, product.Id);
    }

    public virtual async Task DeleteProductCategory(string id, string productId)
    {
        var product = await productService.GetProductById(productId, true);
        var productCategory = product.ProductCategories.FirstOrDefault(x => x.Id == id);
        if (productCategory == null)
            throw new ArgumentException("No product category mapping found with the specified id");

        await productCategoryService.DeleteProductCategory(productCategory, product.Id);
    }
}
