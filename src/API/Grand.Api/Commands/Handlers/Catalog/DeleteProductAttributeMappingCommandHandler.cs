using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductAttributeMappingCommandHandler : IRequestHandler<DeleteProductAttributeMappingCommand, bool>
    {
        private readonly IProductAttributeService _productAttributeService;

        public DeleteProductAttributeMappingCommandHandler(IProductAttributeService productAttributeService)
        {
            _productAttributeService = productAttributeService;
        }

        public async Task<bool> Handle(DeleteProductAttributeMappingCommand request, CancellationToken cancellationToken)
        {
            //insert mapping
            var productAttributeMapping = request.Model.ToEntity();
            await _productAttributeService.DeleteProductAttributeMapping(productAttributeMapping, request.Product.Id);

            return true;
        }
    }
}
