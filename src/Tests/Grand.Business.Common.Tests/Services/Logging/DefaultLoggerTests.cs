using Grand.Business.Common.Services.Logging;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Data;
using Grand.Domain.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Tests.Services.Logging
{
    [TestClass()]
    public class DefaultLoggerTests
    {
        private IRepository<Log> _repository;
        private DefaultLogger _logger;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Log>();
            _logger = new DefaultLogger(_repository);
        }

        [TestMethod()]
        public async Task GetAllLogsTest()
        {
            //Arrange
            await _repository.InsertAsync(new Log());
            await _repository.InsertAsync(new Log());
            await _repository.InsertAsync(new Log());
            //Act
            var result = await _logger.GetAllLogs();
            //Assert
            Assert.IsTrue(result.Any());
        }

        [TestMethod()]
        public async Task GetLogByIdTest()
        {
            //Arrange
            var log = new Log();
            await _repository.InsertAsync(log);
            //Act
            var result = await _logger.GetLogById(log.Id);
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetLogByIdsTest()
        {
            //Arrange
            var log = new Log();
            await _repository.InsertAsync(log);
            //Act
            var result = await _logger.GetLogByIds(new[] { log.Id });
            //Assert
            Assert.IsTrue(result.Any());
        }

        [TestMethod()]
        public async Task InsertLogTest()
        {
            //Act
            await _logger.InsertLog(LogLevel.Debug, "message");
            //Assert
            Thread.Sleep(100);
            Assert.IsTrue(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task DeleteLogTest()
        {
            //Arrange
            var log = new Log();
            await _repository.InsertAsync(log);
            //Act
            await _logger.DeleteLog(log);
            var result = await _logger.GetLogById(log.Id);
            //Assert
            Assert.IsNull(result);
        }

        [TestMethod()]
        public async Task ClearLogTest()
        {
            //Arrange
            await _repository.InsertAsync(new Log());
            await _repository.InsertAsync(new Log());
            await _repository.InsertAsync(new Log());
            //Act
            await _logger.ClearLog();
            //Assert
            Assert.IsFalse(_repository.Table.Any());
        }
    }
}