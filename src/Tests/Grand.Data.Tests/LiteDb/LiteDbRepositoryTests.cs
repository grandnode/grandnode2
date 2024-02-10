using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.LiteDb
{
    [TestClass()]
    public class LiteDbRepositoryTests
    {
        private IRepository<SampleCollection> _myRepository;

        [TestInitialize()]
        public void Init()
        {
            _myRepository = new LiteDBRepositoryMock<SampleCollection>();

            CommonPath.BaseDirectory = "";
        }

        [TestMethod()]
        public void Insert_LiteRepository_Success()
        {
            //Arrange
            var product = new SampleCollection {Id = "1"};
            //Act
            _myRepository.Insert(product);
            //Assert
            Assert.AreEqual(1, _myRepository.Table.Count());
            Assert.IsTrue(_myRepository.Table.FirstOrDefault(x=>x.Id == "1")!.CreatedBy == "user");
        }

        [TestMethod()]
        public async Task InsertAsync_LiteRepository_Success()
        {
            //Arrange
            var product = new SampleCollection { Id = "11"};
            //Act
            await _myRepository.InsertAsync(product);
            var p = _myRepository.GetById("11");
            //Assert
            Assert.IsNotNull(p);
            Assert.AreEqual(1, _myRepository.Table.Count());
            Assert.IsTrue(p.CreatedBy == "user");
        }
        
        [TestMethod()]
        public async Task GetById_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            var p = _myRepository.GetById("1");

            Assert.IsNotNull(p);
        }

        [TestMethod()]
        public async Task GetByIdAsync_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            var p = await _myRepository.GetByIdAsync("1");

            Assert.IsNotNull(p);
        }

        [TestMethod()]
        public async Task GetOneAsyncAsync_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            var p = await _myRepository.GetOneAsync(x=>x.Id == "1");

            Assert.IsNotNull(p);
        }
        
        [TestMethod()]
        public async Task ClearAsync_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.ClearAsync();

            Assert.IsTrue(_myRepository.Table.Count() == 0);
        }

        [TestMethod()]
        public async Task AddToSet_LiteRepository_Success()
        {
            //Arrange
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.AddToSet("1", x => x.UserFields,
                new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" });

            //Act
            await _myRepository.AddToSet("1", x => x.UserFields,
            new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" });
            var p = _myRepository.GetById("1");
            
            //Assert
            Assert.IsTrue(p.UserFields.Count == 2);
            Assert.IsTrue(p.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p.UpdatedBy == "user");

        }

        [TestMethod()]
        public async Task Delete_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            _myRepository.Delete(product);

            var p = _myRepository.GetById("1");

            Assert.IsNull(p);
        }

        [TestMethod()]
        public async Task DeleteAsync_LiteRepository_Success()
        {
            var product = new SampleCollection { Id = "1" };
            await _myRepository.InsertAsync(product);

            await _myRepository.DeleteAsync(product);

            var p = _myRepository.GetById("1");

            Assert.IsNull(p);
        }
        [TestMethod()]
        public async Task DeleteManyAsync_LiteRepository_Success()
        {
            await _myRepository.InsertAsync(new SampleCollection { Id = "1", Name = "Test" });
            await _myRepository.InsertAsync(new SampleCollection { Id = "2", Name = "Test" });
            await _myRepository.InsertAsync(new SampleCollection { Id = "3", Name = "Test2" });

            await _myRepository.DeleteManyAsync(x => x.Name == "Test");

            Assert.IsTrue(_myRepository.Table.Count() == 1);
        }

        [TestMethod()]
        public async Task Pull_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                    Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection { Id = "2", Name = "Test2",
                Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            //Act
            await _myRepository.Pull("1", x => x.Phones, "Phone2");
            var p = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p.Phones.Count == 2);
            Assert.IsTrue(p.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p.UpdatedBy == "user");
        }
        [TestMethod()]
        public async Task Pull_Many_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                    Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection { Id = "2", Name = "Test2",
                Phones = new [] { "Phone1", "Phone2", "Phone3" }
                },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));

            //Act
            await _myRepository.Pull(String.Empty, x => x.Phones, "Phone2");

            var p1 = _myRepository.GetById("1");
            var p2 = _myRepository.GetById("2");
            var p3 = _myRepository.GetById("3");
            
            //Assert
            Assert.IsTrue(p1.Phones.Count == 2 && p2.Phones.Count == 2 && p3.Phones.Count == 0);
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");

        }

        [TestMethod()]
        public async Task PullFilter_1_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value1", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            //Act
            await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value == "value");

            var p1 = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p1.UserFields.Count == 2);
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");

        }

        [TestMethod()]
        public async Task PullFilter_2_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));

            //Act
            await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value, "value");

            var p1 = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p1.UserFields.Count == 1);
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");

        }
        [TestMethod()]
        public async Task PullFilter_2_Many_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value1", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));

            //Act
            await _myRepository.PullFilter(String.Empty, x => x.UserFields, x => x.Value, "value");

            var p1 = _myRepository.GetById("1");
            var p2 = _myRepository.GetById("2");

            //Assert
            Assert.IsTrue(p1.UserFields.Count == 1 && p2.UserFields.Count == 2);
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");
        }


        [TestMethod()]
        public void Update_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));

            var p1_update = products.FirstOrDefault();
            p1_update.Name = "update";

            //Act
            _myRepository.Update(p1_update);

            var p1 = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p1.Name == "update");
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");

        }
        [TestMethod()]
        public async Task UpdateAsync_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));

            var p1_update = products.FirstOrDefault();
            p1_update.Name = "update";
            //Act
            await _myRepository.UpdateAsync(p1_update);

            var p1 = _myRepository.GetById("1");
            //Assert
            Assert.IsTrue(p1.Name == "update");
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");
        }

        [TestMethod()]
        public async Task UpdateField_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test2" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            //Act
            await _myRepository.UpdateField("1", x => x.Name, "update");

            var p1 = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p1.Name == "update");
            Assert.IsTrue(p1.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p1.UpdatedBy == "user");

        }
        [TestMethod()]
        public async Task IncField_MongoRepository_Success()
        {
            var sample = new SampleCollection { Id = "1", Name = "Test" };
            await _myRepository.InsertAsync(sample);

            await _myRepository.IncField("1", x => x.Count, 1);
            await _myRepository.IncField("1", x => x.Count, 1);
            await _myRepository.IncField("1", x => x.Count, 1);

            var p1 = _myRepository.GetById("1");

            Assert.IsTrue(p1.Count == 3);
        }
        [TestMethod()]
        public async Task UpdateManyAsync_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            
            //Act
            await _myRepository.UpdateManyAsync(x => x.Name == "Test",
                UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));

            var pUpdated = _myRepository.Table.Where(x=>x.Name == "UpdateTest");

            //Asser 
            Assert.IsTrue(pUpdated.Count() == 2);
            Assert.IsTrue(pUpdated.FirstOrDefault()!.UpdatedOnUtc.HasValue);
            Assert.IsTrue(pUpdated.FirstOrDefault()!.UpdatedBy == "user");

        }

        [TestMethod()]
        public async Task UpdateOneAsync_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            //Act
            await _myRepository.UpdateOneAsync(x => x.Name == "Test",
                UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));

            var pUpdated = _myRepository.Table.Where(x => x.Name == "UpdateTest");

            //Assert
            Assert.IsTrue(pUpdated.Count() == 1);
            Assert.IsTrue(pUpdated.FirstOrDefault()!.UpdatedOnUtc.HasValue);
            Assert.IsTrue(pUpdated.FirstOrDefault()!.UpdatedBy == "user");

        }
        [TestMethod()]
        public async Task UpdateToSet_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            
            //Act
            await _myRepository.UpdateToSet("1", x => x.UserFields, z => z.Key, "key", new Domain.Common.UserField { Key = "key", Value = "update", StoreId = "1" });
            var p = _myRepository.GetById("1");
            
            //Assert
            Assert.IsTrue(p.UserFields!.FirstOrDefault(x=>x.Key=="key")!.Value == "update");
            Assert.IsTrue(p.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p.UpdatedBy == "user");

        }

        [TestMethod()]
        public async Task UpdateToSet_2_LiteRepository_Success()
        {
            //Arrange
            var products = new List<SampleCollection> {
            new SampleCollection { Id = "1", Name = "Test",
                UserFields = new List<Domain.Common.UserField> {
                    new Domain.Common.UserField { Key = "key", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key1", Value = "value", StoreId = "" },
                    new Domain.Common.UserField { Key = "key2", Value = "value2", StoreId = "" }
                } },
            new SampleCollection { Id = "2", Name = "Test" },
            new SampleCollection { Id = "3", Name = "Test3" }

            };
            products.ForEach(x=>_myRepository.Insert(x));
            //Act
            await _myRepository.UpdateToSet("1", x => x.UserFields, z => z.Key == "key", new Domain.Common.UserField { Key = "key", Value = "update", StoreId = "1" });

            var p = _myRepository.GetById("1");

            //Assert
            Assert.IsTrue(p.UserFields!.FirstOrDefault(x => x.Key == "key")!.Value == "update");
            Assert.IsTrue(p.UpdatedOnUtc.HasValue);
            Assert.IsTrue(p.UpdatedBy == "user");

        }
    }
}
