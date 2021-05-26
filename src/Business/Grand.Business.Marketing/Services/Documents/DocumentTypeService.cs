using Grand.Business.Marketing.Interfaces.Documents;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Documents;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Documents
{
    public partial class DocumentTypeService : IDocumentTypeService
    {
        private readonly IRepository<DocumentType> _documentTypeRepository;
        private readonly IMediator _mediator;

        public DocumentTypeService(IRepository<DocumentType> documentTypeRepository, IMediator mediator)
        {
            _documentTypeRepository = documentTypeRepository;
            _mediator = mediator;
        }

        public virtual async Task Delete(DocumentType documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            await _documentTypeRepository.DeleteAsync(documentType);

            //event notification
            await _mediator.EntityDeleted(documentType);
        }

        public virtual async Task<IList<DocumentType>> GetAll()
        {
            var query = from t in _documentTypeRepository.Table
                        orderby t.DisplayOrder
                        select t;
            return await Task.FromResult(query.ToList());
        }

        public virtual Task<DocumentType> GetById(string id)
        {
            return _documentTypeRepository.GetByIdAsync(id);
        }

        public virtual async Task Insert(DocumentType documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            await _documentTypeRepository.InsertAsync(documentType);

            //event notification
            await _mediator.EntityInserted(documentType);
        }

        public virtual async Task Update(DocumentType documentType)
        {
            if (documentType == null)
                throw new ArgumentNullException(nameof(documentType));

            await _documentTypeRepository.UpdateAsync(documentType);

            //event notification
            await _mediator.EntityUpdated(documentType);

        }
    }
}
