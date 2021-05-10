using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IProductService _productService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public DeleteProductCommandHandler(
            IProductService productService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _productService = productService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Model.Id);
            if (product != null)
            {
                await _productService.DeleteProduct(product);

                //activity log
                await _customerActivityService.InsertActivity("DeleteProduct", product.Id, _translationService.GetResource("ActivityLog.DeleteProduct"), product.Name);
            }
            return true;
        }
    }
}
