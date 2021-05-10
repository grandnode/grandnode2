using Grand.Domain.Documents;
using Grand.Web.Admin.Models.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface IDocumentViewModelService
    {
        Task<(IEnumerable<DocumentModel> documetListModel, int totalCount)> PrepareDocumentListModel(DocumentListModel model, int pageIndex, int pageSize);
        Task<DocumentModel> PrepareDocumentModel(DocumentModel documentModel, Document document, SimpleDocumentModel simpleModel);
    }
}
