﻿using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Documents;

namespace Grand.Business.Core.Interfaces.Marketing.Documents
{
    public interface IDocumentService
    {
        /// <summary>
        /// Gets a document 
        /// </summary>
        /// <param name="id">document identifier</param>
        /// <returns>Document</returns>
        Task<Document> GetById(string id);

        /// <summary>
        /// Gets all documents
        /// </summary>
        /// <returns>Documents</returns>
        Task<IPagedList<Document>> GetAll(string name = "", string number = "", string email = "", string username = "",
            Reference reference = Reference.None, string objectId = "", string seId = "", int status = -1, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Insert a document
        /// </summary>
        /// <param name="document">Document</param>
        Task Insert(Document document);

        /// <summary>
        /// Update a document type
        /// </summary>
        /// <param name="document">Document</param>
        Task Update(Document document);

        /// <summary>
        /// Delete a document type
        /// </summary>
        /// <param name="document">Document</param>
        Task Delete(Document document);
    }
}
