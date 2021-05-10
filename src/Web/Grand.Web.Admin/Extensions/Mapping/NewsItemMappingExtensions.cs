using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.News;
using Grand.Web.Admin.Models.News;

namespace Grand.Web.Admin.Extensions
{
    public static class NewsItemMappingExtensions
    {
        public static NewsItemModel ToModel(this NewsItem entity, IDateTimeService dateTimeService)
        {
            var newsitem = entity.MapTo<NewsItem, NewsItemModel>();
            newsitem.StartDate = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
            newsitem.EndDate = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
            return newsitem;
        }

        public static NewsItem ToEntity(this NewsItemModel model, IDateTimeService dateTimeService)
        {
            var newsitem = model.MapTo<NewsItemModel, NewsItem>();
            newsitem.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
            newsitem.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
            return newsitem;
        }

        public static NewsItem ToEntity(this NewsItemModel model, NewsItem destination, IDateTimeService dateTimeService)
        {
            var newsitem = model.MapTo(destination);
            newsitem.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
            newsitem.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
            return newsitem;
        }
    }
}