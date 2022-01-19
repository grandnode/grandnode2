using Grand.Web.Models.Newsletter;
using MediatR;

namespace Grand.Web.Commands.Models.Newsletter
{
    public class SubscriptionActivationCommand : IRequest<SubscriptionActivationModel>
    {
        public Guid Token { get; set; }
        public bool Active { get; set; }
    }
}
