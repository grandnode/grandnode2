using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICustomerReportViewModelService
    {
        Task<CustomerReportsModel> PrepareCustomerReportsModel();
        Task<IList<RegisteredCustomerReportLineModel>> GetReportRegisteredCustomersModel(string storeId);
        Task<(IEnumerable<BestCustomerReportLineModel> bestCustomerReportLineModels, int totalCount)> PrepareBestCustomerReportLineModel(BestCustomersReportModel model, int orderBy, int pageIndex, int pageSize);
    }
}
