using Grand.Api.Commands.Models.Catalog;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class DeleteProductAttributeCommandHandler : IRequestHandler<DeleteProductAttributeCommand, bool>
    {
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public DeleteProductAttributeCommandHandler(
            IProductAttributeService productAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _productAttributeService = productAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(DeleteProductAttributeCommand request, CancellationToken cancellationToken)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(request.Model.Id);
            if (productAttribute != null)
            {
                await _productAttributeService.DeleteProductAttribute(productAttribute);
                //activity log
                await _customerActivityService.InsertActivity("DeleteProductAttribute", productAttribute.Id,
                    _translationService.GetResource("ActivityLog.DeleteProductAttribute"), productAttribute.Name);
            }
            return true;
        }
    }
}
