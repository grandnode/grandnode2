using Grand.Infrastructure.Mapper;
using Grand.Business.Core.Interfaces.Cms;
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