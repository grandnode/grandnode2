using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductCategoryCommandHandler : IRequestHandler<UpdateProductCategoryCommand, bool>
    {
        private readonly IProductCategoryService _productCategoryService;
        private readonly IProductService _productService;

        public UpdateProductCategoryCommandHandler(
            IProductCategoryService productCategoryService,
            IProductService productService)
        {
            _productCategoryService = productCategoryService;
            _productService = productService;
        }

        public async Task<bool> Handle(UpdateProductCategoryCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var productCategory = product.ProductCategories.Where(x => x.CategoryId == request.Model.CategoryId).FirstOrDefault();
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");

            productCategory.CategoryId = request.Model.CategoryId;
            productCategory.IsFeaturedProduct = request.Model.IsFeaturedProduct;

            await _productCategoryService.UpdateProductCategory(productCategory, product.Id);

            return true;
        }
    }
}
