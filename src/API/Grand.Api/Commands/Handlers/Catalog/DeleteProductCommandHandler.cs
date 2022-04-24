using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly IProductService _productService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public DeleteProductCommandHandler(
            IProductService productService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _productService = productService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Model.Id);
            if (product != null)
            {
                await _productService.DeleteProduct(product);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteProduct", product.Id,
                    _workContext.CurrentCustomer, "",
                    _translationService.GetResource("ActivityLog.DeleteProduct"), product.Name);
            }
            return true;
        }
    }
}
