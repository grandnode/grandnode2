using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class CatalogSettingsProfile : Profile, IAutoMapperProfile
    {
        public CatalogSettingsProfile()
        {
            CreateMap<CatalogSettings, CatalogSettingsModel>()
                .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<CatalogSettingsModel, CatalogSettings>()
                .ForMember(dest => dest.ProductSortingEnumDisabled, mo => mo.Ignore())
                .ForMember(dest => dest.ProductSortingEnumDisplayOrder, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}