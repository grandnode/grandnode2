using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetSubAccountsHandler : IRequestHandler<GetSubAccounts, IList<SubAccountSimpleModel>>
    {
        private readonly ICustomerService _customerService;
        public GetSubAccountsHandler(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IList<SubAccountSimpleModel>> Handle(GetSubAccounts request, CancellationToken cancellationToken)
        {
            var model = new List<SubAccountSimpleModel>();

            var subaccouns = await _customerService.GetAllCustomers(ownerId: request.Customer.Id);
            foreach (var item in subaccouns)
            {
                model.Add(new SubAccountSimpleModel()
                {
                    Id = item.Id,
                    Email = item.Email,
                    Active = item.Active,
                    FirstName = item.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                    LastName = item.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)
                });
            }

            return model;
        }
    }
}
