using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class InteractiveFormProfile : Profile, IAutoMapperProfile
    {
        public InteractiveFormProfile()
        {
            CreateMap<InteractiveForm, InteractiveFormModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableEmailAccounts, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableTokens, mo => mo.Ignore());

            CreateMap<InteractiveFormModel, InteractiveForm>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.FormAttributes, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));

            CreateMap<InteractiveForm.FormAttribute, InteractiveFormAttributeModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<InteractiveFormAttributeModel, InteractiveForm.FormAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));

            CreateMap<InteractiveForm.FormAttributeValue, InteractiveFormAttributeValueModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<InteractiveFormAttributeValueModel, InteractiveForm.FormAttributeValue>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}