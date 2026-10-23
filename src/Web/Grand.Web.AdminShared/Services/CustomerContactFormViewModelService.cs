using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Services;

public class CustomerContactFormViewModelService(
    IContactUsService contactUsService,
    IStoreService storeService,
    IDateTimeService dateTimeService)
    : ICustomerContactFormViewModelService
{
    public virtual async Task<(IEnumerable<ContactFormModel> contactFormModels, int totalCount)>
        PrepareContactFormModel(string customerId, int pageIndex, int pageSize)
    {
        var contactform = await contactUsService.GetAllContactUs(storeId: "", vendorId: "", customerId: customerId,
            pageIndex: pageIndex - 1, pageSize: pageSize);
        var items = new List<ContactFormModel>();
        foreach (var x in contactform)
        {
            var store = await storeService.GetStoreById(x.StoreId);
            var m = x.ToModel();
            m.CreatedOn = dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
            m.Enquiry = "";
            m.Email = m.FullName + " - " + m.Email;
            m.Store = store != null ? store.Shortcut : "-empty-";
            items.Add(m);
        }

        return (items, contactform.TotalCount);
    }
}
