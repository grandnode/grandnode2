using Grand.Business.Common.Services.Pdf;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Pdf;

[TestClass]
public class HtmlToPdfServiceTests
{
    private Mock<ILanguageService> _languageServiceMock;
    private HtmlToPdfService _service;
    private Mock<IViewRenderService> _viewRenderServiceMock;

    [TestInitialize]
    public void Init()
    {
        _viewRenderServiceMock = new Mock<IViewRenderService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _service = new HtmlToPdfService(_viewRenderServiceMock.Object, new Mock<IRepository<Download>>().Object,
            _languageServiceMock.Object, new Mock<IStoreFilesContext>().Object,
            new Mock<IWebHostEnvironment>().Object, new Mock<ILogger<HtmlToPdfService>>().Object);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_WhenRequestCancelled_DoesNotWriteThePdf()
    {
        //Arrange
        _viewRenderServiceMock
            .Setup(v => v.RenderToStringAsync(It.IsAny<string>(), It.IsAny<ValueTuple<IList<Order>, string>>()))
            .ReturnsAsync("<html></html>");
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.PrintOrdersToPdf(stream, [new Order()], cancellationToken: cancellation.Token));
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public async Task PrintPackagingSlipsToPdf_WhenRequestCancelled_DoesNotWriteThePdf()
    {
        //Arrange
        _languageServiceMock.Setup(l => l.GetLanguageById("lang-1")).ReturnsAsync(new Language { Id = "lang-1" });
        _viewRenderServiceMock
            .Setup(v => v.RenderToStringAsync(It.IsAny<string>(), It.IsAny<IList<Shipment>>()))
            .ReturnsAsync("<html></html>");
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.PrintPackagingSlipsToPdf(stream, [new Shipment()], "lang-1", cancellation.Token));
        Assert.AreEqual(0, stream.Length);
    }
}
