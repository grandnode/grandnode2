using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductSpecificationCommandHandler : IRequestHandler<DeleteProductSpecificationCommand, bool>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IProductService _productService;

        public DeleteProductSpecificationCommandHandler(ISpecificationAttributeService specificationAttributeService,
            IProductService productService)
        {
            _specificationAttributeService = specificationAttributeService;
            _productService = productService;
        }

        public async Task<bool> Handle(DeleteProductSpecificationCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == request.Id);
            if (psa != null)
            {
                await _specificationAttributeService.DeleteProductSpecificationAttribute(psa, product.Id);
            }

            return true;
        }
    }
}
