using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Models.Plugins;

namespace Grand.Web.Admin.Mapper
{
    public class PluginDescriptorProfile : Profile, IAutoMapperProfile
    {
        public PluginDescriptorProfile()
        {
            CreateMap<PluginInfo, PluginModel>();
        }

        public int Order => 0;
    }
}