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

namespace Grand.Web.Commands.Handler.Orders;

public class InsertMerchandiseReturnNoteCommandHandler(
    IMerchandiseReturnService merchandiseReturnService,
    IDownloadService downloadService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
    MediaSettings mediaSettings,
    LanguageSettings languageSettings)
    : IRequestHandler<InsertMerchandiseReturnNoteCommand, string>
{
    public async Task<string> Handle(InsertMerchandiseReturnNoteCommand request, CancellationToken cancellationToken)
    {
        //a return the store has closed is not written about any more
        if (request.MerchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Completed
            or MerchandiseReturnStatus.Cancelled or MerchandiseReturnStatus.Rejected)
            return "MerchandiseReturns.Notes.Closed";

        var note = request.Note?.Trim();
        if (string.IsNullOrEmpty(note))
            return "MerchandiseReturns.Notes.Required";
        if (note.Length > MerchandiseReturnUploads.MaxNoteLength)
            return "MerchandiseReturns.Notes.TooLong";
        if (request.File != null && !MerchandiseReturnUploads.IsAllowed(request.File,
                FileExtensions.GetAllowedMediaFileTypes(mediaSettings.AllowedFileTypes)))
            return MerchandiseReturnUploads.PhotosInvalid;

        string downloadId = null;
        if (request.File != null)
        {
            var download = MerchandiseReturnUploads.ToDownload(request.File, request.Customer.Id,
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

        await merchandiseReturnMessageProviderService.SendNewMerchandiseReturnNoteAddedStoreOwnerMessage(
            request.MerchandiseReturn, merchandiseReturnNote, request.Order, languageSettings.DefaultAdminLanguageId,
            request.Host);

        return null;
    }
}
