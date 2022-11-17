using AutoMapper;
using Grand.Business.Catalog.Services.ExportImport.Dto;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport.Mapper
{
    public class BrandProfile : Profile, IAutoMapperProfile
    {
        public BrandProfile()
        {
            CreateMap<BrandDto, Brand>()
                .ForMember(x => x.UpdatedOnUtc, opt => opt.MapFrom(o => DateTime.UtcNow))
                .ForMember(x => x.UpdatedOnUtc, opt => opt.MapFrom(o => DateTime.UtcNow))
                .ForMember(x => x.PageSize, opt => opt.Condition(z => z.PageSize.HasValue))
                .ForMember(x => x.AllowCustomersToSelectPageSize, opt => opt.Condition(z => z.AllowCustomersToSelectPageSize.HasValue))
                .ForMember(x => x.ShowOnHomePage, opt => opt.Condition(z => z.ShowOnHomePage.HasValue))
                .ForMember(x => x.ShowOnHomePage, opt => opt.Condition(z => z.ShowOnHomePage.HasValue))
                .ForMember(x => x.IncludeInMenu, opt => opt.Condition(z => z.IncludeInMenu.HasValue))
                .ForMember(x => x.DefaultSort, opt => opt.Condition(z => z.DefaultSort.HasValue))
                .ForMember(x => x.Published, opt => opt.Condition(z => z.Published.HasValue))
                .ForMember(x => x.DisplayOrder, opt => opt.Condition(z => z.DisplayOrder.HasValue));
        }

        public int Order => 0;
    }
}
