using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Services;

public class OrderNoteViewModelService(
    IOrderService orderService,
    IDownloadService downloadService,
    IDateTimeService dateTimeService,
    IOrderMessageProviderService orderMessageProviderService)
    : IOrderNoteViewModelService
{
    public virtual async Task<IList<OrderModel.OrderNote>> PrepareOrderNotes(Order order)
    {
        //order notes
        var orderNoteModels = new List<OrderModel.OrderNote>();
        foreach (var orderNote in (await orderService.GetOrderNotes(order.Id))
                 .OrderByDescending(on => on.CreatedOnUtc))
        {
            var download = string.IsNullOrEmpty(orderNote.DownloadId)
                ? null
                : await downloadService.GetDownloadById(orderNote.DownloadId);
            orderNoteModels.Add(new OrderModel.OrderNote {
                Id = orderNote.Id,
                OrderId = order.Id,
                DownloadId = string.IsNullOrEmpty(orderNote.DownloadId) ? "" : orderNote.DownloadId,
                DownloadGuid = download?.DownloadGuid ?? Guid.Empty,
                DisplayToCustomer = orderNote.DisplayToCustomer,
                Note = orderNote.Note,
                CreatedOn = dateTimeService.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = orderNote.CreatedByCustomer
            });
        }

        return orderNoteModels;
    }

    public virtual async Task InsertOrderNote(Order order, string downloadId, bool displayToCustomer,
        string message)
    {
        //the id comes from the request; a file uploaded for another order is not attached
        if (!string.IsNullOrEmpty(downloadId) &&
            !IsAttachmentOf(await downloadService.GetDownloadById(downloadId), order))
            downloadId = "";

        var orderNote = new OrderNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            DownloadId = downloadId,
            OrderId = order.Id
        };
        await orderService.InsertOrderNote(orderNote);

        //new order notification
        if (displayToCustomer)
            //email
            await orderMessageProviderService.SendNewOrderNoteAddedCustomerMessage(order, orderNote);
    }

    public virtual async Task DeleteOrderNote(Order order, string id)
    {
        var orderNote = (await orderService.GetOrderNotes(order.Id)).FirstOrDefault(on => on.Id == id);
        if (orderNote == null)
            throw new ArgumentException("No order note found with the specified id");

        await orderService.DeleteOrderNote(orderNote);

        //delete an old "attachment" file
        if (!string.IsNullOrEmpty(orderNote.DownloadId))
        {
            var attachment = await downloadService.GetDownloadById(orderNote.DownloadId);
            if (IsAttachmentOf(attachment, order))
                await downloadService.DeleteDownload(attachment);
        }
    }

    //the notes form uploads with DownloadType.Order and the order id as the reference
    private static bool IsAttachmentOf(Download download, Order order) =>
        download is { DownloadType: DownloadType.Order } && download.ReferenceId == order.Id;
}
