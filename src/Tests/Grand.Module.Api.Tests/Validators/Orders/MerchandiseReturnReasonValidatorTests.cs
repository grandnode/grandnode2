using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Module.Api.DTOs.Orders;
using Grand.Module.Api.Validators.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Validators.Orders;

[TestClass]
public class MerchandiseReturnReasonValidatorTests
{
    private readonly Mock<IMerchandiseReturnService> _merchandiseReturnService = new();
    private MerchandiseReturnReasonValidator _validator;

    [TestInitialize]
    public void Init()
    {
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>())).Returns("message");
        _validator = new MerchandiseReturnReasonValidator([], translation.Object,
            new Mock<IContextAccessor>().Object, _merchandiseReturnService.Object);
    }

    [TestMethod]
    public async Task Validate_EmptyName_Error()
    {
        var result = await _validator.ValidateAsync(new MerchandiseReturnReasonDto { Name = "" });

        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == "Name"));
    }

    [TestMethod]
    public async Task Validate_UnknownId_Error()
    {
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnReasonById("missing"))
            .ReturnsAsync((MerchandiseReturnReason)null);

        var result = await _validator.ValidateAsync(
            new MerchandiseReturnReasonDto { Name = "n", Id = "missing" });

        Assert.IsTrue(result.Errors.Any());
    }

    [TestMethod]
    public async Task Validate_Valid_NoErrors()
    {
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnReasonById("r1"))
            .ReturnsAsync(new MerchandiseReturnReason { Id = "r1" });

        var result = await _validator.ValidateAsync(new MerchandiseReturnReasonDto { Name = "n", Id = "r1" });

        Assert.IsFalse(result.Errors.Any());
    }
}
