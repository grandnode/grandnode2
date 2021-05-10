using AutoMapper;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Knowledgebase;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class KnowledgebaseCategoryProfile : Profile, IAutoMapperProfile
    {
        public KnowledgebaseCategoryProfile()
        {
            CreateMap<KnowledgebaseCategory, KnowledgebaseCategoryModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<KnowledgebaseCategoryModel, KnowledgebaseCategory>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());
            CreateMap<KnowledgebaseArticle, KnowledgebaseArticleModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<KnowledgebaseArticleModel, KnowledgebaseArticle>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}