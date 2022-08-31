namespace Grand.Business.Core.Interfaces.System.ExportImport
{
    /// <summary>
    /// Import manager interface
    /// </summary>
    public partial interface IImportManager
    {
        /// <summary>
        /// Import products from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        Task ImportProductsFromXlsx(Stream stream);

        /// <summary>
        /// Import newsletter subscribers from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="currentStoreId">Current store ident</param>
        /// <returns>Number of imported subscribers</returns>
        Task<int> ImportNewsletterSubscribersFromTxt(Stream stream, string currentStoreId);

        /// <summary>
        /// Import country states from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        Task ImportCountryStatesFromXlsx(Stream stream);

        /// <summary>
        /// Import brand from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        Task ImportBrandFromXlsx(Stream stream);

        /// <summary>
        /// Import collections from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        Task ImportCollectionFromXlsx(Stream stream);

        /// <summary>
        /// Import categories from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        Task ImportCategoryFromXlsx(Stream stream);
    }
}
