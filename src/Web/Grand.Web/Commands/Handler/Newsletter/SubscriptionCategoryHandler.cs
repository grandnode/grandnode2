using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Web.Commands.Models.Newsletter;
using MediatR;

namespace Grand.Web.Commands.Handler.Newsletter;

public class
    SubscriptionCategoryHandler : IRequestHandler<SubscriptionCategoryCommand, (string message, bool success)>
{
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;

    public SubscriptionCategoryHandler(INewsLetterSubscriptionService newsLetterSubscriptionService)
    {
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
    }

    public async Task<(string message, bool success)> Handle(SubscriptionCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var success = false;
        var message = string.Empty;

        var newsletterEmailId = request.Model.NewsletterEmailId;
        if (!string.IsNullOrEmpty(newsletterEmailId))
        {
            var subscription =
                await _newsLetterSubscriptionService.GetNewsLetterSubscriptionById(newsletterEmailId);
            if (subscription != null)
            {
                foreach (var category in request.Model.Category) subscription.Categories.Add(category);

                success = true;
                await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription, false);
            }
            else
            {
                message = "Email not exist";
            }
        }
        else
        {
            message = "Empty NewsletterEmailId";
        }

        return (message, success);
    }
}