using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Security
{
    [TestClass()]
    public class PermissionServiceTests
    {
        private Mock<IRepository<Permission>> _permissionRepositoryMock;
        private Mock<IRepository<PermissionAction>> _permissionActionRepositoryMock;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IGroupService> _groupServiceMock;
        private Mock<ICacheBase> _cacheMock;
        private PermissionService _service;

        [TestInitialize()]
        public void Init()
        {
            _permissionRepositoryMock = new Mock<IRepository<Permission>>();
            _permissionActionRepositoryMock = new Mock<IRepository<PermissionAction>>();
            _workContextMock = new Mock<IWorkContext>();
            _groupServiceMock = new Mock<IGroupService>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new PermissionService(_permissionRepositoryMock.Object,_permissionActionRepositoryMock.Object,_workContextMock.Object,
                _groupServiceMock.Object,_cacheMock.Object);
        }

        [TestMethod]
        public async Task Authorize_ReturnTrue()
        {
            Permission permission = new Permission() { SystemName = "permistion" };
            var fakeCustomer = new Customer();
            fakeCustomer.Groups.Add("group1");
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(fakeCustomer);
            _cacheMock.Setup(c => c.GetAsync<bool>(It.IsAny<string>(), It.IsAny<Func<Task<bool>>>())).Returns(Task.FromResult(true));
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>() {new CustomerGroup() }));
            Assert.IsTrue(await _service.Authorize(permission));
        }

        [TestMethod()]
        public async Task Authorize_NullPermission_ReturnFalse()
        {
            Permission permission = null;
            var fakeCustomer = new Customer();
            fakeCustomer.Groups.Add("group1");
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(fakeCustomer);
            _cacheMock.Setup(c => c.GetAsync<bool>(It.IsAny<string>(), It.IsAny<Func<Task<bool>>>())).Returns(Task.FromResult(true));
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>())).Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup>() { new CustomerGroup() }));
            Assert.IsFalse(await _service.Authorize(permission));
        }

        [TestMethod]
        public async Task InsertPermission_InovokeMethods()
        {
            await _service.InsertPermission(new Permission());
            _permissionRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Permission>()),Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task DeletePermission_InovokeMethods()
        {
            await _service.DeletePermission(new Permission());
            _permissionRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Permission>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePermission_InovokeMethods()
        {
            await _service.UpdatePermission(new Permission());
            _permissionRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Permission>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }


        [TestMethod]
        public async Task InsertPermissionAction_InovokeMethods()
        {
            await _service.InsertPermissionAction(new PermissionAction());
            _permissionActionRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<PermissionAction>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task DeletePermissionAction_InovokeMethods()
        {
            await _service.DeletePermissionAction(new PermissionAction());
            _permissionActionRepositoryMock.Verify(c => c.DeleteAsync(It.IsAny<PermissionAction>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
