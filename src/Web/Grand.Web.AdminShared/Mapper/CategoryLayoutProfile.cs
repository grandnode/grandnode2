using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Layouts;

namespace Grand.Web.AdminShared.Mapper;

public class CategoryLayoutProfile : Profile, IAutoMapperProfile
{
    public CategoryLayoutProfile()
    {
        CreateMap<CategoryLayout, CategoryLayoutModel>();
        CreateMap<CategoryLayoutModel, CategoryLayout>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}