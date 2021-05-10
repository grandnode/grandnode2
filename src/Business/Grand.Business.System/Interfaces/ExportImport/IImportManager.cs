using System.IO;
using System.Threading.Tasks;

namespace Grand.Business.System.Interfaces.ExportImport
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
        /// <returns>Number of imported subscribers</returns>
        Task<int> ImportNewsletterSubscribersFromTxt(Stream stream);

        /// <summary>
        /// Import states from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported states</returns>
        Task<int> ImportStatesFromTxt(Stream stream);

        /// <summary>
        /// Import collections from XLSX file
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
