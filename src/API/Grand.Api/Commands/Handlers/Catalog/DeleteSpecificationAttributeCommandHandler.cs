using Grand.Api.Commands.Models.Catalog;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class DeleteSpecificationAttributeCommandHandler : IRequestHandler<DeleteSpecificationAttributeCommand, bool>
    {
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public DeleteSpecificationAttributeCommandHandler(
            ISpecificationAttributeService specificationAttributeService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _specificationAttributeService = specificationAttributeService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(DeleteSpecificationAttributeCommand request, CancellationToken cancellationToken)
        {
            var specificationAttribute = await _specificationAttributeService.GetSpecificationAttributeById(request.Model.Id);
            if (specificationAttribute != null)
            {
                await _specificationAttributeService.DeleteSpecificationAttribute(specificationAttribute);
                //activity log
                await _customerActivityService.InsertActivity("DeleteSpecAttribute", specificationAttribute.Id, _translationService.GetResource("ActivityLog.DeleteSpecAttribute"), specificationAttribute.Name);
            }
            return true;
        }
    }
}
