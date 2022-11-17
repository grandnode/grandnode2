using Grand.Business.Core.Interfaces.ExportImport;

namespace Grand.Business.Common.Services.ExportImport
{
    public class ExportManager<T> : IExportManager<T> where T : class
    {
        private readonly IExportProvider _exportProvider;
        private readonly ISchemaProperty<T> _schemaProperty;

        public ExportManager(IExportProvider exportProvider, ISchemaProperty<T> schemaProperty)
        {
            _exportProvider = exportProvider;
            _schemaProperty = schemaProperty;
        }

        public virtual byte[] Export(IEnumerable<T> entity)
        {
            return _exportProvider.ExportToByte(_schemaProperty.GetProperties(), entity);
        }
    }
}
