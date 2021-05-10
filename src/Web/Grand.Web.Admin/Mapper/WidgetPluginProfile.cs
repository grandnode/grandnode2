using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Business.Cms.Interfaces;
using Grand.Web.Admin.Models.Cms;

namespace Grand.Web.Admin.Mapper
{
    public class WidgetPluginProfile : Profile, IAutoMapperProfile
    {
        public WidgetPluginProfile()
        {
            CreateMap<IWidgetProvider, WidgetModel>()
                .ForMember(dest => dest.FriendlyName, mo => mo.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.SystemName, mo => mo.MapFrom(src => src.SystemName))
                .ForMember(dest => dest.DisplayOrder, mo => mo.MapFrom(src => src.Priority))
                .ForMember(dest => dest.IsActive, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}