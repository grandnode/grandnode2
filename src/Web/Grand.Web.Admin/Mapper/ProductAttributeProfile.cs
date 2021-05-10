using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class ProductAttributeProfile : Profile, IAutoMapperProfile
    {
        public ProductAttributeProfile()
        {
            CreateMap<ProductAttribute, ProductAttributeModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<ProductAttributeModel, ProductAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}