using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, bool>
    {
        private readonly ICollectionService _collectionService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public DeleteCollectionCommandHandler(
            ICollectionService collectionService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _collectionService = collectionService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
        {
            var collection = await _collectionService.GetCollectionById(request.Model.Id);
            if (collection != null)
            {
                await _collectionService.DeleteCollection(collection);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteCollection", collection.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.DeleteCollection"), collection.Name);
            }
            return true;
        }
    }
}
