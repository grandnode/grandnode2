using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Documents;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services
{
    public class DocumentViewModelService : IDocumentViewModelService
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentTypeService _documentTypeService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly IProductService _productService;
        private readonly IShipmentService _shipmentService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly ICategoryService _categoryService;
        private readonly ICollectionService _collectionService;
        private readonly IVendorService _vendorService;
        private readonly ISalesEmployeeService _salesEmployeeService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IDownloadService _downloadService;
        private readonly IWorkContext _workContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DocumentViewModelService(
            IDocumentService documentService,
            IDocumentTypeService documentTypeService,
            ICustomerService customerService,
            IOrderService orderService,
            ITranslationService translationService,
            IProductService productService,
            IShipmentService shipmentService,
            IMerchandiseReturnService merchandiseReturnService,
            ICategoryService categoryService,
            ICollectionService collectionService,
            IVendorService vendorService,
            ISalesEmployeeService salesEmployeeService,
            ICustomerActivityService customerActivityService,
            IDownloadService downloadService,
            IWorkContext workContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _documentService = documentService;
            _documentTypeService = documentTypeService;
            _customerService = customerService;
            _orderService = orderService;
            _translationService = translationService;
            _productService = productService;
            _shipmentService = shipmentService;
            _merchandiseReturnService = merchandiseReturnService;
            _categoryService = categoryService;
            _collectionService = collectionService;
            _vendorService = vendorService;
            _salesEmployeeService = salesEmployeeService;
            _customerActivityService = customerActivityService;
            _downloadService = downloadService;
            _workContext = workContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public virtual async Task<(IEnumerable<DocumentModel> documetListModel, int totalCount)> PrepareDocumentListModel(DocumentListModel model, int pageIndex, int pageSize)
        {
            var documents = await _documentService.GetAll(name: model.SearchName, number: model.SearchNumber,
                email: model.SearchEmail, reference: (Reference)model.Reference, objectId: model.ObjectId, status: model.StatusId, pageIndex: pageIndex - 1, pageSize: pageSize);

            var documentListModel = new List<DocumentModel>();
            foreach (var x in documents)
            {
                var docModel = x.ToModel();
                documentListModel.Add(docModel);
            }
            return (documentListModel, documents.TotalCount);
        }

        public virtual async Task<DocumentModel> PrepareDocumentModel(DocumentModel documentModel, Document document, SimpleDocumentModel simpleModel)
        {
            var model = documentModel == null ? new DocumentModel() { Published = true } : documentModel;
            if (document != null)
                model = document.ToModel();
            else
            {
                if (simpleModel != null)
                {
                    model.ReferenceId = simpleModel.Reference;
                    model.ObjectId = simpleModel.ObjectId;
                    if (!string.IsNullOrEmpty(simpleModel.CustomerId))
                        model.CustomerEmail = (await _customerService.GetCustomerById(simpleModel.CustomerId))?.Email;

                    if (!string.IsNullOrEmpty(simpleModel.ObjectId))
                        switch (simpleModel.Reference)
                        {
                            case (int)Reference.Order:
                                var order = await _orderService.GetOrderById(simpleModel.ObjectId);
                                if (order != null)
                                {
                                    model.Number = order.OrderNumber.ToString();
                                    model.TotalAmount = order.OrderTotal;
                                    model.OutstandAmount = order.PaymentStatusId == Domain.Payments.PaymentStatus.Paid ? 0 : order.OrderTotal;
                                    model.CurrencyCode = order.CustomerCurrencyCode;
                                    model.Name = string.Format(_translationService.GetResource("Order.Document"), model.Number);
                                    model.DocDate = order.CreatedOnUtc;
                                    model.DueDate = order.CreatedOnUtc;
                                    model.Quantity = 1;
                                    model.Username = $"{order.BillingAddress?.FirstName} {order.BillingAddress?.LastName}";
                                    model.CustomerEmail = order.CustomerEmail;
                                }
                                break;
                            case (int)Reference.Product:
                                var product = await _productService.GetProductById(simpleModel.ObjectId);
                                if (product != null)
                                {
                                    model.Name = product.Name;
                                    model.Number = product.Sku;
                                    model.Quantity = 1;
                                }
                                break;
                            case (int)Reference.Category:
                                var category = await _categoryService.GetCategoryById(simpleModel.ObjectId);
                                if (category != null)
                                {
                                    model.Name = category.Name;
                                    model.Quantity = 1;
                                }
                                break;
                            case (int)Reference.Collection:
                                var collection = await _collectionService.GetCollectionById(simpleModel.ObjectId);
                                if (collection != null)
                                {
                                    model.Name = collection.Name;
                                    model.Quantity = 1;
                                }
                                break;
                            case (int)Reference.Vendor:
                                var vendor = await _vendorService.GetVendorById(simpleModel.ObjectId);
                                if (vendor != null)
                                {
                                    model.Name = vendor.Name;
                                    model.Quantity = 1;
                                }
                                break;
                            case (int)Reference.Shipment:
                                var shipment = await _shipmentService.GetShipmentById(simpleModel.ObjectId);
                                if (shipment != null)
                                {
                                    model.DocDate = shipment.CreatedOnUtc;
                                    model.Number = shipment.ShipmentNumber.ToString();
                                    model.Name = string.Format(_translationService.GetResource("Shipment.Document"), shipment.ShipmentNumber);
                                    var sorder = await _orderService.GetOrderById(shipment.OrderId);
                                    if (sorder != null)
                                    {
                                        model.CustomerId = sorder.CustomerId;
                                        model.CustomerEmail = sorder.CustomerEmail;
                                    }
                                }
                                break;
                            case (int)Reference.MerchandiseReturn:
                                var merchandisereturn = await _merchandiseReturnService.GetMerchandiseReturnById(simpleModel.ObjectId);
                                if (merchandisereturn != null)
                                {
                                    model.DocDate = merchandisereturn.CreatedOnUtc;
                                    model.Number = merchandisereturn.ReturnNumber.ToString();
                                    model.Name = string.Format(_translationService.GetResource("MerchandiseReturns.Document"), merchandisereturn.ReturnNumber);
                                    var sorder = await _orderService.GetOrderById(merchandisereturn.OrderId);
                                    if (sorder != null)
                                    {
                                        model.CustomerId = sorder.CustomerId;
                                        model.CustomerEmail = sorder.CustomerEmail;
                                    }
                                }
                                break;
                        }
                }
            }
            //fill document types
            var types = await _documentTypeService.GetAll();
            foreach (var item in types)
            {
                model.AvailableDocumentTypes.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id
                });
            }

            //fill sales employees
            model.AvailableSelesEmployees.Add(new SelectListItem
            {
                Text = _translationService.GetResource("Admin.Documents.Document.Fields.SeId.None"),
                Value = ""
            });
            var salesEmployees = await _salesEmployeeService.GetAll();
            foreach (var item in salesEmployees.Where(x => x.Active))
            {
                model.AvailableSelesEmployees.Add(new SelectListItem
                {
                    Text = item.Name,
                    Value = item.Id
                });

            }
            return model;
        }

        public virtual async Task<Document> InsertDocument(DocumentModel model)
        {
            if (!string.IsNullOrEmpty(model.CustomerEmail))
                model.CustomerId = (await _customerService.GetCustomerByEmail(model.CustomerEmail))?.Id;
            else
                model.CustomerId = string.Empty;

            var document = model.ToEntity();
            document.CreatedOnUtc = DateTime.UtcNow;

            await _documentService.Insert(document);

            //activity log
            _ = _customerActivityService.InsertActivity("AddNewDocument", document.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.AddNewDocument"), document.Name);

            return document;
        }
        public virtual async Task<Document> UpdateDocument(Document document, DocumentModel model)
        {
            var prevAttachmentId = document.DownloadId;

            if (!string.IsNullOrEmpty(model.CustomerEmail))
            {
                model.CustomerEmail = model.CustomerEmail.ToLowerInvariant();
                model.CustomerId = (await _customerService.GetCustomerByEmail(model.CustomerEmail))?.Id;
            }
            else
                model.CustomerId = string.Empty;

            document = model.ToEntity(document);
            document.UpdatedOnUtc = DateTime.UtcNow;

            await _documentService.Update(document);

            //delete an old "attachment" file (if deleted or updated)
            if (!string.IsNullOrEmpty(prevAttachmentId) && prevAttachmentId != document.DownloadId)
            {
                var prevAttachment = await _downloadService.GetDownloadById(prevAttachmentId);
                if (prevAttachment != null)
                    await _downloadService.DeleteDownload(prevAttachment);
            }

            //activity log
            _ = _customerActivityService.InsertActivity("EditDocument", document.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.EditDocument"), document.Name);

            return document;
        }

        public virtual async Task DeleteDocument(Document document)
        {
            await _documentService.Delete(document);

            //delete an old "attachment" file
            if (!string.IsNullOrEmpty(document.DownloadId))
            {
                var prevAttachment = await _downloadService.GetDownloadById(document.DownloadId);
                if (prevAttachment != null)
                    await _downloadService.DeleteDownload(prevAttachment);
            }

            //activity log
            _ = _customerActivityService.InsertActivity("DeleteDocument", document.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.DeleteDocument"), document.Name);

        }
    }
}
