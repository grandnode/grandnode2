using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetUserAgreement : IRequest<UserAgreementModel>
{
    public Guid OrderItemId { get; set; }
}