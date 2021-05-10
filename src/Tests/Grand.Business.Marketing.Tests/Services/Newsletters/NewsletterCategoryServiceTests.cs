using Grand.Business.Marketing.Services.Newsteletters;
using Grand.Domain.Data;
using Grand.Domain.Documents;
using Grand.Domain.Messages;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Tests.Services.Newsletters
{
    [TestClass()]
    public class NewsletterCategoryServiceTests
    {
        private Mock<IRepository<NewsletterCategory>> _repoMock;
        private Mock<IMediator> _mediatorMock;
        private NewsletterCategoryService _newsletterCategoryService;

        [TestInitialize()]
        public void Init()
        {
            _repoMock = new Mock<IRepository<NewsletterCategory>>();
            _mediatorMock = new Mock<IMediator>();
            _newsletterCategoryService = new NewsletterCategoryService(_repoMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task DeleteNewsletterCategory_NullArgument_ThrowException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _newsletterCategoryService.DeleteNewsletterCategory(null), "newslettercategory");
        }

        [TestMethod()]
        public async Task DeleteNewsletterCategory_ValidArgument()
        {
            await _newsletterCategoryService.DeleteNewsletterCategory(new NewsletterCategory());
            _repoMock.Verify(c => c.DeleteAsync(It.IsAny<NewsletterCategory>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<NewsletterCategory>>(), default), Times.Once);
        }

        [TestMethod]
        public async Task GetDocumentById()
        {
            await _newsletterCategoryService.GetNewsletterCategoryById("id");
            _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod()]
        public async Task InsertNewsletterCategory_NullArgument_ThrowException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _newsletterCategoryService.InsertNewsletterCategory(null), "newslettercategory");
        }

        [TestMethod()]
        public async Task InsertNewsletterCategory_ValidArgument()
        {
            await _newsletterCategoryService.InsertNewsletterCategory(new NewsletterCategory());
            _repoMock.Verify(c => c.InsertAsync(It.IsAny<NewsletterCategory>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<NewsletterCategory>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateNewsletterCategory_ValidArgument()
        {
            await _newsletterCategoryService.UpdateNewsletterCategory(new NewsletterCategory());
            _repoMock.Verify(c => c.UpdateAsync(It.IsAny<NewsletterCategory>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<NewsletterCategory>>(), default), Times.Once);
        }

        [TestMethod()]
        public void UpdateNewsletterCategory_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _newsletterCategoryService.UpdateNewsletterCategory(null), "newslettercategory");
        }
    }
}
