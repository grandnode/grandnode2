using Grand.Api.Commands.Models.Customers;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Customers
{
    public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public DeleteCustomerCommandHandler(
            ICustomerService customerService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _customerService = customerService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetCustomerByEmail(request.Email);
            if (customer != null)
            {
                await _customerService.DeleteCustomer(customer);
                //activity log
                await _customerActivityService.InsertActivity("DeleteCustomer", customer.Id, _translationService.GetResource("ActivityLog.DeleteCustomer"), customer.Id);
            }

            return true;
        }

    }
}
