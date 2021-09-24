using AutoMapper;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
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
}