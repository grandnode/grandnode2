using Grand.Domain.Common;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Data.Tests.MongoDb;

[TestClass]
public class MongoRepositoryTests
{
    private IRepository<SampleCollection> _myRepository;

    [TestInitialize]
    public void Init()
    {
        _myRepository = new MongoDBRepositoryTest<SampleCollection>();

        CommonPath.BaseDirectory = "";
    }

    [TestMethod]
    public void Insert_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection();
        //Act
        _myRepository.Insert(product);
        //Assert
        Assert.AreEqual(1, _myRepository.Table.Count());
        Assert.IsTrue(_myRepository.Table.FirstOrDefault()!.CreatedBy == "user");
        Assert.IsTrue(_myRepository.Table.FirstOrDefault()!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(_myRepository.Table.FirstOrDefault()!.CreatedOnUtc.Month == DateTime.UtcNow.Month);
        Assert.IsTrue(_myRepository.Table.FirstOrDefault()!.CreatedOnUtc.Day == DateTime.UtcNow.Day);
    }


    [TestMethod]
    public async Task GetById_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);

        //Act
        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsNotNull(p);
    }

    [TestMethod]
    public async Task GetByIdAsync_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);
        //Act
        var p = await _myRepository.GetByIdAsync("1");
        //Assert
        Assert.IsNotNull(p);
        Assert.IsTrue(p!.CreatedBy == "user");
        Assert.IsTrue(p!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
    }

    [TestMethod]
    public async Task GetOneAsync_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);
        //Act
        var p = await _myRepository.GetOneAsync(x => x.Id == "1");
        //Assert
        Assert.IsNotNull(p);
        Assert.IsTrue(p!.CreatedBy == "user");
        Assert.IsTrue(p!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
    }

    [TestMethod]
    public async Task ClearAsync_MongoRepository_Success()
    {
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);

        await _myRepository.ClearAsync();

        Assert.IsTrue(_myRepository.Table.Count() == 0);
    }

    [TestMethod]
    public async Task AddToSet_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);

        await _myRepository.AddToSet("1", x => x.UserFields,
            new UserField { Key = "key", Value = "value", StoreId = "" });

        //Act
        await _myRepository.AddToSet("1", x => x.UserFields,
            new UserField { Key = "key2", Value = "value2", StoreId = "" });

        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p.UserFields.Count == 2);
        Assert.IsTrue(p!.CreatedBy == "user");
        Assert.IsTrue(p!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p!.UpdatedBy == "user");
        Assert.IsTrue(p!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task Delete_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);

        //Act
        _myRepository.Delete(product);
        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsNull(p);
    }

    [TestMethod]
    public async Task DeleteAsync_MongoRepository_Success()
    {
        //Arrange
        var product = new SampleCollection { Id = "1" };
        await _myRepository.InsertAsync(product);

        //Act
        await _myRepository.DeleteAsync(product);
        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsNull(p);
    }

    [TestMethod]
    public async Task DeleteManyAsync_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() { Id = "1", Name = "Test" },
            new() { Id = "2", Name = "Test" },
            new() { Id = "3", Name = "Test2" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.DeleteManyAsync(x => x.Name == "Test");

        //Assert
        Assert.IsTrue(_myRepository.Table.Count() == 1);
    }

    [TestMethod]
    public async Task Pull_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                Phones = new[] { "Phone1", "Phone2", "Phone3" }
            },
            new() {
                Id = "2", Name = "Test2",
                Phones = new[] { "Phone1", "Phone2", "Phone3" }
            },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.Pull("1", x => x.Phones, "Phone2");

        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p.Phones.Count == 2);
        Assert.IsTrue(p!.CreatedBy == "user");
        Assert.IsTrue(p!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p!.UpdatedBy == "user");
        Assert.IsTrue(p!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task Pull_Many_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                Phones = new[] { "Phone1", "Phone2", "Phone3" }
            },
            new() {
                Id = "2", Name = "Test2",
                Phones = new[] { "Phone1", "Phone2", "Phone3" }
            },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.Pull(string.Empty, x => x.Phones, "Phone2");

        var p1 = _myRepository.GetById("1");
        var p2 = _myRepository.GetById("2");
        var p3 = _myRepository.GetById("3");

        //Assert
        Assert.IsTrue(p1.Phones.Count == 2 && p2.Phones.Count == 2 && p3.Phones.Count == 0);
        Assert.IsTrue(p1!.CreatedBy == "user");
        Assert.IsTrue(p1!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task PullFilter_1_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test2" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value == "value");

        var p1 = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p1.UserFields.Count == 1);
        Assert.IsTrue(p1!.CreatedBy == "user");
        Assert.IsTrue(p1!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task PullFilter_2_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test2" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.PullFilter("1", x => x.UserFields, x => x.Value, "value");

        var p1 = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p1.UserFields.Count == 1);
        Assert.IsTrue(p1!.CreatedBy == "user");
        Assert.IsTrue(p1!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task PullFilter_2_Many_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() {
                Id = "2", Name = "Test2",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value1", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.PullFilter(string.Empty, x => x.UserFields, x => x.Value, "value");

        var p1 = _myRepository.GetById("1");
        var p2 = _myRepository.GetById("2");

        //Assert
        Assert.IsTrue(p1.UserFields.Count == 1 && p2.UserFields.Count == 2);
        Assert.IsTrue(p1!.CreatedBy == "user");
        Assert.IsTrue(p1!.CreatedOnUtc.Year == DateTime.UtcNow.Year);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }


    [TestMethod]
    public void Update_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test2" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        var p1_update = products.FirstOrDefault();
        p1_update.Name = "update";

        //Act
        _myRepository.Update(p1_update);
        var p1 = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p1.Name == "update");
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task UpdateAsync_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test2" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        var p1_update = products.FirstOrDefault();
        p1_update.Name = "update";
        //Act
        await _myRepository.UpdateAsync(p1_update);
        var p1 = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p1.Name == "update");
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task UpdateField_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test2" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.UpdateField("1", x => x.Name, "update");
        var p1 = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p1.Name == "update");
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
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

    [TestMethod]
    public async Task UpdateManyAsync_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));
        //Act
        await _myRepository.UpdateManyAsync(x => x.Name == "Test",
            UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));
        var pUpdated = _myRepository.Table.Where(x => x.Name == "UpdateTest");
        var p1 = pUpdated.FirstOrDefault();
        //Assert
        Assert.IsTrue(pUpdated.Count() == 2);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task UpdateOneAsync_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));

        //Act
        await _myRepository.UpdateOneAsync(x => x.Name == "Test",
            UpdateBuilder<SampleCollection>.Create().Set(x => x.Name, "UpdateTest"));

        var pUpdated = _myRepository.Table.Where(x => x.Name == "UpdateTest");
        var p1 = pUpdated.FirstOrDefault();
        //Assert
        Assert.IsTrue(pUpdated.Count() == 1);
        Assert.IsTrue(p1!.UpdatedBy == "user");
        Assert.IsTrue(p1!.UpdatedOnUtc.HasValue);
    }

    [TestMethod]
    public async Task UpdateToSet_MongoRepository_Success()
    {
        //Arrange
        var products = new List<SampleCollection> {
            new() {
                Id = "1", Name = "Test",
                UserFields = new List<UserField> {
                    new() { Key = "key", Value = "value", StoreId = "" },
                    new() { Key = "key1", Value = "value", StoreId = "" },
                    new() { Key = "key2", Value = "value2", StoreId = "" }
                }
            },
            new() { Id = "2", Name = "Test" },
            new() { Id = "3", Name = "Test3" }
        };
        products.ForEach(x => _myRepository.Insert(x));
        //Act
        await _myRepository.UpdateToSet("1", x => x.UserFields, z => z.Key, "key",
            new UserField { Key = "key", Value = "update", StoreId = "1" });
        var p = _myRepository.GetById("1");

        //Assert
        Assert.IsTrue(p.UserFields.FirstOrDefault(x => x.Key == "key")!.Value == "update");
        Assert.IsTrue(p!.UpdatedBy == "user");
        Assert.IsTrue(p!.UpdatedOnUtc.HasValue);
    }
}