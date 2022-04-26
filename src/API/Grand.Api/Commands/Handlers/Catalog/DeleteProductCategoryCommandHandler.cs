using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductCategoryCommandHandler : IRequestHandler<DeleteProductCategoryCommand, bool>
    {
        private readonly IProductCategoryService _productcategoryService;
        private readonly IProductService _productService;

        public DeleteProductCategoryCommandHandler(
            IProductCategoryService productcategoryService,
            IProductService productService)
        {
            _productcategoryService = productcategoryService;
            _productService = productService;
        }

        public async Task<bool> Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var productCategory = product.ProductCategories.Where(x => x.CategoryId == request.CategoryId).FirstOrDefault();
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");

            await _productcategoryService.DeleteProductCategory(productCategory, product.Id);

            return true;
        }
    }
}
