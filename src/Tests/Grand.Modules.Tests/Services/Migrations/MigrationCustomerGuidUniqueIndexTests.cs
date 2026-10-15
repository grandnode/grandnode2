using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Data.Tests;
using Grand.Domain.Customers;
using Grand.Module.Migration.Migrations._2._5;
using Grand.SharedKernel;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationCustomerGuidUniqueIndexTests
{
    private LiteDatabase _database;
    private LiteDBRepository<Customer> _repository;
    private LiteDBContext _context;

    [TestInitialize]
    public void Setup()
    {
        _database = new LiteDatabase(new MemoryStream());
        _repository = new LiteDBRepository<Customer>(_database, new AuditInfoProvider());
        _context = new LiteDBContext(_database);
        //an upgraded install: the lookup index exists, not unique
        _database.GetCollection<Customer>(nameof(Customer)).EnsureIndex(x => x.CustomerGuid);
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    [TestMethod]
    public async Task LiteDb_RecreatesTheIndexUnique()
    {
        var guid = Guid.NewGuid();
        await _repository.InsertAsync(new Customer { CustomerGuid = guid });

        Assert.IsTrue(MigrationCustomerGuidUniqueIndex.MakeUnique(_repository, _context, NullLogger.Instance));

        await Assert.ThrowsExactlyAsync<DuplicateKeyGrandException>(() => _repository.InsertAsync(new Customer { CustomerGuid = guid }));
    }

    [TestMethod]
    public async Task LiteDb_Duplicates_KeepTheIndexNonUnique()
    {
        var guid = Guid.NewGuid();
        await _repository.InsertAsync(new Customer { CustomerGuid = guid });
        await _repository.InsertAsync(new Customer { CustomerGuid = guid });

        Assert.IsFalse(MigrationCustomerGuidUniqueIndex.MakeUnique(_repository, _context, NullLogger.Instance));

        await _repository.InsertAsync(new Customer { CustomerGuid = guid });
        Assert.AreEqual(3, _repository.Table.Count(x => x.CustomerGuid == guid));
    }

    [TestMethod]
    public async Task UniqueIndexRejected_RestoresTheLookupIndexAndFailsTheMigration()
    {
        await _repository.InsertAsync(new Customer { CustomerGuid = Guid.NewGuid() });
        //a duplicate saved by a running instance after the duplicate check
        var context = new Mock<IDatabaseContext>();
        context.Setup(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<Customer>>(), "CustomerGuid_1", true))
            .ThrowsAsync(new LiteException(0, "duplicate key"));

        var services = new ServiceCollection();
        services.AddSingleton<IRepository<Customer>>(_repository);
        services.AddSingleton(context.Object);
        services.AddSingleton<ILogger<MigrationCustomerGuidUniqueIndex>>(NullLogger<MigrationCustomerGuidUniqueIndex>.Instance);

        Assert.IsFalse(new MigrationCustomerGuidUniqueIndex().UpgradeProcess(services.BuildServiceProvider()));

        context.Verify(x => x.CreateIndex(_repository, It.IsAny<OrderBuilder<Customer>>(), "CustomerGuid_1", false),
            Times.Once);
    }
}
