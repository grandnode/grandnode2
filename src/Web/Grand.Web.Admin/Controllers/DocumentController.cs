using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Documents;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Documents)]
    public class DocumentController : BaseAdminController
    {
        private readonly IDocumentViewModelService _documentViewModelService;
        private readonly IDocumentTypeService _documentTypeService;
        private readonly IDocumentService _documentService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWorkContext _workContext;

        public DocumentController(IDocumentViewModelService documentViewModelService,
            IDocumentService documentService,
            IDocumentTypeService documentTypeService,
            ITranslationService translationService,
            ICustomerService customerService,
            ICustomerActivityService customerActivityService,
            IWorkContext workContext)
        {
            _documentViewModelService = documentViewModelService;
            _documentService = documentService;
            _documentTypeService = documentTypeService;
            _translationService = translationService;
            _customerService = customerService;
            _customerActivityService = customerActivityService;
            _workContext = workContext;
        }

        public IActionResult Index() => RedirectToAction("List");

        public IActionResult List() => View(new DocumentListModel());

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListDocuments(DataSourceRequest command, DocumentListModel model)
        {
            if (!string.IsNullOrEmpty(model.CustomerId))
                model.SearchEmail = (await _customerService.GetCustomerById(model.CustomerId))?.Email;

            if (model.Reference > 0 && string.IsNullOrEmpty(model.ObjectId))
                return Json(new DataSourceResult {
                    Data = Enumerable.Empty<DocumentModel>(),
                    Total = 0
                });

            var documents = await _documentViewModelService.PrepareDocumentListModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult {
                Data = documents.documetListModel,
                Total = documents.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> CreateDocument(SimpleDocumentModel simpleModel)
        {
            var model = await _documentViewModelService.PrepareDocumentModel(null, null, simpleModel);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateDocument(DocumentModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var document = await _documentViewModelService.InsertDocument(model);

                Success(_translationService.GetResource("Admin.Documents.Document.Added"));
                return continueEditing ? RedirectToAction("EditDocument", new { id = document.Id }) : RedirectToAction("List");
            }
            model = await _documentViewModelService.PrepareDocumentModel(model, null, null);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> EditDocument(string id)
        {
            var document = await _documentService.GetById(id);
            if (document == null)
                return RedirectToAction("List");

            var model = await _documentViewModelService.PrepareDocumentModel(null, document, null);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditDocument(DocumentModel model, bool continueEditing)
        {
            var document = await _documentService.GetById(model.Id);
            if (document == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                document = await _documentViewModelService.UpdateDocument(document, model);

                Success(_translationService.GetResource("Admin.Documents.Document.Updated"));
                return continueEditing ? RedirectToAction("EditDocument", new { id = document.Id }) : RedirectToAction("List");
            }

            model = await _documentViewModelService.PrepareDocumentModel(model, document, null);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            var document = await _documentService.GetById(id);
            if (document == null)
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                await _documentViewModelService.DeleteDocument(document);

                Success(_translationService.GetResource("Admin.Documents.Document.Deleted"));
                return RedirectToAction("List");
            }
            return RedirectToAction("EditDocument", new { id = id });
        }

        #region Document type

        public IActionResult Types() => View();

        [PermissionAuthorizeAction(PermissionActionName.List)]
        public async Task<IActionResult> ListType()
        {
            var types = await _documentTypeService.GetAll();
            var gridModel = new DataSourceResult {
                Data = types,
                Total = types.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public IActionResult CreateType()
        {
            var model = new DocumentTypeModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> CreateType(DocumentTypeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var documenttype = model.ToEntity();
                await _documentTypeService.Insert(documenttype);

                //activity log
                _ = _customerActivityService.InsertActivity("AddNewDocumentType", documenttype.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.AddNewDocumentType"), documenttype.Name);

                Success(_translationService.GetResource("Admin.Documents.Type.Added"));
                return continueEditing ? RedirectToAction("EditType", new { id = documenttype.Id }) : RedirectToAction("Types");
            }
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> EditType(string id)
        {
            var documentType = await _documentTypeService.GetById(id);
            if (documentType == null)
                return RedirectToAction("Types");

            var model = documentType.ToModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> EditType(DocumentTypeModel model, bool continueEditing)
        {
            var documentType = await _documentTypeService.GetById(model.Id);
            if (documentType == null)
                return RedirectToAction("Types");

            if (ModelState.IsValid)
            {
                documentType = model.ToEntity(documentType);
                await _documentTypeService.Update(documentType);

                //activity log
                _ = _customerActivityService.InsertActivity("EditDocumentType", documentType.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.EditDocumentType"), documentType.Name);

                Success(_translationService.GetResource("Admin.Documents.Type.Updated"));
                return continueEditing ? RedirectToAction("EditType", new { id = documentType.Id }) : RedirectToAction("Types");
            }

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteType(string id)
        {
            var documentType = await _documentTypeService.GetById(id);
            if (documentType == null)
                return RedirectToAction("Types");

            if (ModelState.IsValid)
            {
                await _documentTypeService.Delete(documentType);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteDocumentType", documentType.Id,
                    _workContext.CurrentCustomer, HttpContext.Connection?.RemoteIpAddress?.ToString(),
                    _translationService.GetResource("ActivityLog.DeleteDocumentType"), documentType.Name);

                Success(_translationService.GetResource("Admin.Documents.Type.Deleted"));
                return RedirectToAction("Types");
            }
            return RedirectToAction("Edit", new { id = id });
        }
        #endregion


    }
}
