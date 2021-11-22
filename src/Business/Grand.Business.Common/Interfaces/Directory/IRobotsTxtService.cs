using Grand.Domain.Common;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    public partial interface IRobotsTxtService
    {
        /// <summary>
        /// Get a robots txt
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>RobotsTxt</returns>
        Task<RobotsTxt> GetRobotsTxt(string storeId = "");

        /// <summary>
        /// Insert a RobotsTxt
        /// </summary>
        /// <param name="robotsTxt">RobotsTxt</param>
        Task InsertRobotsTxt(RobotsTxt robotsTxt);

        /// <summary>
        /// Update a RobotsTxt
        /// </summary>
        /// <param name="robotsTxt">RobotsTxt</param>
        Task UpdateRobotsTxt(RobotsTxt robotsTxt);

        /// <summary>
        /// Delete a RobotsTxt
        /// </summary>
        /// <param name="robotsTxt">RobotsTxt</param>
        Task DeleteRobotsTxt(RobotsTxt robotsTxt);
    }
}
