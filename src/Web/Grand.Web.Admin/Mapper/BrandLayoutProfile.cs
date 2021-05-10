using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Mapper
{
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
}