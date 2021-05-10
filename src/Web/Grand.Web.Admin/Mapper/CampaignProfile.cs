using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class CampaignProfile : Profile, IAutoMapperProfile
    {
        public CampaignProfile()
        {
            CreateMap<Campaign, CampaignModel>()
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore())
                .ForMember(dest => dest.AllowedTokens, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableStores, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableLanguages, mo => mo.Ignore())
                .ForMember(dest => dest.TestEmail, mo => mo.Ignore());

            CreateMap<CampaignModel, Campaign>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}