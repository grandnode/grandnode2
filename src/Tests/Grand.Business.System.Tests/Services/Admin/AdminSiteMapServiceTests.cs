using Grand.Business.System.Services.Admin;
using Grand.Domain.Admin;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Services.Admin
{
    [TestClass]
    public class AdminSiteMapServiceTests
    {
        private Mock<IRepository<AdminSiteMap>> _repositoryMock;
        private Mock<ICacheBase> _cacheMock;
        private AdminSiteMapService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<AdminSiteMap>>();
            _cacheMock = new Mock<ICacheBase>();
            _service = new AdminSiteMapService(_repositoryMock.Object,_cacheMock.Object);
        }

        [TestMethod]
        public async Task GetSiteMap_InvokeExpectedMethods()
        {
            _cacheMock.Setup(c => c.GetAsync<List<AdminSiteMap>>(It.IsAny<string>(), It.IsAny<Func<Task<List<AdminSiteMap>>>>()));
            var result = await _service.GetSiteMap();
            _cacheMock.Verify(c => c.GetAsync<List<AdminSiteMap>>(It.IsAny<string>(), It.IsAny<Func<Task<List<AdminSiteMap>>>>()), Times.Once);
        }
    }
}
