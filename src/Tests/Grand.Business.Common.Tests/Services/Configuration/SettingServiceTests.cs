using Grand.Business.Common.Services.Configuration;
using Grand.Domain.Configuration;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Configuration
{
    [TestClass()]
    public class SettingServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IRepository<Setting>> _repositoryMock;
        private SettingService _service;

        [TestInitialize()]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _repositoryMock = new Mock<IRepository<Setting>>();
            _service = new SettingService(_cacheMock.Object, _repositoryMock.Object) ;
        }

        [TestMethod()]
        public async Task InsertSetting_InvokeExpectedMethods()
        {
            await _service.InsertSetting(new Setting(), true);
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Setting>()), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public void InsertSetting_NullArgument_TrhowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertSetting(null, true));
        }

        [TestMethod()]
        public async Task UpdateSetting_InvokeExpectedMethods()
        {
            await _service.UpdateSetting(new Setting(), true);
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Setting>()), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public void UpdateSetting_NullArgument_TrhowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateSetting(null, true));
        }

        [TestMethod()]
        public async Task DeleteSetting_InvokeExpectedMethods()
        {
            await _service.DeleteSetting(new Setting());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Setting>()), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public void DeleteSetting_NullArgument_TrhowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteSetting(null));
        }
    }
}
