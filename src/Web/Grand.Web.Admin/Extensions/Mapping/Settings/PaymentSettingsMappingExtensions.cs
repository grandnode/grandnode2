using Grand.Domain.Payments;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Payments;

namespace Grand.Web.Admin.Extensions
{
    public static class PaymentSettingsMappingExtensions
    {
        public static PaymentSettingsModel ToModel(this PaymentSettings entity)
        {
            return entity.MapTo<PaymentSettings, PaymentSettingsModel>();
        }
        public static PaymentSettings ToEntity(this PaymentSettingsModel model, PaymentSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}