using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductSpecificationCommandHandler : IRequestHandler<AddProductSpecificationCommand, bool>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;

        public AddProductSpecificationCommandHandler(ISpecificationAttributeService specificationAttributeService)
        {
            _specificationAttributeService = specificationAttributeService;
        }

        public async Task<bool> Handle(AddProductSpecificationCommand request, CancellationToken cancellationToken)
        {
            //we allow filtering only for "Option" attribute type
            if (request.Model.AttributeTypeId != SpecificationAttributeType.Option)
            {
                request.Model.AllowFiltering = false;
                request.Model.SpecificationAttributeOptionId = null;
            }

            var psa = new ProductSpecificationAttribute
            {
                AttributeTypeId = request.Model.AttributeTypeId,
                SpecificationAttributeOptionId = request.Model.SpecificationAttributeOptionId,
                SpecificationAttributeId = request.Model.SpecificationAttributeId,
                CustomValue = request.Model.CustomValue,
                AllowFiltering = request.Model.AllowFiltering,
                ShowOnProductPage = request.Model.ShowOnProductPage,
                DisplayOrder = request.Model.DisplayOrder,
            };
            await _specificationAttributeService.InsertProductSpecificationAttribute(psa, request.Product.Id);

            return true;
        }
    }
}
