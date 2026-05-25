---
name: write-tests
description: Write unit tests for GrandNode2 backend code — command handlers, query handlers, services, validators. Use when asked to add tests, write unit tests, test a handler, or cover new code with tests.
---

## Stack

- **Framework**: MSTest (`[TestClass]`, `[TestInitialize]`, `[TestMethod]`)
- **Mocking**: Moq (`Mock<T>`, `.Setup()`, `.Verify()`, `.Object`)
- **In-memory DB**: `MongoDBRepositoryTest<T>` from `Grand.Data.Tests` — use instead of mocking `IRepository<T>` when you need real query/insert behavior
- **Cache helper**: `MemoryCacheBase` + `MemoryCacheTest.Get()` from `Grand.Infrastructure.Tests`
- **No xUnit, no NUnit**

## Project Structure

Each business module has a mirror test project: `src/Tests/Grand.Business.{Module}.Tests/`

Folder layout mirrors source:
```
Commands/Handlers/Orders/DeleteOrderCommandHandlerTests.cs
Queries/Handlers/Orders/HandlerTests.cs
Services/Orders/OrderServiceTests.cs
Validators/ShoppingCartValidatorsTests.cs
Events/Handlers/BrandDeletedEventHandlerTests.cs
```

## Adding a Test File

1. Create the `.cs` file in the appropriate folder under the test project.
2. The `.csproj` uses globbing — no manual `<Compile>` entries needed.
3. Reference `Grand.Data.Tests` and `Grand.Infrastructure.Tests` if you need `MongoDBRepositoryTest<T>` or `MemoryCacheTest`.

## Canonical Patterns

### Command Handler Test (Moq repositories — behavior/interaction focus)

```csharp
[TestClass]
public class DeleteOrderCommandHandlerTests
{
    private DeleteOrderCommandHandler _handler;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IShipmentService> _shipmentServiceMock;
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Init()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _mediatorMock = new Mock<IMediator>();
        _handler = new DeleteOrderCommandHandler(
            _mediatorMock.Object, _orderServiceMock.Object, _shipmentServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new DeleteOrderCommand { Order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending } };
        _shipmentServiceMock.Setup(c => c.GetShipmentsByOrder(It.IsAny<string>())).ReturnsAsync(new List<Shipment>());
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }
}
```

### Query Handler Test (MongoDBRepositoryTest — real in-memory DB)

```csharp
[TestClass]
public class GetVendorByIdQueryHandlerTests
{
    private IRepository<Vendor> _repository;
    private GetVendorByIdQueryHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Vendor>();
        _handler = new GetVendorByIdQueryHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        await _repository.InsertAsync(new Vendor { Id = "1" });
        //Act
        var result = await _handler.Handle(new GetVendorByIdQuery { Id = "1" }, CancellationToken.None);
        //Assert
        Assert.IsNotNull(result);
    }
}
```

### Service Test (Moq — verify side effects)

```csharp
[TestClass]
public class OrderServiceTests
{
    private OrderService _service;
    private Mock<IRepository<Order>> _orderRepositoryMock;
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Init()
    {
        _orderRepositoryMock = new Mock<IRepository<Order>>();
        _mediatorMock = new Mock<IMediator>();
        _service = new OrderService(_orderRepositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task UpdateOrder_InvokeExpectedMethods()
    {
        await _service.UpdateOrder(new Order());
        _orderRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Order>>(), default), Times.Once);
    }

    [TestMethod]
    public void UpdateOrder_NullArguments_ThrowException()
    {
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateOrder(null));
    }
}
```

### Validator Test (FluentValidation — check IsValid)

```csharp
[TestMethod]
public void ShoppingCartAuctionValidator_Success()
{
    //Arrange
    var validator = new ShoppingCartAuctionValidator(_translationServiceMock.Object);
    //Act
    var result = validator.Validate(new ShoppingCartAuctionValidatorRecord(
        new Customer(), new Product { AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1) },
        new ShoppingCartItem(), 10));
    //Assert
    Assert.IsTrue(result.IsValid);
}
```

### Service Test with Real Cache + DB (integration-style unit test)

```csharp
[TestInitialize]
public void InitializeTests()
{
    _repository = new MongoDBRepositoryTest<Category>();
    _mediatorMock = new Mock<IMediator>();
    _workContextMock = new Mock<IContextAccessor>();
    _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "" });
    _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());
    _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
        new CacheConfig { DefaultCacheTimeMinutes = 1 });
    _service = new CategoryService(_cacheBase, _repository, _workContextMock.Object,
        _mediatorMock.Object, new AclService(new AccessControlConfig()), new AccessControlConfig());
}
```

## Naming Conventions

| What | Convention | Example |
|---|---|---|
| Test class | `{ClassName}Tests` or `{ClassName}Test` | `ProductServiceTests` |
| Test method | `{Method}_{Scenario}_{ExpectedResult}` | `UpdateOrder_NullArguments_ThrowException` |
| Simple handler test | `HandleTest` | `HandleTest` |

## When to Use MongoDBRepositoryTest vs Mock<IRepository<T>>

- Use `MongoDBRepositoryTest<T>` when the test needs real LINQ/filter behavior (queries, inserts, counts).
- Use `Mock<IRepository<T>>` when only verifying that repository methods are called the right number of times.

## Running Tests

```bash
# Build first
dotnet build src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj

# Run all tests in a project
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj --configuration Release --no-build

# Run a specific test class
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj --filter "FullyQualifiedName~ProductServiceTests"
```

## Gotchas

- `MongoDBRepositoryTest<T>` drops the collection on construction — each test class starts with an empty collection. Never share an instance across test classes.
- `Assert.ThrowsExceptionAsync` must be awaited or the assertion is silent — prefer: `await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await ...)`.
- `IContextAccessor` wraps both `WorkContext` and `StoreContext` — always mock both if the SUT touches either.
- `_mediatorMock.Verify(c => c.Publish(...), default)` — the second arg is `CancellationToken.None` but matching `default` is equivalent and conventional in the codebase.
