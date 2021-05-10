using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class NewsletterCategoryProfile : Profile, IAutoMapperProfile
    {
        public NewsletterCategoryProfile()
        {
            CreateMap<NewsletterCategory, NewsletterCategoryModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<NewsletterCategoryModel, NewsletterCategory>()
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}