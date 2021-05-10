using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Documents;
using Grand.Domain.Documents;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetDocumentsHandler : IRequestHandler<GetDocuments, DocumentsModel>
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentTypeService _documentTypeService;
        private readonly ITranslationService _translationService;
        private readonly DocumentSettings _documentSettings;

        public GetDocumentsHandler(IDocumentService documentService,
            IDocumentTypeService documentTypeService,
            ITranslationService translationService,
            DocumentSettings documentSettings)
        {
            _documentService = documentService;
            _documentTypeService = documentTypeService;
            _translationService = translationService;
            _documentSettings = documentSettings;
        }

        public async Task<DocumentsModel> Handle(GetDocuments request, CancellationToken cancellationToken)
        {
            if (request.Command.PageSize <= 0) request.Command.PageSize = _documentSettings.PageSize;
            if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;

            var model = new DocumentsModel
            {
                CustomerId = request.Customer.Id
            };
            var documents = await _documentService.GetAll(request.Customer.Id,
                pageIndex: request.Command.PageNumber - 1,
                pageSize: request.Command.PageSize);
            model.PagingContext.LoadPagedList(documents);
            foreach (var item in documents.Where(x => x.Published).OrderBy(x => x.DisplayOrder))
            {
                var doc = new Grand.Web.Models.Customer.Document
                {
                    Id = item.Id,
                    Amount = item.TotalAmount,
                    OutstandAmount = item.OutstandAmount,
                    Link = item.Link,
                    Name = item.Name,
                    Number = item.Number,
                    Quantity = item.Quantity,
                    Status = item.StatusId.GetTranslationEnum(_translationService, request.Language.Id),
                    Description = item.Description,
                    DocDate = item.DocDate,
                    DueDate = item.DueDate,
                    DocumentType = (await _documentTypeService.GetById(item.DocumentTypeId))?.Name,
                    DownloadId = item.DownloadId
                };
                model.DocumentList.Add(doc);
            }
            return model;
        }
    }
}
