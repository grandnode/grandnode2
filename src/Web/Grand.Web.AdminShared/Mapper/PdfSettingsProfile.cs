using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class PdfSettingsProfile : Profile, IAutoMapperProfile
{
    public PdfSettingsProfile()
    {
        CreateMap<PdfSettings, GeneralCommonSettingsModel.PdfSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<GeneralCommonSettingsModel.PdfSettingsModel, PdfSettings>();
    }

    public int Order => 0;
}