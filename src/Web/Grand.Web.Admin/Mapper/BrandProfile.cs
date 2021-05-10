using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class BrandProfile : Profile, IAutoMapperProfile
    {
        public BrandProfile()
        {
            CreateMap<Brand, BrandModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableBrandLayouts, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableDiscounts, mo => mo.Ignore())
                .ForMember(dest => dest.SelectedDiscountIds, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)))
                .ForMember(dest => dest.AvailableSortOptions, mo => mo.Ignore());

            CreateMap<BrandModel, Brand>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}