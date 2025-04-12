using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Models.Plugins;

namespace Grand.Web.AdminShared.Mapper;

public class PluginDescriptorProfile : Profile, IAutoMapperProfile
{
    public PluginDescriptorProfile()
    {
        CreateMap<PluginInfo, PluginModel>();
    }

    public int Order => 0;
}