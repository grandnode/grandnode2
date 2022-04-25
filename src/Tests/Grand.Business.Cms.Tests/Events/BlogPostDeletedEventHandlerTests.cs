using Grand.Business.Cms.Events;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Events
{
    [TestClass()]
    public class BlogPostDeletedEventHandlerTests
    {
        private Mock<ISlugService> _slugServiceMock;
        private BlogPostDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _slugServiceMock = new Mock<ISlugService>();
            _handler = new BlogPostDeletedEventHandler(_slugServiceMock.Object);
        }

        [TestMethod()]
        public async Task Handle_InvokeSlugService()
        {
            await _handler.Handle(new EntityDeleted<BlogPost>(new BlogPost()), default);
            _slugServiceMock.Verify(c => c.DeleteEntityUrl(It.IsAny<EntityUrl>()), Times.Once);
        }
    }
}
