using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class SpecificationAttributeProfile : Profile, IAutoMapperProfile
    {
        public SpecificationAttributeProfile()
        {
            CreateMap<SpecificationAttribute, SpecificationAttributeModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<SpecificationAttributeModel, SpecificationAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.SpecificationAttributeOptions, mo => mo.Ignore());
            CreateMap<SpecificationAttributeOption, SpecificationAttributeOptionModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.NumberOfAssociatedProducts, mo => mo.Ignore());
            CreateMap<SpecificationAttributeOptionModel, SpecificationAttributeOption>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}