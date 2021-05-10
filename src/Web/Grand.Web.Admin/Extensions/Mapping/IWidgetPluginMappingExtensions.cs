using Grand.Infrastructure.Mapper;
using Grand.Business.Cms.Interfaces;
using Grand.Web.Admin.Models.Cms;

namespace Grand.Web.Admin.Extensions
{
    public static class IWidgetPluginMappingExtensions
    {
        public static WidgetModel ToModel(this IWidgetProvider entity)
        {
            return entity.MapTo<IWidgetProvider, WidgetModel>();
        }
    }
}