using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Customers
{
    public class UpdateCustomerGroupCommandHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
    {
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public UpdateCustomerGroupCommandHandler(
            IGroupService groupService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _groupService = groupService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand request, CancellationToken cancellationToken)
        {
            var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
            customerGroup = request.Model.ToEntity(customerGroup);
            await _groupService.UpdateCustomerGroup(customerGroup);

            //activity log
            await _customerActivityService.InsertActivity("EditCustomerGroup", customerGroup.Id, _translationService.GetResource("ActivityLog.EditCustomerGroup"), customerGroup.Name);

            return customerGroup.ToModel();
        }
    }
}
