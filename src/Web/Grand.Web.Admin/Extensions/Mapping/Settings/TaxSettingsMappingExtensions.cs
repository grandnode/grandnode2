using Grand.Domain.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Extensions
{
    public static class TaxSettingsMappingExtensions
    {
        public static TaxSettingsModel ToModel(this TaxSettings entity)
        {
            return entity.MapTo<TaxSettings, TaxSettingsModel>();
        }
        public static TaxSettings ToEntity(this TaxSettingsModel model, TaxSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}