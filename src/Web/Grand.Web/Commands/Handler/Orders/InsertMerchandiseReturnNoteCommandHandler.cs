using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Commands.Handler.Orders;

public class InsertMerchandiseReturnNoteCommandHandler(
    IMerchandiseReturnService merchandiseReturnService,
    IDownloadService downloadService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
    MediaSettings mediaSettings,
    LanguageSettings languageSettings,
    OrderSettings orderSettings,
    ILogger<InsertMerchandiseReturnNoteCommandHandler> logger)
    : IRequestHandler<InsertMerchandiseReturnNoteCommand, string>
{
    public async Task<string> Handle(InsertMerchandiseReturnNoteCommand request, CancellationToken cancellationToken)
    {
        //a store that takes no files saves none, whatever was posted
        var file = orderSettings.MerchandiseReturns_AllowToUploadFiles ? request.File : null;

        //a return the store has closed is not written about any more
        if (request.MerchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Completed
            or MerchandiseReturnStatus.Cancelled or MerchandiseReturnStatus.Rejected)
            return "MerchandiseReturns.Notes.Closed";

        var note = request.Note?.Trim();
        if (string.IsNullOrEmpty(note))
            return "MerchandiseReturns.Notes.Required";
        if (note.Length > MerchandiseReturnUploads.MaxNoteLength)
            return "MerchandiseReturns.Notes.TooLong";
        if (file != null && !MerchandiseReturnUploads.IsAllowed(file,
                FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes)))
            return MerchandiseReturnUploads.PhotosInvalid;

        //a second click on Send, or a loop of posts, does not fill the store's mailbox and the downloads
        var customerNotes = (await merchandiseReturnService.GetMerchandiseReturnNotes(request.MerchandiseReturn.Id))
            .Where(x => x.CreatedByCustomer).ToList();
        if (customerNotes.Count >= MerchandiseReturnUploads.MaxCustomerNotes)
            return "MerchandiseReturns.Notes.TooMany";
        if (customerNotes.Any(x => x.CreatedOnUtc > DateTime.UtcNow - MerchandiseReturnUploads.NoteInterval))
            return "MerchandiseReturns.Notes.TooSoon";

        string downloadId = null;
        if (file != null)
        {
            var download = MerchandiseReturnUploads.ToDownload(file, request.Customer.Id,
                request.MerchandiseReturn.Id);
            await downloadService.InsertDownload(download);
            downloadId = download.Id;
        }

        //kept as typed: the pages and the e-mails encode it when they show it
        var merchandiseReturnNote = new MerchandiseReturnNote {
            MerchandiseReturnId = request.MerchandiseReturn.Id,
            Note = note,
            DownloadId = downloadId,
            DisplayToCustomer = true,
            CreatedByCustomer = true
        };
        await merchandiseReturnService.InsertMerchandiseReturnNote(merchandiseReturnNote);

        //the message is saved and the staff see it with the return: an e-mail that fails is logged, not shown as an
        //error that would make the customer send it again
        try
        {
            await merchandiseReturnMessageProviderService.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(
                request.MerchandiseReturn, merchandiseReturnNote, request.Order,
                languageSettings.DefaultAdminLanguageId, request.Host);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The e-mail of the customer's message on merchandise return {MerchandiseReturnId} " +
                                "was not sent", request.MerchandiseReturn.Id);
        }

        return null;
    }
}
