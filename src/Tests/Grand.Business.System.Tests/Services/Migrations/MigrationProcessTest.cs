using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.System.Services.Migrations;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Services.Migrations
{
    [TestClass()]
    public class MigrationProcessTest
    {
        private Mock<IRepository<MigrationDb>> _repository;
        private MigrationProcess _service;
        private Mock<IDatabaseContext> _dbContext;
        private Mock<ILogger> _loggerMock;
        private IServiceProvider _serviceProvider;

        [TestInitialize]
        public void Init()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider = serviceProvider.Object;

            _repository = new Mock<IRepository<MigrationDb>>();
            _dbContext = new Mock<IDatabaseContext>();
            _loggerMock = new Mock<ILogger>();
            _service = new MigrationProcess(_dbContext.Object, _serviceProvider, _loggerMock.Object, _repository.Object);
        }
        //TODO
        /*
        [TestMethod]
        public void RunMigrationProcess_CheckInsertMigrationDb()
        {
            _service.RunMigrationProcess();
            _repository.Verify(c => c.Insert(It.IsAny<MigrationDb>()), Times.AtLeastOnce);
        }
        */

    }
}
