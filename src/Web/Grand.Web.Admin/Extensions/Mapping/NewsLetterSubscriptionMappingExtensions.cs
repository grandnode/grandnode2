using Grand.Infrastructure.Mapper;
using Grand.Domain.Messages;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Extensions
{
    public static class NewsLetterSubscriptionMappingExtensions
    {
        public static NewsLetterSubscriptionModel ToModel(this NewsLetterSubscription entity)
        {
            return entity.MapTo<NewsLetterSubscription, NewsLetterSubscriptionModel>();
        }

        public static NewsLetterSubscription ToEntity(this NewsLetterSubscriptionModel model)
        {
            return model.MapTo<NewsLetterSubscriptionModel, NewsLetterSubscription>();
        }

        public static NewsLetterSubscription ToEntity(this NewsLetterSubscriptionModel model, NewsLetterSubscription destination)
        {
            return model.MapTo(destination);
        }
    }
}