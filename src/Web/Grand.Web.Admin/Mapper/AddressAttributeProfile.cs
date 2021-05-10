using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Mapper
{
    public class AddressAttributeProfile : Profile, IAutoMapperProfile
    {
        public AddressAttributeProfile()
        {
            CreateMap<AddressAttribute, AddressAttributeModel>()
                .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<AddressAttributeModel, AddressAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.AttributeControlType, mo => mo.Ignore())
                .ForMember(dest => dest.AddressAttributeValues, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}