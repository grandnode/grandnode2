using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Contact;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Events;
using Grand.Web.Models.Contact;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class ContactController : BasePublicController
{
    #region Constructors

    public ContactController(
        ITranslationService translationService,
        IContextAccessor contextAccessor,
        IMediator mediator)
    {
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _mediator = mediator;
    }

    #endregion

    //available even when a store is closed
    [ClosedStore(true)]
    [HttpGet]
    public virtual async Task<IActionResult> Index(
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IPageService pageService)
    {
        if (storeInformationSettings.StoreClosed)
        {
            var closestorepage = await pageService.GetPageBySystemName("ContactUs");
            if (closestorepage is not { AccessibleWhenStoreClosed: true })
                return RedirectToRoute("StoreClosed");
        }

        var model = await _mediator.Send(new ContactUsCommand {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [ClosedStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> Index(
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IPageService pageService,
        ContactUsModel model)
    {
        if (storeInformationSettings.StoreClosed)
        {
            var closeStorePage = await pageService.GetPageBySystemName("ContactUs");
            if (closeStorePage is not { AccessibleWhenStoreClosed: true })
                return RedirectToRoute("StoreClosed");
        }

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new ContactUsSendCommand {
                Model = model,
                IpAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString()
            });

            //notification
            await _mediator.Publish(new ContactUsEvent(_contextAccessor.WorkContext.CurrentCustomer, result));

            return View(result);
        }

        var modelReturn = await _mediator.Send(new ContactUsCommand {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Model = model
        });

        return View(modelReturn);
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ContactAttributeChange(ContactAttributeChangeModel model)
    {
        var result = await _mediator.Send(new ContactAttributeChangeCommand {
            Attributes = model.Attributes,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return Json(new {
            enabledattributeids = result.enabledAttributeIds.ToArray(),
            disabledattributeids = result.disabledAttributeIds.ToArray()
        });
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> UploadFileContactAttribute(string attributeId, IFormFile file,
        [FromServices] IDownloadService downloadService,
        [FromServices] IContactAttributeService contactAttributeService)
    {
        var attribute = await contactAttributeService.GetContactAttributeById(attributeId);
        if (attribute is not { AttributeControlType: AttributeControlType.FileUpload })
            return Json(new
            {
                success = false,
                downloadGuid = Guid.Empty
            });

        if (file == null)
            return Json(new
            {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });

        var fileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType;
        var fileExtension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!allowedFileExtensions.IsAllowedMediaFileType(fileExtension))
                return Json(new
                {
                    success = false,
                    message = _translationService.GetResource("ContactUs.ValidationFileAllowed"),
                    downloadGuid = Guid.Empty
                });
        }

        var fileBinary = file.GetDownloadBits();

        if (attribute.ValidationFileMaximumSize.HasValue)
        {
            //compare in bytes
            var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
            if (fileBinary.Length > maxFileSizeBytes)
                //when returning JSON the mime-type must be set to text/plain
                //otherwise some browsers will pop-up a "Save As" dialog.
                return Json(new
                {
                    success = false,
                    message = string.Format(_translationService.GetResource("ContactUs.MaximumUploadedFileSize"),
                        attribute.ValidationFileMaximumSize.Value),
                    downloadGuid = Guid.Empty
                });
        }

        var download = new Download
        {
            DownloadGuid = Guid.NewGuid(),
            CustomerId = _contextAccessor.WorkContext.CurrentCustomer.Id,
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = contentType,
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            DownloadType = DownloadType.ContactAttribute,
            ReferenceId = attributeId
        };

        await downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new
        {
            success = true,
            message = _translationService.GetResource("ContactUs.FileUploaded"),
            downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
            downloadGuid = download.DownloadGuid
        });
    }

    #region Fields

    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IMediator _mediator;

    #endregion
}