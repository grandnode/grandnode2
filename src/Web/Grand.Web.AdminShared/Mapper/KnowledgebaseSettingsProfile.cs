using AutoMapper;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class KnowledgebaseSettingsProfile : Profile, IAutoMapperProfile
{
    public KnowledgebaseSettingsProfile()
    {
        CreateMap<KnowledgebaseSettings, ContentSettingsModel.KnowledgebaseSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<ContentSettingsModel.KnowledgebaseSettingsModel, KnowledgebaseSettings>();
    }

    public int Order => 0;
}