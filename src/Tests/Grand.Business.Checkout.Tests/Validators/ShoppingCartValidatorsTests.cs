using Grand.Business.Catalog.Services.Products;
using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Validators;

[TestClass]
public class ShoppingCartValidatorsTests
{
    private Mock<IAclService> _aclServiceMock;
    private Mock<ICheckoutAttributeParser> _checkoutAttributeParserMock;
    private Mock<ICheckoutAttributeService> _checkoutAttributeServiceMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IProductAttributeService> _productAttributeService;
    private Mock<IProductReservationService> _productReservationServiceMock;
    private Mock<IProductService> _productServiceMock;
    private IStockQuantityService _stockQuantityService;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _translationServiceMock = new Mock<ITranslationService>();
        _checkoutAttributeParserMock = new Mock<ICheckoutAttributeParser>();
        _checkoutAttributeServiceMock = new Mock<ICheckoutAttributeService>();
        _productServiceMock = new Mock<IProductService>();
        _aclServiceMock = new Mock<IAclService>();
        _productAttributeService = new Mock<IProductAttributeService>();
        _productReservationServiceMock = new Mock<IProductReservationService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _stockQuantityService = new StockQuantityService();
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Name");
    }

    [TestMethod]
    public void ShoppingCartAuctionValidator_Success()
    {
        //Arrange
        var shoppingCartAuctionValidator = new ShoppingCartAuctionValidator(_translationServiceMock.Object);
        //Act
        var result = shoppingCartAuctionValidator.Validate(new ShoppingCartAuctionValidatorRecord(new Customer(),
            new Product { AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1) }, new ShoppingCartItem(), 10));
        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ShoppingCartAuctionValidator_Fail()
    {
        //Arrange
        var shoppingCartAuctionValidator = new ShoppingCartAuctionValidator(_translationServiceMock.Object);
        //Act
        var result = shoppingCartAuctionValidator.Validate(new ShoppingCartAuctionValidatorRecord(new Customer(),
            new Product { AvailableEndDateTimeUtc = DateTime.UtcNow.AddDays(1), StartPrice = 20 },
            new ShoppingCartItem(), 10));
        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ShoppingCartGiftVoucherValidator_Success()
    {
        //Arrange
        var shoppingCartGiftVoucherValidator = new ShoppingCartGiftVoucherValidator(_translationServiceMock.Object);
        var customAttributes = new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = "RecipientName", Value = "value" });
        customAttributes.Add(new CustomAttribute { Key = "RecipientEmail", Value = "value@emai.com" });
        customAttributes.Add(new CustomAttribute { Key = "SenderName", Value = "value" });
        customAttributes.Add(new CustomAttribute { Key = "SenderEmail", Value = "value@email.com" });
        customAttributes.Add(new CustomAttribute { Key = "Message", Value = "value" });
        //Act
        var result = shoppingCartGiftVoucherValidator.Validate(new ShoppingCartGiftVoucherValidatorRecord(
            new Customer(), new Product {
                GiftVoucherTypeId = GiftVoucherType.Virtual
            }, new ShoppingCartItem { Attributes = customAttributes }));
        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ShoppingCartGiftVoucherValidator_Fail_Wrong_SenderEmail()
    {
        //Arrange
        var shoppingCartGiftVoucherValidator = new ShoppingCartGiftVoucherValidator(_translationServiceMock.Object);
        var customAttributes = new List<CustomAttribute>();
        customAttributes.Add(new CustomAttribute { Key = "RecipientName", Value = "value" });
        customAttributes.Add(new CustomAttribute { Key = "RecipientEmail", Value = "value@emai.com" });
        customAttributes.Add(new CustomAttribute { Key = "SenderName", Value = "value" });
        customAttributes.Add(new CustomAttribute { Key = "SenderEmail", Value = "@email.com" });
        customAttributes.Add(new CustomAttribute { Key = "Message", Value = "value" });
        //Act
        var result = shoppingCartGiftVoucherValidator.Validate(new ShoppingCartGiftVoucherValidatorRecord(
            new Customer(), new Product {
                GiftVoucherTypeId = GiftVoucherType.Virtual
            }, new ShoppingCartItem { Attributes = customAttributes }));
        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartCheckoutAttributesValidator_Success()
    {
        //Arrange
        var shoppingCartCheckoutValidator = new ShoppingCartCheckoutAttributesValidator(_translationServiceMock.Object,
            _checkoutAttributeParserMock.Object, _checkoutAttributeServiceMock.Object);

        _checkoutAttributeParserMock.Setup(x => x.ParseCheckoutAttributes(It.IsAny<IList<CustomAttribute>>()))
            .Returns(() => Task.FromResult((IList<CheckoutAttribute>)new List<CheckoutAttribute> {
                new() {
                    Id = "1", Name = "test", IsRequired = true, AttributeControlTypeId = AttributeControlType.TextBox
                }
            }));

        _checkoutAttributeServiceMock.Setup(x => x.GetAllCheckoutAttributes(It.IsAny<string>(), true, false))
            .Returns(() => Task.FromResult((IList<CheckoutAttribute>)new List<CheckoutAttribute> {
                new() {
                    Id = "1", Name = "test", IsRequired = true, AttributeControlTypeId = AttributeControlType.TextBox
                }
            }));

        _checkoutAttributeParserMock.Setup(x => x.IsConditionMet(
                It.IsAny<CheckoutAttribute>(), It.IsAny<IList<CustomAttribute>>()))
            .Returns(() => Task.FromResult((bool?)true));

        //Act
        var result = await shoppingCartCheckoutValidator.ValidateAsync(
            new ShoppingCartCheckoutAttributesValidatorRecord(
                new Customer(),
                new Store(),
                new List<ShoppingCartItem> { new() },
                new List<CustomAttribute> { new() { Key = "1", Value = "test" } }
            ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartCheckoutAttributesValidator_Fail()
    {
        //Arrange
        var shoppingCartCheckoutValidator = new ShoppingCartCheckoutAttributesValidator(_translationServiceMock.Object,
            _checkoutAttributeParserMock.Object, _checkoutAttributeServiceMock.Object);

        _checkoutAttributeParserMock.Setup(x => x.ParseCheckoutAttributes(It.IsAny<IList<CustomAttribute>>()))
            .Returns(() => Task.FromResult((IList<CheckoutAttribute>)new List<CheckoutAttribute> {
                new() {
                    Id = "1", Name = "test", IsRequired = true, AttributeControlTypeId = AttributeControlType.TextBox,
                    ValidationMinLength = 10
                }
            }));

        _checkoutAttributeServiceMock.Setup(x => x.GetAllCheckoutAttributes(It.IsAny<string>(), true, false))
            .Returns(() => Task.FromResult((IList<CheckoutAttribute>)new List<CheckoutAttribute> {
                new() {
                    Id = "1", Name = "test", IsRequired = true, AttributeControlTypeId = AttributeControlType.TextBox,
                    ValidationMinLength = 10
                }
            }));

        _checkoutAttributeParserMock.Setup(x => x.IsConditionMet(
                It.IsAny<CheckoutAttribute>(), It.IsAny<IList<CustomAttribute>>()))
            .Returns(() => Task.FromResult((bool?)true));

        //Act
        var result = await shoppingCartCheckoutValidator.ValidateAsync(
            new ShoppingCartCheckoutAttributesValidatorRecord(
                new Customer(),
                new Store(),
                new List<ShoppingCartItem> { new() },
                new List<CustomAttribute> { new() { Key = "1", Value = "" } }
            ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartInventoryProductValidator_Success()
    {
        //Arrange
        var shoppingCartInventoryValidator = new ShoppingCartInventoryProductValidator(_translationServiceMock.Object,
            _productServiceMock.Object, _stockQuantityService, new ShoppingCartSettings());

        //Act
        var result = await shoppingCartInventoryValidator.ValidateAsync(new ShoppingCartInventoryProductValidatorRecord(
            new Customer(),
            new Product {
                Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock
            },
            new ShoppingCartItem { ProductId = "1", Quantity = 1 }
        ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartInventoryProductValidator_Fail()
    {
        //Arrange
        var shoppingCartInventoryValidator = new ShoppingCartInventoryProductValidator(_translationServiceMock.Object,
            _productServiceMock.Object, _stockQuantityService, new ShoppingCartSettings());

        //Act
        var result = await shoppingCartInventoryValidator.ValidateAsync(new ShoppingCartInventoryProductValidatorRecord(
            new Customer(),
            new Product {
                Id = "1", StockQuantity = 0, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock
            },
            new ShoppingCartItem { ProductId = "1", Quantity = 1 }
        ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartItemAttributeValidator_Success()
    {
        //Arrange
        var shoppingCartItemAttributeValidator = new ShoppingCartItemAttributeValidator(_translationServiceMock.Object,
            _productServiceMock.Object, _productAttributeService.Object);

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productAttributeService.Setup(x => x.GetProductAttributeById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new ProductAttribute { Name = "test" }));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var mapping = new ProductAttributeMapping {
            Id = "1", ProductAttributeId = "1", AttributeControlTypeId = AttributeControlType.DropdownList,
            IsRequired = true
        };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "1", Name = "a" });
        product.ProductAttributeMappings.Add(mapping);

        var attributes = new List<CustomAttribute> { new() { Key = "1", Value = "1" } };

        //Act
        var result = await shoppingCartItemAttributeValidator.ValidateAsync(
            new ShoppingCartItemAttributeValidatorRecord(
                new Customer(),
                product,
                new ShoppingCartItem { ProductId = "1", Quantity = 1, Attributes = attributes },
                false
            ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartItemAttributeValidator_Fail()
    {
        //Arrange
        var shoppingCartItemAttributeValidator = new ShoppingCartItemAttributeValidator(_translationServiceMock.Object,
            _productServiceMock.Object, _productAttributeService.Object);

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productAttributeService.Setup(x => x.GetProductAttributeById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new ProductAttribute { Name = "test" }));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var mapping = new ProductAttributeMapping {
            Id = "1", ProductAttributeId = "1", AttributeControlTypeId = AttributeControlType.DropdownList,
            IsRequired = true
        };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "1", Name = "a" });
        product.ProductAttributeMappings.Add(mapping);

        var attributes = new List<CustomAttribute> { new() { Key = "2" } };

        //Act
        var result = await shoppingCartItemAttributeValidator.ValidateAsync(
            new ShoppingCartItemAttributeValidatorRecord(
                new Customer(),
                product,
                new ShoppingCartItem { ProductId = "1", Quantity = 1, Attributes = attributes },
                false
            ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartRequiredProductValidator_Success()
    {
        //Arrange
        var shoppingCartRequiredProductValidator =
            new ShoppingCartRequiredProductValidator(_translationServiceMock.Object, _productServiceMock.Object,
                new ShoppingCartSettings());

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productServiceMock.Setup(x => x.GetProductById("1", false))
            .Returns(() => Task.FromResult(new Product { Id = "1", Name = "test" }));

        _productServiceMock.Setup(x => x.GetProductById("2", false))
            .Returns(() => Task.FromResult(new Product { Id = "2", Name = "test" }));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            RequireOtherProducts = true,
            RequiredProductIds = "2"
        };
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" });
        //Act
        var result = await shoppingCartRequiredProductValidator.ValidateAsync(
            new ShoppingCartRequiredProductValidatorRecord(
                customer,
                new Store { Id = "1" },
                product,
                new ShoppingCartItem
                    { ProductId = "1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" }
            ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartRequiredProductValidator_Fail()
    {
        //Arrange
        var shoppingCartRequiredProductValidator =
            new ShoppingCartRequiredProductValidator(_translationServiceMock.Object, _productServiceMock.Object,
                new ShoppingCartSettings());

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productServiceMock.Setup(x => x.GetProductById("1", false))
            .Returns(() => Task.FromResult(new Product { Id = "1", Name = "test" }));

        _productServiceMock.Setup(x => x.GetProductById("2", false))
            .Returns(() => Task.FromResult(new Product { Id = "2", Name = "test" }));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            RequireOtherProducts = true,
            RequiredProductIds = "2"
        };
        var customer = new Customer();
        //Act
        var result = await shoppingCartRequiredProductValidator.ValidateAsync(
            new ShoppingCartRequiredProductValidatorRecord(
                customer,
                new Store { Id = "1" },
                product,
                new ShoppingCartItem
                    { ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" }
            ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartReservationProductValidator_Success()
    {
        //Arrange
        var shoppingCartReservationProductValidator =
            new ShoppingCartReservationProductValidator(_translationServiceMock.Object,
                _productReservationServiceMock.Object);

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productServiceMock.Setup(x => x.GetProductById("1", false))
            .Returns(() => Task.FromResult(new Product { Id = "1", Name = "test" }));


        _productReservationServiceMock.Setup(x => x.GetCustomerReservationsHelpers(It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<CustomerReservationsHelper>)new List<CustomerReservationsHelper>
                { new() { Id = "1", CustomerId = "1", ShoppingCartItemId = "2" } }));

        _productReservationServiceMock.Setup(x =>
                x.GetProductReservationsByProductId(It.IsAny<string>(), true, null, 0, int.MaxValue))
            .Returns(() => Task.FromResult((IPagedList<ProductReservation>)
                new PagedList<ProductReservation> {
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(4).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(3).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(2).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(1).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.Date, Resource = "" }
                }
            ));


        var product = new Product
            { Id = "1", ProductTypeId = ProductType.Reservation, IntervalUnitId = IntervalUnit.Day };

        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem {
            Id = "1", ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1"
        });
        //Act
        var result = await shoppingCartReservationProductValidator.ValidateAsync(
            new ShoppingCartReservationProductValidatorRecord(
                customer,
                product,
                new ShoppingCartItem {
                    ProductId = "1",
                    Quantity = 1,
                    ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
                    StoreId = "1",
                    RentalStartDateUtc = DateTime.UtcNow.AddDays(1).Date,
                    RentalEndDateUtc = DateTime.UtcNow.AddDays(2).Date
                }
            ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartReservationProductValidator_Fail()
    {
        //Arrange
        var shoppingCartReservationProductValidator =
            new ShoppingCartReservationProductValidator(_translationServiceMock.Object,
                _productReservationServiceMock.Object);

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        _productServiceMock.Setup(x => x.GetProductById("1", false))
            .Returns(() => Task.FromResult(new Product { Id = "1", Name = "test" }));


        _productReservationServiceMock.Setup(x => x.GetCustomerReservationsHelpers(It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<CustomerReservationsHelper>)new List<CustomerReservationsHelper>
                { new() { Id = "1", CustomerId = "1", ShoppingCartItemId = "2" } }));

        _productReservationServiceMock.Setup(x =>
                x.GetProductReservationsByProductId(It.IsAny<string>(), true, null, 0, int.MaxValue))
            .Returns(() => Task.FromResult((IPagedList<ProductReservation>)
                new PagedList<ProductReservation> {
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(10).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(9).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(8).Date, Resource = "" },
                    new() { Id = "1", Date = DateTime.UtcNow.AddDays(7).Date, Resource = "" }
                }
            ));


        var product = new Product
            { Id = "1", ProductTypeId = ProductType.Reservation, IntervalUnitId = IntervalUnit.Day };

        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem {
            Id = "1", ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1"
        });
        //Act
        var result = await shoppingCartReservationProductValidator.ValidateAsync(
            new ShoppingCartReservationProductValidatorRecord(
                customer,
                product,
                new ShoppingCartItem {
                    ProductId = "1",
                    Quantity = 1,
                    ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
                    StoreId = "1",
                    RentalStartDateUtc = DateTime.UtcNow.AddDays(1).Date,
                    RentalEndDateUtc = DateTime.UtcNow.AddDays(2).Date
                }
            ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartCommonWarningsValidator_Success()
    {
        //Arrange
        var shoppingCartCommonWarningsValidator = new ShoppingCartCommonWarningsValidator(
            _translationServiceMock.Object, _permissionServiceMock.Object, new ShoppingCartSettings {
                MiniCartProductNumber = 1,
                MaximumShoppingCartItems = 100
            });
        _permissionServiceMock.Setup(x => x.Authorize(It.IsAny<Permission>(), It.IsAny<Customer>()))
            .Returns(async () => await Task.FromResult(true));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" });
        //Act
        var result = await shoppingCartCommonWarningsValidator.ValidateAsync(
            new ShoppingCartCommonWarningsValidatorRecord(
                customer,
                new Store { Id = "1" },
                new List<ShoppingCartItem> {
                    new() {
                        ProductId = "1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1"
                    }
                },
                product, ShoppingCartType.ShoppingCart, null, null, 1, "")
        );

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartCommonWarningsValidator_Fail()
    {
        //Arrange
        var shoppingCartCommonWarningsValidator = new ShoppingCartCommonWarningsValidator(
            _translationServiceMock.Object, _permissionServiceMock.Object, new ShoppingCartSettings {
                MiniCartProductNumber = 1,
                MaximumShoppingCartItems = 100
            });
        _permissionServiceMock.Setup(x => x.Authorize(It.IsAny<Permission>(), It.IsAny<Customer>()))
            .Returns(async () => await Task.FromResult(false));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ProductId = "2", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" });
        //Act
        var result = await shoppingCartCommonWarningsValidator.ValidateAsync(
            new ShoppingCartCommonWarningsValidatorRecord(
                customer,
                new Store { Id = "1" },
                new List<ShoppingCartItem> {
                    new() {
                        ProductId = "1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1"
                    }
                },
                product, ShoppingCartType.ShoppingCart, null, null, 1, "")
        );

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartStandardValidator_Success()
    {
        //Arrange
        var shoppingCartStandardValidator =
            new ShoppingCartStandardValidator(_translationServiceMock.Object, _aclServiceMock.Object);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<Customer>())).Returns(() => true);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<string>())).Returns(() => true);

        var product = new Product {
            Id = "1",
            Published = true
        };
        var customer = new Customer();
        //Act
        var result = await shoppingCartStandardValidator.ValidateAsync(new ShoppingCartStandardValidatorRecord(
            customer,
            product,
            new ShoppingCartItem
                { ProductId = "1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" }
        ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartStandardValidator_Fail()
    {
        //Arrange
        var shoppingCartStandardValidator =
            new ShoppingCartStandardValidator(_translationServiceMock.Object, _aclServiceMock.Object);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<Customer>())).Returns(() => false);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Product>(), It.IsAny<string>())).Returns(() => true);

        var product = new Product {
            Id = "1",
            Published = true
        };
        var customer = new Customer();
        //Act
        var result = await shoppingCartStandardValidator.ValidateAsync(new ShoppingCartStandardValidatorRecord(
            customer,
            product,
            new ShoppingCartItem
                { ProductId = "1", Quantity = 1, ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "1" }
        ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartWarningsValidator_Success()
    {
        //Arrange
        var shoppingCartWarningsValidator =
            new ShoppingCartWarningsValidator(_translationServiceMock.Object, _productServiceMock.Object);

        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(() => Task.FromResult(new Product()));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var mapping = new ProductAttributeMapping {
            Id = "1", ProductAttributeId = "1", AttributeControlTypeId = AttributeControlType.DropdownList,
            IsRequired = true
        };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "1", Name = "a" });
        product.ProductAttributeMappings.Add(mapping);

        var attributes = new List<CustomAttribute> { new() { Key = "1", Value = "1" } };

        //Act
        var result = await shoppingCartWarningsValidator.ValidateAsync(new ShoppingCartWarningsValidatorRecord(
            new Customer(),
            new Store(),
            new List<ShoppingCartItem> {
                new() { ProductId = "1", Quantity = 1, Attributes = attributes }
            }
        ));

        //Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task ShoppingCartWarningsValidator_Fail()
    {
        //Arrange
        var shoppingCartWarningsValidator =
            new ShoppingCartWarningsValidator(_translationServiceMock.Object, _productServiceMock.Object);

        _productServiceMock.Setup(x => x.GetProductById("1", false))
            .Returns(() => Task.FromResult(new Product
                { IsRecurring = true, RecurringCyclePeriodId = RecurringCyclePeriod.Weeks }));
        _productServiceMock.Setup(x => x.GetProductById("2", false))
            .Returns(() => Task.FromResult(new Product { IsRecurring = false }));

        var product = new Product {
            Id = "1", StockQuantity = 1, OrderMinimumQuantity = 1, OrderMaximumQuantity = 10,
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var mapping = new ProductAttributeMapping {
            Id = "1", ProductAttributeId = "1", AttributeControlTypeId = AttributeControlType.DropdownList,
            IsRequired = true
        };
        mapping.ProductAttributeValues.Add(new ProductAttributeValue { Id = "1", Name = "a" });
        product.ProductAttributeMappings.Add(mapping);

        var attributes = new List<CustomAttribute> { new() { Key = "1", Value = "1" } };

        //Act
        var result = await shoppingCartWarningsValidator.ValidateAsync(new ShoppingCartWarningsValidatorRecord(
            new Customer(),
            new Store(),
            new List<ShoppingCartItem> {
                new() { ProductId = "1", Quantity = 1, Attributes = attributes },
                new() { ProductId = "2", Quantity = 1, Attributes = attributes }
            }
        ));

        //Assert
        Assert.IsFalse(result.IsValid);
    }
}