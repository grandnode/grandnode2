using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductAttributeMappingCommandHandler : IRequestHandler<AddProductAttributeMappingCommand, ProductAttributeMappingDto>
    {
        private readonly IProductAttributeService _productAttributeService;

        public AddProductAttributeMappingCommandHandler(IProductAttributeService productAttributeService)
        {
            _productAttributeService = productAttributeService;
        }

        public async Task<ProductAttributeMappingDto> Handle(AddProductAttributeMappingCommand request, CancellationToken cancellationToken)
        {
            //insert mapping
            var productAttributeMapping = request.Model.ToEntity();
            await _productAttributeService.InsertProductAttributeMapping(productAttributeMapping, request.Product.Id);

            return productAttributeMapping.ToModel();
        }
    }
}
