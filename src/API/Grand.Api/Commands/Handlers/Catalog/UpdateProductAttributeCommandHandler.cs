﻿using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, ProductAttributeDto>
    {
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public UpdateProductAttributeCommandHandler(
            IProductAttributeService productAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _productAttributeService = productAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<ProductAttributeDto> Handle(UpdateProductAttributeCommand request, CancellationToken cancellationToken)
        {
            var productAttribute = await _productAttributeService.GetProductAttributeById(request.Model.Id);

            productAttribute = request.Model.ToEntity(productAttribute);
            await _productAttributeService.UpdateProductAttribute(productAttribute);

            //activity log
            _ = _customerActivityService.InsertActivity("EditProductAttribute", productAttribute.Id,
                _workContext.CurrentCustomer, "",
                _translationService.GetResource("ActivityLog.EditProductAttribute"), productAttribute.Name);

            return productAttribute.ToModel();
        }
    }
}
