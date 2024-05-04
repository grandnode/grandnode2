using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.Queries.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.Queries.Handlers;

[TestClass]
public class GetProductByIdQueryHandlerTests
{
    private GetProductByIdQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        var _repository = new MongoDBRepositoryTest<Product>();
        _repository.Insert(new Product { Id = "1" });
        _repository.Insert(new Product());
        _repository.Insert(new Product());
        _repository.Insert(new Product());

        handler = new GetProductByIdQueryHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Act
        var result = await handler.Handle(new GetProductByIdQuery { Id = "1" }, CancellationToken.None);
        //Assert
        Assert.IsNotNull(result);
    }
}