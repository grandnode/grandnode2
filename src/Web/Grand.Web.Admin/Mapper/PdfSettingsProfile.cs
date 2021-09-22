using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
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
}