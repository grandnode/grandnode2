using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductSpecificationCommandHandler : IRequestHandler<UpdateProductSpecificationCommand, bool>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IProductService _productService;

        public UpdateProductSpecificationCommandHandler(
            ISpecificationAttributeService specificationAttributeService,
            IProductService productService)
        {
            _specificationAttributeService = specificationAttributeService;
            _productService = productService;
        }

        public async Task<bool> Handle(UpdateProductSpecificationCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id, true);
            var psa = product.ProductSpecificationAttributes.FirstOrDefault(x => x.Id == request.Model.Id);
            if (psa != null)
            {
                if (request.Model.AttributeTypeId == SpecificationAttributeType.Option)
                {
                    psa.AllowFiltering = request.Model.AllowFiltering;
                    psa.SpecificationAttributeOptionId = request.Model.SpecificationAttributeOptionId;
                }
                else
                {
                    psa.CustomName = request.Model.CustomName;
                    psa.CustomValue = request.Model.CustomValue;
                }
                psa.SpecificationAttributeId = request.Model.SpecificationAttributeId;
                psa.SpecificationAttributeOptionId = request.Model.SpecificationAttributeOptionId;
                psa.AttributeTypeId = request.Model.AttributeTypeId;
                psa.ShowOnProductPage = request.Model.ShowOnProductPage;
                psa.DisplayOrder = request.Model.DisplayOrder;
                await _specificationAttributeService.UpdateProductSpecificationAttribute(psa, product.Id);
            }

            return true;
        }
    }
}
