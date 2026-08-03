# Best Practice: Tests

Patterns from `src/Tests/**`. Framework: **MSTest** + **Moq**.

---

## Project Naming

Each business project has a sibling test project:

```
Grand.Business.Checkout   →  Grand.Business.Checkout.Tests
Grand.Business.Catalog    →  Grand.Business.Catalog.Tests
Grand.Web                 →  Grand.Web.Store.Tests
```

---

## Test Structure

### Framework attributes

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class PickupPointServiceTests
{
    private Mock<IRepository<PickupPoint>> _repositoryMock;
    private Mock<ICacheBase> _cacheMock;
    private PickupPointService _service;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IRepository<PickupPoint>>();
        _cacheMock = new Mock<ICacheBase>();
        _service = new PickupPointService(_repositoryMock.Object, _cacheMock.Object);
    }
}
```

- `[TestClass]` marks the class.
- `[TestMethod]` marks each test.
- `[TestInitialize]` runs before every test — initialize mocks and the subject under test here.

### Arrange / Act / Assert

Always structure test bodies with the three sections, using comment separators when the test is non-trivial:

```csharp
[TestMethod]
public async Task Methods_ReturnPaymentMethodsForCurrentStore()
{
    // Arrange
    _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, StoreId, ""))
        .ReturnsAsync(new List<IPaymentProvider> { CreateProvider() });
    
    // Act
    var result = await _controller.Methods();
    
    // Assert
    var data = (DataSourceResult)((JsonResult)result).Value;
    Assert.AreEqual(1, data.Total);
    _paymentServiceMock.Verify(p => p.LoadAllPaymentMethods(null, StoreId, ""), Times.Once);
}
```

### Test naming

```
MethodName_WhenCondition_ThenResult

Examples:
GetPickupPointById_WhenExists_ReturnsPoint
PlaceOrder_WhenProductOutOfStock_ReturnsError
ShoppingCartAuctionValidator_Fail
```

---

## Mocking Patterns

### Setup return values

```csharp
_settingServiceMock
    .Setup(s => s.LoadSetting<PaymentSettings>(StoreId))
    .ReturnsAsync(new PaymentSettings { ... });
```

### Translation service (always needed for validators)

Every test that instantiates a FluentValidation validator must stub `ITranslationService`:

```csharp
_translationServiceMock
    .Setup(t => t.GetResource(It.IsAny<string>()))
    .Returns("resource");
```

### Verify interactions

```csharp
_paymentServiceMock.Verify(p => p.LoadAllPaymentMethods(null, StoreId, ""), Times.Once);
_settingServiceMock.Verify(s => s.LoadSetting<PaymentSettings>(StoreId), Times.Once);
```

---

## Testing Validators

Use the record input types. Test both success and failure paths:

```csharp
[TestMethod]
public void ShoppingCartAuctionValidator_Success()
{
    var validator = new ShoppingCartAuctionValidator(_translationServiceMock.Object);
    
    var result = validator.Validate(new ShoppingCartAuctionValidatorRecord(
        new Customer(),
        new Product { AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1) },
        new ShoppingCartItem(),
        bidAmount: 10));
    
    Assert.IsTrue(result.IsValid);
}

[TestMethod]
public void ShoppingCartAuctionValidator_Fail()
{
    var validator = new ShoppingCartAuctionValidator(_translationServiceMock.Object);
    
    var result = validator.Validate(new ShoppingCartAuctionValidatorRecord(
        new Customer(),
        new Product { AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1), StartPrice = 20 },
        new ShoppingCartItem(),
        bidAmount: 10));     // bid below start price
    
    Assert.IsFalse(result.IsValid);
}
```

---

## Testing Controllers with AutoMapper

Controllers that use the custom mapping framework need `AutoMapperConfig.Init` in setup:

```csharp
[TestInitialize]
public void Setup()
{
    var mapperConfig = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<PaymentMethodProfile>();
        cfg.AddProfile<CountryProfile>();
    });
    AutoMapperConfig.Init(mapperConfig);
    
    // ... rest of setup
}
```

---

## Anti-Patterns

| Anti-pattern | Problem | Fix |
|---|---|---|
| No `[TestInitialize]` — new mocks inline | Mocks recreated manually in each test | Use `[TestInitialize]` |
| No translation stub in validator tests | NullReferenceException at runtime | Always stub `GetResource` to return a string |
| `Assert.IsTrue(result != null)` | Vague failure message | `Assert.IsNotNull(result)` |
| Not verifying service calls | Test passes even if method was never called | Add `.Verify(...)` for critical interactions |
| Hardcoded sleep / `Task.Delay` | Flaky tests | Design code to be testable without timing |
