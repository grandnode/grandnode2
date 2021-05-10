using AutoMapper;
using Grand.Business.Authentication.Interfaces;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.ExternalAuthentication;

namespace Grand.Web.Admin.Mapper
{
    public class ExternalAuthenticationMethodProfile : Profile, IAutoMapperProfile
    {
        public ExternalAuthenticationMethodProfile()
        {
            CreateMap<IExternalAuthenticationProvider, AuthenticationMethodModel>()
                .ForMember(dest => dest.FriendlyName, mo => mo.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.SystemName, mo => mo.MapFrom(src => src.SystemName))
                .ForMember(dest => dest.DisplayOrder, mo => mo.MapFrom(src => src.Priority))
                .ForMember(dest => dest.IsActive, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}