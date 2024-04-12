using AutoMapper;
using Grand.Business.Core.Dto;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport.Mapper;

public class CategoryProfile : Profile, IAutoMapperProfile
{
    public CategoryProfile()
    {
        CreateMap<CategoryDto, Category>()
            .ForMember(x => x.UpdatedOnUtc, opt => opt.MapFrom(o => DateTime.UtcNow))
            .ForMember(x => x.PageSize, opt => opt.Condition(z => z.PageSize.HasValue))
            .ForMember(x => x.AllowCustomersToSelectPageSize,
                opt => opt.Condition(z => z.AllowCustomersToSelectPageSize.HasValue))
            .ForMember(x => x.ShowOnHomePage, opt => opt.Condition(z => z.ShowOnHomePage.HasValue))
            .ForMember(x => x.IncludeInMenu, opt => opt.Condition(z => z.IncludeInMenu.HasValue))
            .ForMember(x => x.DefaultSort, opt => opt.Condition(z => z.DefaultSort.HasValue))
            .ForMember(x => x.FeaturedProductsOnHomePage,
                opt => opt.Condition(z => z.FeaturedProductsOnHomePage.HasValue))
            .ForMember(x => x.ShowOnSearchBox, opt => opt.Condition(z => z.ShowOnSearchBox.HasValue))
            .ForMember(x => x.SearchBoxDisplayOrder, opt => opt.Condition(z => z.SearchBoxDisplayOrder.HasValue))
            .ForMember(x => x.HideOnCatalog, opt => opt.Condition(z => z.HideOnCatalog.HasValue))
            .ForMember(x => x.Published, opt => opt.Condition(z => z.Published.HasValue))
            .ForMember(x => x.DisplayOrder, opt => opt.Condition(z => z.DisplayOrder.HasValue))
            .ForMember(x => x.Name, opt => opt.Condition(z => z.Name != null))
            .ForMember(x => x.BottomDescription, opt => opt.Condition(z => z.BottomDescription != null))
            .ForMember(x => x.CategoryLayoutId, opt => opt.Condition(z => z.CategoryLayoutId != null))
            .ForMember(x => x.Description, opt => opt.Condition(z => z.Description != null))
            .ForMember(x => x.ExternalId, opt => opt.Condition(z => z.ExternalId != null))
            .ForMember(x => x.Flag, opt => opt.Condition(z => z.Flag != null))
            .ForMember(x => x.FlagStyle, opt => opt.Condition(z => z.FlagStyle != null))
            .ForMember(x => x.Icon, opt => opt.Condition(z => z.Icon != null))
            .ForMember(x => x.MetaDescription, opt => opt.Condition(z => z.MetaDescription != null))
            .ForMember(x => x.MetaKeywords, opt => opt.Condition(z => z.MetaKeywords != null))
            .ForMember(x => x.MetaTitle, opt => opt.Condition(z => z.MetaTitle != null))
            .ForMember(x => x.PageSizeOptions, opt => opt.Condition(z => z.PageSizeOptions != null))
            .ForMember(x => x.ParentCategoryId, opt => opt.Condition(z => z.ParentCategoryId != null))
            .ForMember(x => x.SeName, opt => opt.Condition(z => z.SeName != null));
    }

    public int Order => 0;
}