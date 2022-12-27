using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Commands.Models.Customers
{
    public class SubAccountAddCommand : IRequest<RegistrationResult>
    {
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public SubAccountModel Model { get; set; }
    }
}
