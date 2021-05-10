using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Mapper
{
    public class ProductLayoutProfile : Profile, IAutoMapperProfile
    {
        public ProductLayoutProfile()
        {
            CreateMap<ProductLayout, ProductLayoutModel>();
            CreateMap<ProductLayoutModel, ProductLayout>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}