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
    public class AddSpecificationAttributeCommandHandler : IRequestHandler<AddSpecificationAttributeCommand, SpecificationAttributeDto>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public AddSpecificationAttributeCommandHandler(
            ISpecificationAttributeService specificationAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _specificationAttributeService = specificationAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<SpecificationAttributeDto> Handle(AddSpecificationAttributeCommand request, CancellationToken cancellationToken)
        {
            var specificationAttribute = request.Model.ToEntity();
            await _specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);

            //activity log
            await _customerActivityService.InsertActivity("AddNewSpecAttribute", specificationAttribute.Id, _translationService.GetResource("ActivityLog.AddNewSpecAttribute"), specificationAttribute.Name);

            return specificationAttribute.ToModel();
        }
    }
}
