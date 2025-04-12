using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Models.Plugins;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class PluginDescriptorMappingExtensions
{
    public static PluginModel ToModel(this PluginInfo entity)
    {
        return entity.MapTo<PluginInfo, PluginModel>();
    }
}