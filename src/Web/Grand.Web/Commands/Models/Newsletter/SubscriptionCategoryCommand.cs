using Grand.Web.Models.Newsletter;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Newsletter;

public class SubscriptionCategoryCommand : IRequest<(string message, bool success)>
{
    public NewsletterCategoryModel Model { get; set; }
}