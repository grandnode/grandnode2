using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.MongoDb
{
    [TestClass()]
    public class MongoRepositoryTests
    {
        private IRepository<SampleCollection> _myRepository;

        [TestInitialize()]
        public void Init()
        {
            _myRepository = new MongoDBRepositoryTest<SampleCollection>();

            CommonPath.BaseDirectory = "";
        }

        [TestMethod()]
        public void Insert_MongoRepository_Success()
        {
            var product = new SampleCollection();
            _myRepository.Insert(product);

            Assert.AreEqual(1, _myRepository.Table.Count());
        }

        [TestMethod()]
        public async Task InsertAsync_MongoRepository_Success()
        {
            var product = new SampleCollection();
            await _myRepository.InsertAsync(product);

            Assert.AreEqual(1, _myRepository.Table.Count());
        }
    }
}
