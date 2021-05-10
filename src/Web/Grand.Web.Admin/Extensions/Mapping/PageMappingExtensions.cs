using Grand.Infrastructure.Mapper;
using Grand.Domain.Pages;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Admin.Models.Pages;

namespace Grand.Web.Admin.Extensions
{
    public static class PageMappingExtensions
    {
        public static PageModel ToModel(this Page entity, IDateTimeService dateTimeService)
        {
            var page = entity.MapTo<Page, PageModel>();
            page.StartDateUtc = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
            page.EndDateUtc = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
            return page;
        }

        public static Page ToEntity(this PageModel model, IDateTimeService dateTimeService)
        {
            var page = model.MapTo<PageModel, Page>();
            page.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
            page.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
            return page;
        }

        public static Page ToEntity(this PageModel model, Page destination, IDateTimeService dateTimeService)
        {
            var page = model.MapTo(destination);
            page.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
            page.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
            return page;
        }
    }
}