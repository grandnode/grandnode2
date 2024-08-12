using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Pages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Pages;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class PageMappingExtensions
{
    public static PageModel ToModel(this IMapper mapper, Page entity, IDateTimeService dateTimeService)
    {
        var page = mapper.Map<PageModel>(entity);
        page.StartDateUtc = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
        page.EndDateUtc = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
        return page;
    }

    public static Page ToEntity(this IMapper mapper, PageModel model, IDateTimeService dateTimeService)
    {
        var page = mapper.Map<Page>(model);
        page.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
        page.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
        return page;
    }

    public static Page ToEntity(this IMapper mapper, PageModel model, Page destination, IDateTimeService dateTimeService)
    {
        var page = mapper.Map(model, destination);
        page.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
        page.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
        return page;
    }
}