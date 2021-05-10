using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class AddProductAttributeCommandHandler : IRequestHandler<AddProductAttributeCommand, ProductAttributeDto>
    {
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public AddProductAttributeCommandHandler(
            IProductAttributeService productAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _productAttributeService = productAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<ProductAttributeDto> Handle(AddProductAttributeCommand request, CancellationToken cancellationToken)
        {
            var productAttribute = request.Model.ToEntity();

            await _productAttributeService.InsertProductAttribute(productAttribute);

            //activity log
            await _customerActivityService.InsertActivity("AddNewProductAttribute",
                productAttribute.Id, _translationService.GetResource("ActivityLog.AddNewProductAttribute"), productAttribute.Name);

            return productAttribute.ToModel();
        }
    }
}
