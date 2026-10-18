using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetShoppingCartTokensCommandHandlerTests
{
    [TestMethod]
    public async Task Handle_FormatsAttributesForTheMessageContextLanguageAndHost()
    {
        //Arrange
        var product = new Product { Id = "p1" };
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(product);
        var formatter = new Mock<IProductAttributeFormatter>();
        var customer = new Customer();
        var item = new ShoppingCartItem { ProductId = "p1" };
        item.Attributes.Add(new CustomAttribute { Key = "1", Value = "1" });
        customer.ShoppingCartItems.Add(item);
        var command = new GetShoppingCartTokensCommand {
            Context = new CalculationContext(customer, new Store { Id = "s1" }, new Currency(),
                TaxDisplayType.ExcludingTax),
            Language = new Language { Id = "pl" },
            Host = new DomainHost { Url = "https://shop.example/" }
        };
        var handler = new GetShoppingCartTokensCommandHandler(new Mock<IPictureService>().Object,
            productService.Object, formatter.Object);

        //Act
        await handler.Handle(command, CancellationToken.None);

        //Assert
        formatter.Verify(x => x.FormatAttributes(product, item.Attributes, command.Context, command.Language,
            command.Host, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ItemWithoutAttributes_DoesNotRepeatThePreviousItemsDescription_AndSkipsDeletedProducts()
    {
        //Arrange
        var product = new Product { Id = "p1" };
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(product);
        var formatter = new Mock<IProductAttributeFormatter>();
        formatter.Setup(x => x.FormatAttributes(product, It.IsAny<IList<CustomAttribute>>(),
                It.IsAny<CalculationContext>(), It.IsAny<Language>(), It.IsAny<DomainHost>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>()))
            .ReturnsAsync("Size: XL");
        var customer = new Customer();
        var withAttributes = new ShoppingCartItem { ProductId = "p1" };
        withAttributes.Attributes.Add(new CustomAttribute { Key = "1", Value = "1" });
        customer.ShoppingCartItems.Add(withAttributes);
        customer.ShoppingCartItems.Add(new ShoppingCartItem { ProductId = "deleted" });
        customer.ShoppingCartItems.Add(new ShoppingCartItem { ProductId = "p1" });
        var command = new GetShoppingCartTokensCommand {
            Context = new CalculationContext(customer, new Store { Id = "s1" }, new Currency(),
                TaxDisplayType.ExcludingTax),
            Language = new Language { Id = "pl" },
            Host = new DomainHost { Url = "https://shop.example/" }
        };
        var handler = new GetShoppingCartTokensCommandHandler(new Mock<IPictureService>().Object,
            productService.Object, formatter.Object);

        //Act
        var result = await handler.Handle(command, CancellationToken.None);

        //Assert
        var items = result.Items.ToList();
        Assert.HasCount(2, items);
        Assert.AreEqual("Size: XL", items[0].AttributeDescription);
        Assert.AreEqual(string.Empty, items[1].AttributeDescription);
    }
}
