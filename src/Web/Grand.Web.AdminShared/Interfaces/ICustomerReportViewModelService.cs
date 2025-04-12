using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerReportViewModelService
{
    Task<CustomerReportsModel> PrepareCustomerReportsModel();
    Task<IList<RegisteredCustomerReportLineModel>> GetReportRegisteredCustomersModel(string storeId);

    Task<(IEnumerable<BestCustomerReportLineModel> bestCustomerReportLineModels, int totalCount)>
        PrepareBestCustomerReportLineModel(BestCustomersReportModel model, int orderBy, int pageIndex, int pageSize);
}