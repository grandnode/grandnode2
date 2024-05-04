using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Common;

namespace Grand.Business.Customers.Services.ExportImport;

public class AddressSchemaProperty : ISchemaProperty<Address>
{
    private readonly ICountryService _countryService;

    public AddressSchemaProperty(ICountryService countryService)
    {
        _countryService = countryService;
    }

    public virtual async Task<PropertyByName<Address>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Address>("Email", p => p.Email),
            new PropertyByName<Address>("FirstName", p => p.FirstName),
            new PropertyByName<Address>("LastName", p => p.LastName),
            new PropertyByName<Address>("PhoneNumber", p => p.PhoneNumber),
            new PropertyByName<Address>("FaxNumber", p => p.FaxNumber),
            new PropertyByName<Address>("Address1", p => p.Address1),
            new PropertyByName<Address>("Address2", p => p.Address2),
            new PropertyByName<Address>("City", p => p.City),
            new PropertyByName<Address>("Country",
                p => !string.IsNullOrEmpty(p.CountryId) ? _countryService.GetCountryById(p.CountryId).Result?.Name : "")
        };
        return await Task.FromResult(properties);
    }
}