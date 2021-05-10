using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
