using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
