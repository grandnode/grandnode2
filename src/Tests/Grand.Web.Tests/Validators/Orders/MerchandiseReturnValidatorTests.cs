using Grand.Business.Core.Interfaces.Checkout.Orders;
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
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;

    [TestInitialize]
    public void Init()
    {
        //the store offers no reasons or actions unless a test gives it some
        _merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons("s1"))
            .ReturnsAsync(new List<MerchandiseReturnReason>());
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions("s1"))
            .ReturnsAsync(new List<MerchandiseReturnAction>());
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
        translationService.Setup(x => x.GetResource("Fields.IsRequired")).Returns("{0} is required");
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        return new MerchandiseReturnValidator(new List<IValidatorConsumer<MerchandiseReturnModel>>(),
            orderSettings ?? new OrderSettings(), _mediatorMock.Object, addressAttributeParser.Object,
            translationService.Object, contextAccessor.Object, _merchandiseReturnService.Object);
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

    private void StoreOffersReasonsAndActions()
    {
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons("s1"))
            .ReturnsAsync(new List<MerchandiseReturnReason> { new() { Id = "wrong-product", FreeReturnShipping = true } });
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions("s1"))
            .ReturnsAsync(new List<MerchandiseReturnAction> { new() { Id = "refund" } });
    }

    [TestMethod]
    public async Task Validate_ReasonNotChosen_IsRejected()
    {
        StoreOffersReasonsAndActions();

        var result = await Validator().ValidateAsync(Model(("a", 1)));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.ReturnReason is required"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Validate_ActionNotChosen_IsValid(bool hideRequestedAction)
    {
        //the requested action is optional, shown or hidden
        StoreOffersReasonsAndActions();
        var model = Model(("a", 1));
        model.Items[0].MerchandiseReturnReasonId = "wrong-product";

        var result = await Validator(new OrderSettings { MerchandiseReturns_HideRequestedAction = hideRequestedAction })
            .ValidateAsync(model);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_ReasonAndActionChosen_IsValid()
    {
        StoreOffersReasonsAndActions();
        var model = Model(("a", 1), ("b", 0));
        model.Items[0].MerchandiseReturnReasonId = "wrong-product";
        model.Items[0].MerchandiseReturnActionId = "refund";

        //the item not returned needs no reason or action
        var result = await Validator().ValidateAsync(model);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_ReasonOfAnotherStore_IsRejected()
    {
        StoreOffersReasonsAndActions();
        var model = Model(("a", 1));
        model.Items[0].MerchandiseReturnReasonId = "other-store-reason";
        model.Items[0].MerchandiseReturnActionId = "refund";

        var result = await Validator().ValidateAsync(model);

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == "MerchandiseReturns.ReturnReason is required"));
    }

    [TestMethod]
    public async Task Validate_StoreOffersNoReasonsOrActions_NoneRequired()
    {
        var result = await Validator().ValidateAsync(Model(("a", 1)));

        Assert.IsTrue(result.IsValid);
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
