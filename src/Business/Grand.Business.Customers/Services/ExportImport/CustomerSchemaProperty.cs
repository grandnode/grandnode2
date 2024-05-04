using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Business.Customers.Services.ExportImport;

public class CustomerSchemaProperty : ISchemaProperty<Customer>
{
    public virtual async Task<PropertyByName<Customer>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Customer>("CustomerId", p => p.Id),
            new PropertyByName<Customer>("CustomerGuid", p => p.CustomerGuid),
            new PropertyByName<Customer>("Email", p => p.Email),
            new PropertyByName<Customer>("Username", p => p.Username),
            new PropertyByName<Customer>("Password", p => p.Password),
            new PropertyByName<Customer>("PasswordFormatId", p => p.PasswordFormatId),
            new PropertyByName<Customer>("PasswordSalt", p => p.PasswordSalt),
            new PropertyByName<Customer>("IsTaxExempt", p => p.IsTaxExempt),
            new PropertyByName<Customer>("AffiliateId", p => p.AffiliateId),
            new PropertyByName<Customer>("VendorId", p => p.VendorId),
            new PropertyByName<Customer>("Active", p => p.Active),
            //attributes
            new PropertyByName<Customer>("FirstName",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName)),
            new PropertyByName<Customer>("LastName",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)),
            new PropertyByName<Customer>("Gender",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender)),
            new PropertyByName<Customer>("Company",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company)),
            new PropertyByName<Customer>("StreetAddress",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress)),
            new PropertyByName<Customer>("StreetAddress2",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2)),
            new PropertyByName<Customer>("ZipPostalCode",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode)),
            new PropertyByName<Customer>("City", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City)),
            new PropertyByName<Customer>("CountryId",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId)),
            new PropertyByName<Customer>("StateProvinceId",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId)),
            new PropertyByName<Customer>("Phone",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone)),
            new PropertyByName<Customer>("Fax", p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax)),
            new PropertyByName<Customer>("VatNumber",
                p => p.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber)),
            new PropertyByName<Customer>("VatNumberStatusId",
                p => p.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId))
        };
        return await Task.FromResult(properties);
    }
}