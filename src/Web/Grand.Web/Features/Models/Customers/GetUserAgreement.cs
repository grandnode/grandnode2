using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Models.Customers
{
    public class GetUserAgreement : IRequest<UserAgreementModel>
    {
        public Guid OrderItemId { get; set; }
    }
}
