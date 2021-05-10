using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.News;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class NewsItemProfile : Profile, IAutoMapperProfile
    {
        public NewsItemProfile()
        {
            CreateMap<NewsItem, NewsItemModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)))
                .ForMember(dest => dest.Comments, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore());

            CreateMap<NewsItemModel, NewsItem>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.NewsComments, mo => mo.Ignore())
                .ForMember(dest => dest.CommentCount, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()));
        }

        public int Order => 0;
    }
}