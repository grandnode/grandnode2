using AutoMapper;
using Grand.Domain.Blogs;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Blogs;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class BlogCategoryProfile : Profile, IAutoMapperProfile
    {
        public BlogCategoryProfile()
        {
            CreateMap<BlogCategory, BlogCategoryModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<BlogCategoryModel, BlogCategory>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.BlogPosts, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}