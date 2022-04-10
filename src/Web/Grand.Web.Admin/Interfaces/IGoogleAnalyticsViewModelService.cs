using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Interfaces
{
    public partial interface IGoogleAnalyticsViewModelService
    {
        Task<GoogleAnalyticsResultModel> GetDataByGeneral(DateTime startDate, DateTime endDate);
        Task<GoogleAnalyticsResultModel> GetDataByLocalization(DateTime startDate, DateTime endDate);
        Task<GoogleAnalyticsResultModel> GetDataBySource(DateTime startDate, DateTime endDate);
        Task<GoogleAnalyticsResultModel> GetDataByExit(DateTime startDate, DateTime endDate);
        Task<GoogleAnalyticsResultModel> GetDataByDevice(DateTime startDate, DateTime endDate);
    }
}
