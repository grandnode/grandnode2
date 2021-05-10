using AutoMapper;
using Grand.Api.DTOs.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Api.Infrastructure.Mapper
{
    public class CollectionProfile : Profile, IAutoMapperProfile
    {
        public CollectionProfile()
        {

            CreateMap<CollectionDto, Collection>()
                .ForMember(dest => dest.LimitedToGroups, mo => mo.Ignore())
                .ForMember(dest => dest.CustomerGroups, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.Ignore())
                .ForMember(dest => dest.Stores, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<Collection, CollectionDto>();

        }

        public int Order => 1;
    }
}
