using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
