using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerReportViewModelService
{
    Task<CustomerReportsModel> PrepareCustomerReportsModel();

    /// <param name="storeId">Store scope applied to the underlying registered-customers report.</param>
    /// <param name="vendorId">Intentionally accepted but currently unused: the underlying report has no vendor
    /// dimension. Kept for signature symmetry with <see cref="PrepareBestCustomerReportLineModel" /> and for
    /// forward-compatibility should a vendor dimension ever be added.</param>
    Task<IList<RegisteredCustomerReportLineModel>> GetReportRegisteredCustomersModel(string storeId,
        string vendorId = "");

    Task<(IEnumerable<BestCustomerReportLineModel> bestCustomerReportLineModels, int totalCount)>
        PrepareBestCustomerReportLineModel(BestCustomersReportModel model, int orderBy, int pageIndex,
            int pageSize, string vendorId = "");
}