using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Customers
{
    public class AddCustomerGroupCommandHandler : IRequestHandler<AddCustomerGroupCommand, CustomerGroupDto>
    {
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public AddCustomerGroupCommandHandler(
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

        public async Task<CustomerGroupDto> Handle(AddCustomerGroupCommand request, CancellationToken cancellationToken)
        {
            var customergroup = request.Model.ToEntity();
            await _groupService.InsertCustomerGroup(customergroup);

            //activity log
            _ = _customerActivityService.InsertActivity("AddNewCustomerGroup", customergroup.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.AddNewCustomerGroup"), customergroup.Name);

            return customergroup.ToModel();
        }
    }
}
