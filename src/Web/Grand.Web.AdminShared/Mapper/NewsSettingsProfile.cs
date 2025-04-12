using AutoMapper;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

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