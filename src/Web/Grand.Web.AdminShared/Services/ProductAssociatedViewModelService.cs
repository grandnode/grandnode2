using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductAssociatedViewModelService(
    IProductService productService)
    : IProductAssociatedViewModelService
{
    public virtual async Task InsertAssociatedProductModel(ProductModel.AddAssociatedProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            if (product != null)
            {
                product.ParentGroupedProductId = model.ProductId;
                await productService.UpdateAssociatedProduct(product);
            }
        }
    }

    public virtual async Task DeleteAssociatedProduct(Product product)
    {
        product.ParentGroupedProductId = "";
        await productService.UpdateAssociatedProduct(product);
    }
}
