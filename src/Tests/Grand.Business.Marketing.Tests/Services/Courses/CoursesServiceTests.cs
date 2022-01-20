﻿using Grand.Business.Marketing.Services.Courses;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Courses
{
    [TestClass()]
    public class CoursesServiceTests
    {
        private Mock<IRepository<Course>> _courseRepositoryMock;
        private Mock<IRepository<Order>> _orderRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private CatalogSettings _settings;
        private CourseService _courseService;

        [TestInitialize()]
        public void Init()
        {
            _courseRepositoryMock = new Mock<IRepository<Course>>();
            _orderRepositoryMock = new Mock<IRepository<Order>>();
            _mediatorMock = new Mock<IMediator>();
            _settings = new CatalogSettings();
            _courseService = new CourseService(_courseRepositoryMock.Object, _orderRepositoryMock.Object, _mediatorMock.Object);

        }

        [TestMethod()]
        public void Delete_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _courseService.Delete(null), "course");
        }

        [TestMethod()]
        public async Task Delete_ValidArgument_InvokeRepository()
        {
            await _courseService.Delete(new Course());
            _courseRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Course>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Course>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public void Insert_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _courseService.Insert(null), "course");
        }

        [TestMethod()]
        public async Task Insert_ValidArgument_InvokeRepository()
        {
            await _courseService.Insert(new Course());
            _courseRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Course>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Course>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public void Update_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _courseService.Update(null), "course");
        }

        [TestMethod()]
        public async Task Update_ValidArgument_InvokeRepository()
        {
            await _courseService.Update(new Course());
            _courseRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Course>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Course>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task GetById_ValidArgument_InvokeRepository()
        {
            await _courseService.GetById("id");
            _courseRepositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
