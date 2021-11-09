using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand, bool>
    {
        private readonly IBrandService _brandService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public DeleteBrandCommandHandler(
            IBrandService brandService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _brandService = brandService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
        {
            var brand = await _brandService.GetBrandById(request.Model.Id);
            if (brand != null)
            {
                await _brandService.DeleteBrand(brand);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteBrand", brand.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.DeleteBrand"), brand.Name);
            }
            return true;
        }
    }
}
