using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class MerchandiseReturnActionProfile : Profile, IAutoMapperProfile
    {
        public MerchandiseReturnActionProfile()
        {
            CreateMap<MerchandiseReturnAction, MerchandiseReturnActionModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<MerchandiseReturnActionModel, MerchandiseReturnAction>()
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}