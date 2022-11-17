using Grand.Business.Core.Utilities.ExportImport;

namespace Grand.Business.Core.Interfaces.ExportImport
{
    public interface ISchemaProperty<T>
    {
        PropertyByName<T>[] GetProperties();
    }
}
