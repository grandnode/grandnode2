using AutoMapper;
using Grand.Domain.Blogs;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class BlogSettingsProfile : Profile, IAutoMapperProfile
{
    public BlogSettingsProfile()
    {
        CreateMap<BlogSettings, ContentSettingsModel.BlogSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<ContentSettingsModel.BlogSettingsModel, BlogSettings>();
    }

    public int Order => 0;
}