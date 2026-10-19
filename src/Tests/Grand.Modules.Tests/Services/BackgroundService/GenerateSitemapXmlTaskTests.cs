using Grand.Business.Core.Commands.System.Common;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class GenerateSitemapXmlTaskTests
{
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediaFileStore> _mediaFileStoreMock;
    private Mock<IMediator> _mediatorMock;
    private GenerateSitemapXmlTask _task;

    [TestInitialize]
    public void Init()
    {
        var languages = new List<Language> {
            new() { Id = "1", UniqueSeoCode = "en" },
            new() { Id = "2", UniqueSeoCode = "pl" }
        };
        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(c => c.GetAllStores()).ReturnsAsync(new List<Store> { new() { Id = "store" } });
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(languages);
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(c => c.Send(It.IsAny<GetSitemapXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<urlset />");
        _mediaFileStoreMock = new Mock<IMediaFileStore>();
        _task = new GenerateSitemapXmlTask(_mediatorMock.Object, _languageServiceMock.Object,
            storeServiceMock.Object, _mediaFileStoreMock.Object);
    }

    [TestMethod]
    public async Task Execute_WritesASitemapPerLanguage()
    {
        await _task.Execute(CancellationToken.None);

        _mediaFileStoreMock.Verify(c => c.WriteAllText("sitemap-en.xml", "<urlset />"), Times.Once);
        _mediaFileStoreMock.Verify(c => c.WriteAllText("sitemap-pl.xml", "<urlset />"), Times.Once);
    }

    [TestMethod]
    public async Task Execute_CancelledAfterTheFirstLanguage_DoesNotGenerateTheNext()
    {
        using var cts = new CancellationTokenSource();
        _mediaFileStoreMock.Setup(c => c.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => cts.Cancel())
            .Returns(Task.CompletedTask);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => _task.Execute(cts.Token));

        //the language being generated is finished and written, the next one is not started
        _mediaFileStoreMock.Verify(c => c.WriteAllText("sitemap-en.xml", "<urlset />"), Times.Once);
        _mediatorMock.Verify(c => c.Send(It.Is<GetSitemapXmlCommand>(x => x.Language.UniqueSeoCode == "pl"),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
