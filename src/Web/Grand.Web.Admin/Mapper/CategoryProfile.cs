using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class CategoryProfile : Profile, IAutoMapperProfile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCategoryLayouts, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.Breadcrumb, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableDiscounts, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableSortOptions, mo => mo.Ignore())
                .ForMember(dest => dest.SelectedDiscountIds, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)));

            CreateMap<CategoryModel, Category>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups == null || !x.CustomerGroups.Any() ? false : true))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}