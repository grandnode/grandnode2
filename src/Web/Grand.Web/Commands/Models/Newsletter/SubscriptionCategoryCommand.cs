using MediatR;

namespace Grand.Web.Commands.Models.Newsletter
{
    public class SubscriptionCategoryCommand : IRequest<(string message, bool success)>
    {
        public IDictionary<string, string> Values { get; set; }
    }
}
