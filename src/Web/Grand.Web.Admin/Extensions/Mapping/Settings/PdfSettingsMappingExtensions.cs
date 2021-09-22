using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class PdfSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.PdfSettingsModel ToModel(this PdfSettings entity)
        {
            return entity.MapTo<PdfSettings, GeneralCommonSettingsModel.PdfSettingsModel>();
        }
        public static PdfSettings ToEntity(this GeneralCommonSettingsModel.PdfSettingsModel model, PdfSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}