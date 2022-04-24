using Grand.Api.Commands.Models.Customers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Customers
{
    public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;

        public DeleteCustomerCommandHandler(
            ICustomerService customerService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _customerService = customerService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetCustomerByEmail(request.Email);
            if (customer != null)
            {
                await _customerService.DeleteCustomer(customer);
                //activity log
                _ = _customerActivityService.InsertActivity("DeleteCustomer", customer.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.DeleteCustomer"), customer.Id);
            }

            return true;
        }

    }
}
