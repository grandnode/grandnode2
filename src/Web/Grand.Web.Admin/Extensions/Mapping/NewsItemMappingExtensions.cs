using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.News;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class NewsItemMappingExtensions
{
    public static NewsItemModel ToModel(this IMapper mapper, NewsItem entity, IDateTimeService dateTimeService)
    {
        var newsItem = mapper.Map<NewsItemModel>(entity);
        newsItem.StartDate = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
        newsItem.EndDate = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
        return newsItem;
    }

    public static NewsItem ToEntity(this IMapper mapper, NewsItemModel model, IDateTimeService dateTimeService)
    {
        var newsItem = mapper.Map<NewsItem>(model);
        newsItem.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
        newsItem.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
        return newsItem;
    }

    public static NewsItem ToEntity(this IMapper mapper, NewsItemModel model, NewsItem destination, IDateTimeService dateTimeService)
    {
        var newsItem = mapper.Map(model, destination);
        newsItem.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
        newsItem.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
        return newsItem;
    }
}