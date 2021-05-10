using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class BannerProfile : Profile, IAutoMapperProfile
    {
        public BannerProfile()
        {
            CreateMap<Banner, BannerModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<BannerModel, Banner>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}