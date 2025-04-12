using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Layouts;

namespace Grand.Web.AdminShared.Mapper;

public class BrandLayoutProfile : Profile, IAutoMapperProfile
{
    public BrandLayoutProfile()
    {
        CreateMap<BrandLayout, BrandLayoutModel>();
        CreateMap<BrandLayoutModel, BrandLayout>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}