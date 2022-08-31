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
        [TestMethod()]
        public async Task InsertManyAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1" },
            new SampleCollection(){ Id = "2" },
            new SampleCollection(){ Id = "3" },

            };
            await _myRepository.InsertManyAsync(products);

            Assert.IsTrue(_myRepository.Table.Count() == 3);
        }
        [TestMethod()]
        public async Task GetById_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            var p = _myRepository.GetById("1");

            Assert.IsNotNull(p);
        }

        [TestMethod()]
        public async Task GetByIdAsync_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            var p = await _myRepository.GetByIdAsync("1");

            Assert.IsNotNull(p);
        }

        [TestMethod()]
        public async Task ClearAsync_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.ClearAsync();

            Assert.IsTrue(_myRepository.Table.Count() == 0);
        }

        [TestMethod()]
        public async Task AddToSet_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.AddToSet("1", x => x.UserFields,
                new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" });

            await _myRepository.AddToSet("1", x => x.UserFields,
            new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" });

            var p = _myRepository.GetById("1");

            Assert.IsTrue(p.UserFields.Count == 2);
        }

        [TestMethod()]
        public async Task Delete_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            _myRepository.Delete(product);

            var p = _myRepository.GetById("1");

            Assert.IsNull(p);
        }

        [TestMethod()]
        public async Task DeleteAsync_MongoRepository_Success()
        {
            var product = new SampleCollection() { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.DeleteAsync(product);

            var p = _myRepository.GetById("1");

            Assert.IsNull(p);
        }
        [TestMethod()]
        public async Task DeleteManyAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test" },
            new SampleCollection(){ Id = "2", Name = "Test" },
            new SampleCollection(){ Id = "3", Name = "Test2" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.DeleteManyAsync(x => x.Name == "Test");

            Assert.IsTrue(_myRepository.Table.Count() == 1);
        }

        [TestMethod()]
        public async Task GetAllAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test" },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            var p = await _myRepository.GetAllAsync();

            Assert.IsTrue(p.Count() == 3);
        }


        [TestMethod()]
        public async Task Pull_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                    Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection(){ Id = "2", Name = "Test2",
                Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.Pull("1", x => x.Phones, "Phone2");

            var p = _myRepository.GetById("1");

            Assert.IsTrue(p.Phones.Count() == 2);
        }
        [TestMethod()]
        public async Task Pull_Many_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                    Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection(){ Id = "2", Name = "Test2",
                Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.Pull(String.Empty, x => x.Phones, "Phone2");

            var p1 = _myRepository.GetById("1");
            var p2 = _myRepository.GetById("2");
            var p3 = _myRepository.GetById("3");

            Assert.IsTrue(p1.Phones.Count() == 2 && p2.Phones.Count() == 2 && p3.Phones.Count() == 0);
        }

        [TestMethod()]
        public async Task PullFilter_1_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value == "value");

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.UserFields.Count() == 1);
        }

        [TestMethod()]
        public async Task PullFilter_2_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value, "value");

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.UserFields.Count() == 1);
        }
        [TestMethod()]
        public async Task PullFilter_2_Many_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value1", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.PullFilter(String.Empty, x => x.UserFields, x => x.Value, "value");

            var p1 = _myRepository.GetById("1");
            var p2 = _myRepository.GetById("2");

            Assert.IsTrue(p1.UserFields.Count() == 1 && p2.UserFields.Count() == 2);
        }


        [TestMethod()]
        public async Task Update_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            var p1_update = products.FirstOrDefault();
            p1_update.Name = "update";

            _myRepository.Update(p1_update);

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.Name == "update");
        }
        [TestMethod()]
        public async Task UpdateAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            var p1_update = products.FirstOrDefault();
            p1_update.Name = "update";

            await _myRepository.UpdateAsync(p1_update);

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.Name == "update");
        }

        [TestMethod()]
        public async Task UpdateField_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test2" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.UpdateField("1", x => x.Name, "update");

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.Name == "update");
        }

        [TestMethod()]
        public async Task UpdateManyAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);
            await _myRepository.UpdateManyAsync(x => x.Name == "Test",
                UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));

            var pUpdated = _myRepository.Table.Where(x=>x.Name == "UpdateTest");

            Assert.IsTrue(pUpdated.Count() == 2);
        }

        [TestMethod()]
        public async Task UpdateOneAsync_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);
            await _myRepository.UpdateOneAsync(x => x.Name == "Test",
                UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));

            var pUpdated = _myRepository.Table.Where(x => x.Name == "UpdateTest");

            Assert.IsTrue(pUpdated.Count() == 1);
        }
        [TestMethod()]
        public async Task UpdateToSet_MongoRepository_Success()
        {
            var products = new List<SampleCollection>() {
            new SampleCollection(){ Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField>()
                {
                    new Domain.Common.UserField() { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField() { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection(){ Id = "2", Name = "Test" },
            new SampleCollection(){ Id = "3", Name = "Test3" },

            };
            await _myRepository.InsertManyAsync(products);

            await _myRepository.UpdateToSet("1", x => x.UserFields, z => z.Key, "key", new Domain.Common.UserField() { Key = "key", Value = "update", StoreId = "1" });

            var p = _myRepository.GetById("1");

            Assert.IsTrue(p.UserFields.FirstOrDefault(x=>x.Key=="key").Value == "update");
        }
    }
}
