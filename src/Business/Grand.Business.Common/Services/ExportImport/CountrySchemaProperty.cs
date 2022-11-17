using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Common.Services.ExportImport
{
    public class CountrySchemaProperty : ISchemaProperty<CountryStates>
    {
        public virtual PropertyByName<CountryStates>[] GetProperties()
        {
            var properties = new[]
            {
                new PropertyByName<CountryStates>("Country", p => p.TwoLetterIsoCode),
                new PropertyByName<CountryStates>("Name", p => p.StateProvinceName),
                new PropertyByName<CountryStates>("Abbreviation", p => p.Abbreviation),
                new PropertyByName<CountryStates>("DisplayOrder", p => p.DisplayOrder),
                new PropertyByName<CountryStates>("Published", p => p.Published)
            };
            return properties;
        }
    }

    public class CountryStates
    {
        public string TwoLetterIsoCode { get; set; }
        public string StateProvinceName { get; set; }
        public string Abbreviation { get; set; }
        public int DisplayOrder { get; set; }
        public bool Published { get; set; }
    }
}
