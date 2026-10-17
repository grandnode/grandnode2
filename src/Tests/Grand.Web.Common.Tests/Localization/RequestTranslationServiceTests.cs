using Grand.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Localization;

[TestClass]
public class RequestTranslationServiceTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<RequestTranslationService> _serviceMock;
    private RequestTranslationService _service;

    [TestInitialize]
    public void Init()
    {
        _contextAccessorMock = new Mock<IContextAccessor>();
        _serviceMock = new Mock<RequestTranslationService>(Mock.Of<IRepository<TranslationResource>>(),
            Mock.Of<IMediator>(), _contextAccessorMock.Object) { CallBase = true };
        _service = _serviceMock.Object;
    }

    [TestMethod]
    public void GetResource_UsesWorkingLanguageOfTheRequest()
    {
        _contextAccessorMock.Setup(c => c.WorkContext.WorkingLanguage).Returns(new Language { Id = "pl" });
        _serviceMock.Setup(x => x.GetResource("name", "pl", "", false)).Returns("nazwa");

        var result = _service.GetResource("name");

        Assert.AreEqual("nazwa", result);
    }

    [TestMethod]
    public void GetResource_WithoutWorkingLanguage_ReturnsEmpty()
    {
        _contextAccessorMock.Setup(c => c.WorkContext.WorkingLanguage).Returns((Language)null);

        var result = _service.GetResource("name");

        Assert.AreEqual("", result);
        _serviceMock.Verify(
            x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public void GetResource_WithoutWorkContext_ReturnsEmpty()
    {
        _contextAccessorMock.Setup(c => c.WorkContext).Returns((IWorkContext)null);

        var result = _service.GetResource("name");

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetResource_WithLanguage_DoesNotReadTheRequest()
    {
        _serviceMock.Setup(x => x.GetResource("name", "en", "default", true)).Returns("value");

        var result = _service.GetResource("name", "en", "default", true);

        Assert.AreEqual("value", result);
        _contextAccessorMock.VerifyNoOtherCalls();
    }
}
