using Grand.Api.Commands.Models.Catalog;
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
    public class AddSpecificationAttributeCommandHandler : IRequestHandler<AddSpecificationAttributeCommand, SpecificationAttributeDto>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public AddSpecificationAttributeCommandHandler(
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

        public async Task<SpecificationAttributeDto> Handle(AddSpecificationAttributeCommand request, CancellationToken cancellationToken)
        {
            var specificationAttribute = request.Model.ToEntity();
            await _specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);

            //activity log
            _ = _customerActivityService.InsertActivity("AddNewSpecAttribute", specificationAttribute.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.AddNewSpecAttribute"), specificationAttribute.Name);

            return specificationAttribute.ToModel();
        }
    }
}
