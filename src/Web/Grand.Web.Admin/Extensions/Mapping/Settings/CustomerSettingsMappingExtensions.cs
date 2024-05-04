using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class CustomerSettingsMappingExtensions
{
    public static CustomerSettingsModel.CustomersSettingsModel ToModel(this CustomerSettings entity)
    {
        return entity.MapTo<CustomerSettings, CustomerSettingsModel.CustomersSettingsModel>();
    }

    public static CustomerSettings ToEntity(this CustomerSettingsModel.CustomersSettingsModel model,
        CustomerSettings destination)
    {
        return model.MapTo(destination);
    }
}