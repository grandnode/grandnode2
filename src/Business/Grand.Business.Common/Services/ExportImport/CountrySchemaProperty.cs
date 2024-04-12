using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Common.Services.ExportImport;

public class CountrySchemaProperty : ISchemaProperty<CountryStatesDto>
{
    public virtual async Task<PropertyByName<CountryStatesDto>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<CountryStatesDto>("Country", p => p.Country),
            new PropertyByName<CountryStatesDto>("StateProvinceName", p => p.StateProvinceName),
            new PropertyByName<CountryStatesDto>("Abbreviation", p => p.Abbreviation),
            new PropertyByName<CountryStatesDto>("DisplayOrder", p => p.DisplayOrder),
            new PropertyByName<CountryStatesDto>("Published", p => p.Published)
        };
        return await Task.FromResult(properties);
    }
}