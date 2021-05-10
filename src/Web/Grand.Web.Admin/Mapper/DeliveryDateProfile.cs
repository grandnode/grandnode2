using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Mapper
{
    public class DeliveryDateProfile : Profile, IAutoMapperProfile
    {
        public DeliveryDateProfile()
        {
            CreateMap<DeliveryDate, DeliveryDateModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<DeliveryDateModel, DeliveryDate>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}