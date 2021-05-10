using Grand.Business.Marketing.Events;
using MediatR;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Extensions
{
    public static class NewsletterPublisherExtensions
    {
        /// <summary>
        /// Publishes the newsletter subscribe event.
        /// </summary>
        /// <param name="mediator">mediator</param>
        /// <param name="email">The email.</param>
        public static async Task PublishNewsletterSubscribe(this IMediator mediator, string email)
        {
            await mediator.Publish(new EmailSubscribedEvent(email));
        }

        /// <summary>
        /// Publishes the newsletter unsubscribe event.
        /// </summary>
        /// <param name="mediator">Mediator</param>
        /// <param name="email">The email.</param>
        public static async Task PublishNewsletterUnsubscribe(this IMediator mediator, string email)
        {
            await mediator.Publish(new EmailUnsubscribedEvent(email));
        }


    }
}