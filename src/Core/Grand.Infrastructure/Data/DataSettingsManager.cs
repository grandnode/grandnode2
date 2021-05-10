using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Infrastructure.Data
{
    /// <summary>
    /// Manager of data settings (connection string)
    /// </summary>
    public partial class DataSettingsManager
    {

        private DataSettings _dataSettings;

        /// <summary>
        /// Load settings
        /// </summary>
        public virtual DataSettings LoadSettings(bool reloadSettings = false)
        {
            if (!reloadSettings && _dataSettings != null)
                return _dataSettings;

            if (!File.Exists(CommonPath.SettingsPath))
                return new DataSettings();

            var connectionString = File.ReadLines(CommonPath.SettingsPath).FirstOrDefault();
            _dataSettings = new DataSettings() { DataConnectionString = connectionString };
            return _dataSettings;

        }

        /// <summary>
        /// Save settings to a file
        /// </summary>
        /// <param name="settings"></param>
        public virtual async Task SaveSettings(DataSettings settings)
        {
            var filePath = CommonPath.SettingsPath;
            if (!File.Exists(filePath))
            {
                using FileStream fs = File.Create(filePath);
            }
            await File.WriteAllTextAsync(filePath, settings.DataConnectionString);
        }
    }
}
