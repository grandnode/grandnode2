using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Common.Services.ExportImport
{
    public class CountrySchemaProperty : ISchemaProperty<CountryStates>
    {
        public virtual async Task<PropertyByName<CountryStates>[]> GetProperties()
        {
            var properties = new[]
            {
                new PropertyByName<CountryStates>("Country", p => p.Country),
                new PropertyByName<CountryStates>("StateProvinceName", p => p.StateProvinceName),
                new PropertyByName<CountryStates>("Abbreviation", p => p.Abbreviation),
                new PropertyByName<CountryStates>("DisplayOrder", p => p.DisplayOrder),
                new PropertyByName<CountryStates>("Published", p => p.Published)
            };
            return await Task.FromResult(properties);
        }
    }
}
