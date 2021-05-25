namespace Grand.Domain.Data
{
    /// <summary>
    /// Data settings helper
    /// </summary>
    public static class DataSettingsHelper
    {
        private static bool? _databaseIsInstalled;
        private static string _connectionString;
        /// <summary>
        /// Returns a value indicating whether database is already installed
        /// </summary>
        /// <returns></returns>
        public static bool DatabaseIsInstalled()
        {
            if (!_databaseIsInstalled.HasValue)
            {
                var manager = new DataSettingsManager();
                var settings = manager.LoadSettings();
                _databaseIsInstalled = settings != null && !string.IsNullOrEmpty(settings.ConnectionString);
                if (!string.IsNullOrEmpty(settings.ConnectionString))
                    _connectionString = settings.ConnectionString;
            }
            return _databaseIsInstalled.Value;
        }
        public static void InitConnectionString()
        {
            var manager = new DataSettingsManager();
            var settings = manager.LoadSettings();
            if (!string.IsNullOrEmpty(settings.ConnectionString))
                _connectionString = settings.ConnectionString;
        }
        public static string ConnectionString()
        {
            return _connectionString;
        }

        public static void ResetCache()
        {
            _databaseIsInstalled = false;
        }

    }
}
