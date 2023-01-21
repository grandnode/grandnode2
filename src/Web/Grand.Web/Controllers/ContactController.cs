using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Common;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Controllers;
using Grand.Web.Events;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class ContactController: BasePublicController
{
    #region Fields

    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IMediator _mediator;
    private readonly CaptchaSettings _captchaSettings;

    #endregion

    #region Constructors

    public ContactController(
        ITranslationService translationService,
        IWorkContext workContext,
        IMediator mediator,
        CaptchaSettings captchaSettings)
    {
        _translationService = translationService;
        _workContext = workContext;
        _mediator = mediator;
        _captchaSettings = captchaSettings;
    }

    #endregion

    //available even when a store is closed
    [ClosedStore(true)]
    public virtual async Task<IActionResult> ContactUs(
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
            Customer = _workContext.CurrentCustomer,
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost, ActionName("ContactUs")]
    [AutoValidateAntiforgeryToken]
    [ValidateCaptcha]
    [ClosedStore(true)]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ContactUsSend(
        [FromServices] StoreInformationSettings storeInformationSettings,
        [FromServices] IPageService pageService,
        ContactUsModel model, bool captchaValid)
    {
        if (storeInformationSettings.StoreClosed)
        {
            var closestorepage = await pageService.GetPageBySystemName("ContactUs");
            if (closestorepage is not { AccessibleWhenStoreClosed: true })
                return RedirectToRoute("StoreClosed");
        }

        //validate CAPTCHA
        if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
        {
            ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
        }

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new ContactUsSendCommand {
                CaptchaValid = captchaValid,
                Model = model,
                Store = _workContext.CurrentStore,
                IpAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString()
            });

            if (result.errors.Any())
            {
                foreach (var item in result.errors)
                {
                    ModelState.AddModelError("", item);
                }
            }
            else
            {
                //notification
                await _mediator.Publish(new ContactUsEvent(_workContext.CurrentCustomer, result.model));

                model = result.model;
                return View(model);
            }
        }

        var model1 = await _mediator.Send(new ContactUsCommand {
            Customer = _workContext.CurrentCustomer,
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore,
            Model = model
        });

        return View(model1);
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> ContactAttributeChange(ContactAttributeChangeModel model)
    {
        var result = await _mediator.Send(new ContactAttributeChangeCommand {
            Attributes = model.Attributes,
            Customer = _workContext.CurrentCustomer,
            Store = _workContext.CurrentStore
        });
        return Json(new {
            enabledattributeids = result.enabledAttributeIds.ToArray(),
            disabledattributeids = result.disabledAttributeIds.ToArray()
        });
    }

    [HttpPost]
    [DenySystemAccount]
    public virtual async Task<IActionResult> UploadFileContactAttribute(string attributeId,
        [FromServices] IDownloadService downloadService,
        [FromServices] IContactAttributeService contactAttributeService)
    {
        var attribute = await contactAttributeService.GetContactAttributeById(attributeId);
        if (attribute is not { AttributeControlType: AttributeControlType.FileUpload })
        {
            return Json(new {
                success = false,
                downloadGuid = Guid.Empty
            });
        }

        var form = await HttpContext.Request.ReadFormAsync();
        var httpPostedFile = form.Files.FirstOrDefault();
        if (httpPostedFile == null)
        {
            return Json(new {
                success = false,
                message = "No file uploaded",
                downloadGuid = Guid.Empty
            });
        }

        var fileBinary = httpPostedFile.GetDownloadBits();

        const string qqFileNameParameter = "qqfilename";
        var fileName = httpPostedFile.FileName;
        if (string.IsNullOrEmpty(fileName) && form.ContainsKey(qqFileNameParameter))
            fileName = form[qqFileNameParameter].ToString();
        //remove path (passed in IE)
        fileName = Path.GetFileName(fileName);

        var contentType = httpPostedFile.ContentType;

        var fileExtension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(fileExtension))
            fileExtension = fileExtension.ToLowerInvariant();

        if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
        {
            var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.ToLowerInvariant()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            if (!allowedFileExtensions.Contains(fileExtension.ToLowerInvariant()))
            {
                return Json(new {
                    success = false,
                    message = _translationService.GetResource("ShoppingCart.ValidationFileAllowed"),
                    downloadGuid = Guid.Empty
                });
            }
        }

        if (attribute.ValidationFileMaximumSize.HasValue)
        {
            //compare in bytes
            var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
            if (fileBinary.Length > maxFileSizeBytes)
            {
                //when returning JSON the mime-type must be set to text/plain
                //otherwise some browsers will pop-up a "Save As" dialog.
                return Json(new {
                    success = false,
                    message = string.Format(_translationService.GetResource("ShoppingCart.MaximumUploadedFileSize"),
                        attribute.ValidationFileMaximumSize.Value),
                    downloadGuid = Guid.Empty
                });
            }
        }

        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadUrl = "",
            DownloadBinary = fileBinary,
            ContentType = contentType,
            //we store filename without extension for downloads
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = fileExtension,
            IsNew = true
        };

        await downloadService.InsertDownload(download);

        //when returning JSON the mime-type must be set to text/plain
        //otherwise some browsers will pop-up a "Save As" dialog.
        return Json(new {
            success = true,
            message = _translationService.GetResource("ContactUs.FileUploaded"),
            downloadUrl = Url.Action("GetFileUpload", "Download", new { downloadId = download.DownloadGuid }),
            downloadGuid = download.DownloadGuid
        });
    }
}