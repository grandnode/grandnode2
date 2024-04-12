using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class MerchandiseReturnServiceTests
{
    private MemoryCacheBase _cacheBase;

    private Mock<IMediator> _mediatorMock;
    private IRepository<MerchandiseReturnAction> _merchandiseReturnActionRepository;
    private IRepository<MerchandiseReturnNote> _merchandiseReturnNoteRepository;
    private IRepository<MerchandiseReturnReason> _merchandiseReturnReasonRepository;
    private MerchandiseReturnService _merchandiseReturnService;

    private IRepository<MerchandiseReturn> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<MerchandiseReturn>();
        _merchandiseReturnActionRepository = new MongoDBRepositoryTest<MerchandiseReturnAction>();
        _merchandiseReturnReasonRepository = new MongoDBRepositoryTest<MerchandiseReturnReason>();
        _merchandiseReturnNoteRepository = new MongoDBRepositoryTest<MerchandiseReturnNote>();

        _mediatorMock = new Mock<IMediator>();

        var query = from p in _repository.Table
            select p;
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), default))
            .Returns(Task.FromResult(query));

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _merchandiseReturnService = new MerchandiseReturnService(_repository, _merchandiseReturnActionRepository,
            _merchandiseReturnReasonRepository, _merchandiseReturnNoteRepository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetMerchandiseReturnByIdTest()
    {
        //Arange
        var merchandiseReturn = new MerchandiseReturn();
        await _repository.InsertAsync(merchandiseReturn);

        //Act
        var result = _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturn.Id);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetMerchandiseReturnByIdTest_ReturnNumber()
    {
        //Arange
        var merchandiseReturn = new MerchandiseReturn { ReturnNumber = 1 };
        await _repository.InsertAsync(merchandiseReturn);

        //Act
        var result = _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturn.ReturnNumber);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SearchMerchandiseReturnsTest()
    {
        //Arange
        var merchandiseReturn = new MerchandiseReturn();
        await _repository.InsertAsync(merchandiseReturn);

        //Act
        var result = await _merchandiseReturnService.SearchMerchandiseReturns();

        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetAllMerchandiseReturnActionsTest()
    {
        //Arrange
        await _merchandiseReturnActionRepository.InsertAsync(new MerchandiseReturnAction());
        await _merchandiseReturnActionRepository.InsertAsync(new MerchandiseReturnAction());

        //Act
        var result = await _merchandiseReturnService.GetAllMerchandiseReturnActions();

        //Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetMerchandiseReturnActionByIdTest()
    {
        //Arrange
        await _merchandiseReturnActionRepository.InsertAsync(new MerchandiseReturnAction { Id = "1" });
        await _merchandiseReturnActionRepository.InsertAsync(new MerchandiseReturnAction());

        //Act
        var result = await _merchandiseReturnService.GetMerchandiseReturnActionById("1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertMerchandiseReturnTest()
    {
        //Act
        await _merchandiseReturnService.InsertMerchandiseReturn(new MerchandiseReturn());

        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnTest()
    {
        //Arange
        var merchandiseReturn = new MerchandiseReturn();
        await _repository.InsertAsync(merchandiseReturn);

        //Act
        merchandiseReturn.CustomerComments = "test";
        await _merchandiseReturnService.UpdateMerchandiseReturn(merchandiseReturn);

        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == merchandiseReturn.Id).CustomerComments == "test");
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturnTest()
    {
        //Arange
        var merchandiseReturn = new MerchandiseReturn();
        await _repository.InsertAsync(merchandiseReturn);

        //Act
        await _merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);

        //Assert
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task InsertMerchandiseReturnActionTest()
    {
        //Act
        await _merchandiseReturnService.InsertMerchandiseReturnAction(new MerchandiseReturnAction());

        //Assert
        Assert.IsTrue(_merchandiseReturnActionRepository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnActionTest()
    {
        //Arange
        var merchandiseReturnAction = new MerchandiseReturnAction();
        await _merchandiseReturnActionRepository.InsertAsync(merchandiseReturnAction);

        //Act
        merchandiseReturnAction.Name = "test";
        await _merchandiseReturnService.UpdateMerchandiseReturnAction(merchandiseReturnAction);

        //Assert
        Assert.IsTrue(_merchandiseReturnActionRepository.Table.FirstOrDefault(x => x.Id == merchandiseReturnAction.Id)
            .Name == "test");
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturnActionTest()
    {
        //Arange
        var merchandiseReturnAction = new MerchandiseReturnAction();
        await _merchandiseReturnActionRepository.InsertAsync(merchandiseReturnAction);

        //Act
        await _merchandiseReturnService.DeleteMerchandiseReturnAction(merchandiseReturnAction);

        //Assert
        Assert.IsFalse(_merchandiseReturnActionRepository.Table.Any());
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturnReasonTest()
    {
        //Arange
        var merchandiseReturnReason = new MerchandiseReturnReason();
        await _merchandiseReturnReasonRepository.InsertAsync(merchandiseReturnReason);

        //Act
        await _merchandiseReturnService.DeleteMerchandiseReturnReason(merchandiseReturnReason);

        //Assert
        Assert.IsFalse(_merchandiseReturnReasonRepository.Table.Any());
    }

    [TestMethod]
    public async Task GetAllMerchandiseReturnReasonsTest()
    {
        //Arrange
        await _merchandiseReturnReasonRepository.InsertAsync(new MerchandiseReturnReason { Id = "1" });
        await _merchandiseReturnReasonRepository.InsertAsync(new MerchandiseReturnReason());

        //Act
        var result = await _merchandiseReturnService.GetAllMerchandiseReturnReasons();

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetMerchandiseReturnReasonByIdTest()
    {
        //Arrange
        await _merchandiseReturnReasonRepository.InsertAsync(new MerchandiseReturnReason { Id = "1" });
        await _merchandiseReturnReasonRepository.InsertAsync(new MerchandiseReturnReason());

        //Act
        var result = await _merchandiseReturnService.GetMerchandiseReturnReasonById("1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertMerchandiseReturnReasonTest()
    {
        //Arange
        var merchandiseReturnReason = new MerchandiseReturnReason();

        //Act
        await _merchandiseReturnService.InsertMerchandiseReturnReason(merchandiseReturnReason);

        //Assert
        Assert.IsTrue(_merchandiseReturnReasonRepository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnReasonTest()
    {
        //Arange
        var merchandiseReturnReason = new MerchandiseReturnReason();
        await _merchandiseReturnReasonRepository.InsertAsync(merchandiseReturnReason);
        //Act
        merchandiseReturnReason.Name = "test";
        await _merchandiseReturnService.UpdateMerchandiseReturnReason(merchandiseReturnReason);

        //Assert
        Assert.IsTrue(_merchandiseReturnReasonRepository.Table.FirstOrDefault(x => x.Id == merchandiseReturnReason.Id)
            .Name == "test");
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturnNoteTest()
    {
        //Arange
        var merchandiseReturnNote = new MerchandiseReturnNote();
        await _merchandiseReturnNoteRepository.InsertAsync(merchandiseReturnNote);

        //Act
        await _merchandiseReturnService.DeleteMerchandiseReturnNote(merchandiseReturnNote);

        //Assert
        Assert.IsFalse(_merchandiseReturnNoteRepository.Table.Any());
    }

    [TestMethod]
    public async Task InsertMerchandiseReturnNoteTest()
    {
        //Arange
        var merchandiseReturnNote = new MerchandiseReturnNote();

        //Act
        await _merchandiseReturnService.InsertMerchandiseReturnNote(merchandiseReturnNote);

        //Assert
        Assert.IsTrue(_merchandiseReturnNoteRepository.Table.Any());
    }

    [TestMethod]
    public async Task GetMerchandiseReturnNotesTest()
    {
        //Arange
        var merchandiseReturnNote = new MerchandiseReturnNote { MerchandiseReturnId = "1" };
        await _merchandiseReturnNoteRepository.InsertAsync(merchandiseReturnNote);

        //Act
        var result = await _merchandiseReturnService.GetMerchandiseReturnNotes("1");

        //Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task GetMerchandiseReturnNoteTest()
    {
        //Arange
        var merchandiseReturnNote = new MerchandiseReturnNote { Id = "1" };
        await _merchandiseReturnNoteRepository.InsertAsync(merchandiseReturnNote);

        //Act
        var result = await _merchandiseReturnService.GetMerchandiseReturnNote("1");

        //Assert
        Assert.IsNotNull(result);
    }
}