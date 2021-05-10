using Grand.Business.Catalog.Services.Categories;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Service.Category
{
    [TestClass()]
    public class CategoryLayoutServiceTest
    {
        private Mock<IRepository<CategoryLayout>> _repostioryMock;
        private Mock<ICacheBase> _cacheMock;
        private Mock<IMediator> _mediatorMock;
        private CategoryLayoutService _categoryLayoutService;

        [TestInitialize()]
        public void Init()
        {
            _repostioryMock = new Mock<IRepository<CategoryLayout>>();
            _cacheMock = new Mock<ICacheBase>();
            _mediatorMock = new Mock<IMediator>();
            _categoryLayoutService = new CategoryLayoutService(_repostioryMock.Object, _cacheMock.Object, _mediatorMock.Object);
        }


        [TestMethod()]
        public async Task DeleteCategoryLayout_ValidArguments_InoveMethods()
        {
            await _categoryLayoutService.DeleteCategoryLayout(new CategoryLayout());
            _repostioryMock.Verify(c => c.DeleteAsync(It.IsAny<CategoryLayout>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CategoryLayout>>(), default(CancellationToken)), Times.Once);
        }


        [TestMethod()]
        public async Task InsertCategoryLayout_ValidArguments_InoveMethods()
        {
            await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
            _repostioryMock.Verify(c => c.InsertAsync(It.IsAny<CategoryLayout>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CategoryLayout>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateCategoryLayout_ValidArguments_InoveMethods()
        {
            await _categoryLayoutService.UpdateCategoryLayout(new CategoryLayout());
            _repostioryMock.Verify(c => c.UpdateAsync(It.IsAny<CategoryLayout>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<CategoryLayout>>(), default(CancellationToken)), Times.Once);
        }
    }
}
