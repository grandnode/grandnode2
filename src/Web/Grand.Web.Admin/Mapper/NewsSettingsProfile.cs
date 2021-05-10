using AutoMapper;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class NewsSettingsProfile : Profile, IAutoMapperProfile
    {
        public NewsSettingsProfile()
        {
            CreateMap<NewsSettings, ContentSettingsModel.NewsSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<ContentSettingsModel.NewsSettingsModel, NewsSettings>();
        }

        public int Order => 0;
    }
}