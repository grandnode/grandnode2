using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CheckoutAttributeViewModelServiceTests
{
    private Mock<ICheckoutAttributeService> _checkoutAttributeServiceMock;
    private CheckoutAttributeViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _checkoutAttributeServiceMock = new Mock<ICheckoutAttributeService>();
        var parserMock = new Mock<ICheckoutAttributeParser>();
        parserMock.Setup(x => x.ParseCheckoutAttributes(It.IsAny<IList<CustomAttribute>>()))
            .ReturnsAsync(new List<CheckoutAttribute>());
        parserMock.Setup(x => x.ParseCheckoutAttributeValues(It.IsAny<IList<CustomAttribute>>()))
            .ReturnsAsync(new List<CheckoutAttributeValue>());

        _service = new CheckoutAttributeViewModelService(_checkoutAttributeServiceMock.Object, parserMock.Object,
            new Mock<ITranslationService>().Object, new Mock<ITaxCategoryService>().Object,
            new Mock<ICurrencyService>().Object, new CurrencySettings(), new Mock<IMeasureService>().Object,
            new MeasureSettings(), new Mock<IEnumTranslationService>().Object);
    }

    [TestMethod]
    public async Task PrepareConditionAttributes_OffersAttributesLimitedToCustomerGroups()
    {
        //Arrange
        var limited = new CheckoutAttribute {
            Id = "limited", Name = "Members gift", LimitedToGroups = true, CustomerGroups = ["members"],
            AttributeControlTypeId = AttributeControlType.DropdownList
        };
        _checkoutAttributeServiceMock
            .Setup(x => x.GetAllCheckoutAttributes(It.Is<CatalogAccess>(a => a.ShowHidden), false))
            .ReturnsAsync(new List<CheckoutAttribute> { limited });
        var model = new CheckoutAttributeModel();

        //Act
        await _service.PrepareConditionAttributes(model, new CheckoutAttribute { Id = "edited" });

        //Assert
        Assert.AreEqual(1, model.ConditionModel.ConditionAttributes.Count);
        Assert.AreEqual("limited", model.ConditionModel.ConditionAttributes[0].Id);
    }
}
