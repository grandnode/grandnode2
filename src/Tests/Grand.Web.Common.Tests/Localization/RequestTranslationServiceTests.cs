using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Localization;

[TestClass]
public class RequestTranslationServiceTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private RequestTranslationService _service;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _translationServiceMock = new Mock<ITranslationService>();
        _contextAccessorMock = new Mock<IContextAccessor>();
        _service = new RequestTranslationService(_translationServiceMock.Object, _contextAccessorMock.Object);
    }

    [TestMethod]
    public void GetResource_UsesWorkingLanguageOfTheRequest()
    {
        _contextAccessorMock.Setup(c => c.WorkContext.WorkingLanguage).Returns(new Language { Id = "pl" });
        _translationServiceMock.Setup(x => x.GetResource("name", "pl", "", false)).Returns("nazwa");

        var result = _service.GetResource("name");

        Assert.AreEqual("nazwa", result);
    }

    [TestMethod]
    public void GetResource_WithoutWorkingLanguage_ReturnsEmpty()
    {
        _contextAccessorMock.Setup(c => c.WorkContext.WorkingLanguage).Returns((Language)null);

        var result = _service.GetResource("name");

        Assert.AreEqual("", result);
        _translationServiceMock.Verify(
            x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public void GetResource_WithLanguage_DoesNotReadTheRequest()
    {
        _translationServiceMock.Setup(x => x.GetResource("name", "en", "default", true)).Returns("value");

        var result = _service.GetResource("name", "en", "default", true);

        Assert.AreEqual("value", result);
        _contextAccessorMock.VerifyNoOtherCalls();
    }
}
