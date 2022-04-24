using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Customers
{
    public class DeleteCustomerGroupCommandHandler : IRequestHandler<DeleteCustomerGroupCommand, bool>
    {
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public DeleteCustomerGroupCommandHandler(
            IGroupService groupService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _groupService = groupService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteCustomerGroupCommand request, CancellationToken cancellationToken)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
            if (customerGroup != null)
            {
                await _groupService.DeleteCustomerGroup(customerGroup);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteCustomerGroup", customerGroup.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.DeleteCustomerGroup"), customerGroup.Name);
            }
            return true;
        }
    }
}
