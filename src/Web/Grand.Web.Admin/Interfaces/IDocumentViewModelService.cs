using Grand.Domain.Documents;
using Grand.Web.Admin.Models.Documents;

namespace Grand.Web.Admin.Interfaces
{
    public interface IDocumentViewModelService
    {
        Task<(IEnumerable<DocumentModel> documetListModel, int totalCount)> PrepareDocumentListModel(DocumentListModel model, int pageIndex, int pageSize);
        Task<DocumentModel> PrepareDocumentModel(DocumentModel documentModel, Document document, SimpleDocumentModel simpleModel);
        Task<Document> InsertDocument(DocumentModel model);
        Task<Document> UpdateDocument(Document document, DocumentModel model);
        Task DeleteDocument(Document document);
    }
}
