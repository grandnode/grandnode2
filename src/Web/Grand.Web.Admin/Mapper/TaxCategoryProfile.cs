using AutoMapper;
using Grand.Domain.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Mapper
{
    public class TaxCategoryProfile : Profile, IAutoMapperProfile
    {
        public TaxCategoryProfile()
        {
            CreateMap<TaxCategory, TaxCategoryModel>();
            CreateMap<TaxCategoryModel, TaxCategory>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}