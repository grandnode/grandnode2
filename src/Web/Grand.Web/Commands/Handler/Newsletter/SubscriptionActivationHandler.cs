using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Web.Commands.Models.Newsletter;
using Grand.Web.Models.Newsletter;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Newsletter
{
    public class SubscriptionActivationHandler : IRequestHandler<SubscriptionActivationCommand, SubscriptionActivationModel>
    {
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ITranslationService _translationService;

        public SubscriptionActivationHandler(INewsLetterSubscriptionService newsLetterSubscriptionService, ITranslationService translationService)
        {
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _translationService = translationService;
        }

        public async Task<SubscriptionActivationModel> Handle(SubscriptionActivationCommand request, CancellationToken cancellationToken)
        {
            var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByGuid(request.Token);
            if (subscription == null)
                return null;

            var model = new SubscriptionActivationModel();

            if (request.Active)
            {
                subscription.Active = true;
                await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);
            }
            else
                await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(subscription);

            model.Result = request.Active
                ? _translationService.GetResource("Newsletter.ResultActivated")
                : _translationService.GetResource("Newsletter.ResultDeactivated");

            return model;

        }
    }
}
