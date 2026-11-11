using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Orders;
using Grand.Web.Validators.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Orders;

[TestClass]
public class MerchandiseReturnValidatorTests
{
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetParseCustomAddressAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomAttribute>());
    }

    private MerchandiseReturnValidator Validator(OrderSettings orderSettings = null)
    {
        var addressAttributeParser = new Mock<IAddressAttributeParser>();
        addressAttributeParser.Setup(x => x.GetAttributeWarnings(It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        return new MerchandiseReturnValidator(new List<IValidatorConsumer<MerchandiseReturnModel>>(),
            orderSettings ?? new OrderSettings(), _mediatorMock.Object, addressAttributeParser.Object,
            translationService.Object, contextAccessor.Object);
    }

    private static MerchandiseReturnModel Model(params (string Id, int Quantity)[] items)
    {
        var model = new MerchandiseReturnModel { OrderId = "o1" };
        foreach (var (id, quantity) in items)
            model.Items.Add(new MerchandiseReturnModel.OrderItemModel { Id = id, Quantity = quantity });
        return model;
    }

    [TestMethod]
    public async Task Validate_NothingSubmitted_IsRejected()
    {
        var result = await Validator().ValidateAsync(Model(("a", 0), ("b", 0)));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.NoItemsSubmitted"));
    }

    [TestMethod]
    public async Task Validate_ItemsOfTwoVendors_AreValid()
    {
        //each vendor gets a return of its own
        var result = await Validator().ValidateAsync(Model(("a", 1), ("b", 1)));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_LeavesTheQuantityToTheSubmit()
    {
        //the quantity left is checked once, under the order's lock, when the return is saved
        await Validator().ValidateAsync(Model(("a", 1)));

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Validate_PickupDateRequiredAndMissing_IsRejected()
    {
        var result = await Validator(new OrderSettings {
            MerchandiseReturns_AllowToSpecifyPickupDate = true, MerchandiseReturns_PickupDateRequired = true
        }).ValidateAsync(Model(("a", 1)));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.PickupDateRequired"));
    }
}
