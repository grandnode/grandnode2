﻿using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Documents;
using MediatR;
using Grand.Domain.Common;

namespace Grand.Business.Marketing.Services.Documents
{
    public class DocumentService : IDocumentService
    {
        private readonly IRepository<Document> _documentRepository;
        private readonly IMediator _mediator;

        public DocumentService(IRepository<Document> documentRepository, IMediator mediator)
        {
            _documentRepository = documentRepository;
            _mediator = mediator;
        }

        public virtual async Task Delete(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.DeleteAsync(document);

            //event notification
            await _mediator.EntityDeleted(document);
        }

        public virtual async Task<IPagedList<Document>> GetAll(string name = "", string number = "", string email = "", string username = "",
            Reference reference = Reference.None, string objectId = "", string seId = "", int status = -1, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from d in _documentRepository.Table
                        select d;

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(m => m.Name != null && m.Name.ToLower().Contains(name.ToLower()));

            if (!string.IsNullOrWhiteSpace(number))
                query = query.Where(m => m.Number != null && m.Number.ToLower().Contains(number.ToLower()));

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(m => m.CustomerEmail == email.ToLower());

            if (!string.IsNullOrWhiteSpace(username))
                query = query.Where(m => m.Username == username);

            if (!string.IsNullOrWhiteSpace(objectId))
                query = query.Where(m => m.ObjectId == objectId);

            if (!string.IsNullOrWhiteSpace(seId))
                query = query.Where(m => m.SeId == seId);

            if (reference > 0)
                query = query.Where(m => m.ReferenceId == reference);

            if (status >= 0)
                query = query.Where(d => d.StatusId == (DocumentStatus)status);

            return await PagedList<Document>.Create(query, pageIndex, pageSize);
        }


        public virtual Task<Document> GetById(string id)
        {
            return _documentRepository.GetByIdAsync(id);
        }

        public virtual async Task Insert(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.InsertAsync(document);

            //event notification
            await _mediator.EntityInserted(document);

        }

        public virtual async Task Update(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            await _documentRepository.UpdateAsync(document);

            //event notification
            await _mediator.EntityUpdated(document);

        }
    }
}
