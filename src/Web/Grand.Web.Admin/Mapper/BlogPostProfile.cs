using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Blogs;
using Grand.Web.Admin.Models.Blogs;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class BlogPostProfile : Profile, IAutoMapperProfile
    {
        public BlogPostProfile()
        {
            CreateMap<BlogPost, BlogPostModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)))
                .ForMember(dest => dest.Comments, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore());

            CreateMap<BlogPostModel, BlogPost>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.CommentCount, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}