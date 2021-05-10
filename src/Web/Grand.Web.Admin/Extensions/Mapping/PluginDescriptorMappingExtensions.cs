using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Models.Plugins;

namespace Grand.Web.Admin.Extensions
{
    public static class PluginDescriptorMappingExtensions
    {
        public static PluginModel ToModel(this PluginInfo entity)
        {
            return entity.MapTo<PluginInfo, PluginModel>();
        }
    }
}