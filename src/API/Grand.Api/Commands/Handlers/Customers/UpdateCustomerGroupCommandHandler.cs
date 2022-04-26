using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Customers
{
    public class UpdateCustomerGroupCommandHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
    {
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public UpdateCustomerGroupCommandHandler(
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

        public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand request, CancellationToken cancellationToken)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
            customerGroup = request.Model.ToEntity(customerGroup);
            await _groupService.UpdateCustomerGroup(customerGroup);

            //activity log
            _ = _customerActivityService.InsertActivity("EditCustomerGroup", customerGroup.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.EditCustomerGroup"), customerGroup.Name);

            return customerGroup.ToModel();
        }
    }
}
