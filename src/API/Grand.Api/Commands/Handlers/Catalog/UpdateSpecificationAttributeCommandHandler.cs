using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class UpdateSpecificationAttributeCommandHandler : IRequestHandler<UpdateSpecificationAttributeCommand, SpecificationAttributeDto>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public UpdateSpecificationAttributeCommandHandler(
            ISpecificationAttributeService specificationAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _specificationAttributeService = specificationAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<SpecificationAttributeDto> Handle(UpdateSpecificationAttributeCommand request, CancellationToken cancellationToken)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(request.Model.Id);
            foreach (var option in specificationAttribute.SpecificationAttributeOptions)
            {
                if (request.Model.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == option.Id) == null)
                {
                    await _specificationAttributeService.DeleteSpecificationAttributeOption(option);
                }
            }
            specificationAttribute = request.Model.ToEntity(specificationAttribute);
            await _specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);

            //activity log
            _ = _customerActivityService.InsertActivity("EditSpecAttribute",
                specificationAttribute.Id, _workContext.CurrentCustomer, "",
                _translationService.GetResource("ActivityLog.EditSpecAttribute"), specificationAttribute.Name);

            return specificationAttribute.ToModel();
        }
    }
}
