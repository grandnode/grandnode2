﻿using Grand.Business.Marketing.Services.Documents;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Documents;
using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Documents
{
    [TestClass()]
    public class DocumentTypeServiceTests
    {
        private Mock<IRepository<DocumentType>> _documentTypeRepositoryMock;
        private DocumentTypeService _documentTypeService;
        private Mock<IMediator> _mediatorMock;
        private List<DocumentType> _expected;
        private IQueryable<DocumentType> _expectedQueryable;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "";

            _mediatorMock = new Mock<IMediator>();
            _documentTypeRepositoryMock = new Mock<IRepository<DocumentType>>();
            var _mongoQueryableMock = new Mock<MongoRepository<DocumentType>>();
            _expected = new List<DocumentType>
            {
                new DocumentType() {Name = "name1", Description = "t1", DisplayOrder = 0},
                new DocumentType() {Name = "name2", Description = "t2", DisplayOrder = 1}
            };
            _expectedQueryable = _expected.AsQueryable();
            //_mongoQueryableMock.Setup(x => x.ElementType).Returns(_expectedQueryable.ElementType);
            //_mongoQueryableMock.Setup(x => x.Expression).Returns(_expectedQueryable.Expression);
            //_mongoQueryableMock.Setup(x => x.Provider).Returns(_expectedQueryable.Provider);
            //_mongoQueryableMock.Setup(x => x.GetEnumerator()).Returns(_expectedQueryable.GetEnumerator());

            _documentTypeRepositoryMock.Setup(x => x.Table).Returns(_mongoQueryableMock.Object.Table);
            _documentTypeService = new DocumentTypeService(_documentTypeRepositoryMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task DeleteDocumentType_NullArgument_ThrowException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _documentTypeService.Delete(null), "documentType");
        }

        [TestMethod()]
        public async Task DeleteDocumentType_ValidArgument()
        {
            await _documentTypeService.Delete(new DocumentType());
            _documentTypeRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<DocumentType>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<DocumentType>>(), default), Times.Once);
        }

        [TestMethod]
        public async Task GetDocumentTypeById()
        {
            await _documentTypeService.GetById("id");
            _documentTypeRepositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task InsertDocumentType_NullArgument_ThrowException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _documentTypeService.Insert(null), "documentType");
        }

        [TestMethod()]
        public async Task InsertDocumentType_ValidArgument()
        {
            await _documentTypeService.Insert(new DocumentType());
            _documentTypeRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<DocumentType>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<DocumentType>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateDocumentType_ValidArgument()
        {
            await _documentTypeService.Update(new DocumentType());
            _documentTypeRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<DocumentType>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<DocumentType>>(), default), Times.Once);
        }

        [TestMethod()]
        public void UpdateDocumentType_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _documentTypeService.Update(null), "documentType");
        }
    }
}
