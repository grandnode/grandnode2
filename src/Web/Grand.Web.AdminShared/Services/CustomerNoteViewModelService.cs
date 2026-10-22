using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Services;

public class CustomerNoteViewModelService(
    ICustomerNoteService customerNoteService,
    ICustomerService customerService,
    IDownloadService downloadService,
    IMessageProviderService messageProviderService,
    IDateTimeService dateTimeService,
    IContextAccessor contextAccessor)
    : ICustomerNoteViewModelService
{
    public virtual async Task<IList<CustomerModel.CustomerNote>> PrepareCustomerNoteList(string customerId)
    {
        var customerNoteModels = new List<CustomerModel.CustomerNote>();
        foreach (var customerNote in (await customerNoteService.GetCustomerNotes(customerId))
                 .OrderByDescending(on => on.CreatedOnUtc))
        {
            var download = !string.IsNullOrEmpty(customerNote.DownloadId)
                ? await downloadService.GetDownloadById(customerNote.DownloadId)
                : null;
            customerNoteModels.Add(new CustomerModel.CustomerNote {
                Id = customerNote.Id,
                CustomerId = customerId,
                DownloadId = string.IsNullOrEmpty(customerNote.DownloadId) ? "" : customerNote.DownloadId,
                DownloadGuid = download?.DownloadGuid ?? Guid.Empty,
                DisplayToCustomer = customerNote.DisplayToCustomer,
                Title = customerNote.Title,
                Note = customerNote.Note,
                CreatedOn = dateTimeService.ConvertToUserTime(customerNote.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return customerNoteModels;
    }

    public virtual async Task<CustomerNote> InsertCustomerNote(string customerId, string downloadId,
        bool displayToCustomer, string title, string message)
    {
        var customerNote = new CustomerNote {
            DisplayToCustomer = displayToCustomer,
            Title = title,
            Note = message,
            DownloadId = downloadId,
            CustomerId = customerId
        };
        await customerNoteService.InsertCustomerNote(customerNote);

        //new customer note notification
        if (displayToCustomer)
        {
            //email
            await messageProviderService.SendNewCustomerNoteMessage(customerNote,
                await customerService.GetCustomerById(customerId), contextAccessor.StoreContext.CurrentStore,
                contextAccessor.WorkContext.WorkingLanguage.Id);
        }

        return customerNote;
    }

    public virtual async Task DeleteCustomerNote(string id, string customerId)
    {
        var customerNote = await customerNoteService.GetCustomerNote(id);
        if (customerNote == null || customerNote.CustomerId != customerId)
            throw new ArgumentException("No customer note found with the specified id");

        await customerNoteService.DeleteCustomerNote(customerNote);

        //delete an old "attachment" file
        if (!string.IsNullOrEmpty(customerNote.DownloadId))
        {
            var attachment = await downloadService.GetDownloadById(customerNote.DownloadId);
            if (attachment != null)
                await downloadService.DeleteDownload(attachment);
        }
    }
}
