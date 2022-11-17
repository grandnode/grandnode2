using AutoMapper;
using Grand.Business.Catalog.Services.ExportImport.Dto;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport.Mapper
{
    public class CategoryProfile : Profile, IAutoMapperProfile
    {
        public CategoryProfile()
        {
            CreateMap<CategoryDto, Category>()
                .ForMember(x => x.UpdatedOnUtc, opt => opt.MapFrom(o => DateTime.UtcNow))
                .ForMember(x => x.PageSize, opt => opt.Condition(z => z.PageSize.HasValue))
                .ForMember(x => x.AllowCustomersToSelectPageSize, opt => opt.Condition(z => z.AllowCustomersToSelectPageSize.HasValue))
                .ForMember(x => x.ShowOnHomePage, opt => opt.Condition(z => z.ShowOnHomePage.HasValue))
                .ForMember(x => x.ShowOnHomePage, opt => opt.Condition(z => z.ShowOnHomePage.HasValue))
                .ForMember(x => x.IncludeInMenu, opt => opt.Condition(z => z.IncludeInMenu.HasValue))
                .ForMember(x => x.DefaultSort, opt => opt.Condition(z => z.DefaultSort.HasValue))
                .ForMember(x => x.FeaturedProductsOnHomePage, opt => opt.Condition(z => z.FeaturedProductsOnHomePage.HasValue))
                .ForMember(x => x.ShowOnSearchBox, opt => opt.Condition(z => z.ShowOnSearchBox.HasValue))
                .ForMember(x => x.SearchBoxDisplayOrder, opt => opt.Condition(z => z.SearchBoxDisplayOrder.HasValue))
                .ForMember(x => x.HideOnCatalog, opt => opt.Condition(z => z.HideOnCatalog.HasValue))
                .ForMember(x => x.Published, opt => opt.Condition(z => z.Published.HasValue))
                .ForMember(x => x.DisplayOrder, opt => opt.Condition(z => z.DisplayOrder.HasValue));
        }

        public int Order => 0;
    }
}
