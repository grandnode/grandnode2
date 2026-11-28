using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Campaigns;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class CampaignControllerTests
{
    private Mock<ICampaignService> _campaignService = null!;
    private CampaignController _controller = null!;

    public CampaignControllerTests()
    {
        _campaignService = new Mock<ICampaignService>();
        _campaignService.Setup(x => x.GetCampaignById("c1")).ReturnsAsync(new Campaign { Id = "c1" });
        var translations = new Mock<IRequestTranslationService>();
        translations.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        _controller = new CampaignController(_campaignService.Object, new Mock<ICampaignViewModelService>().Object,
            new Mock<IEmailAccountService>().Object, new Mock<INewsLetterSubscriptionService>().Object,
            translations.Object, new Mock<IContextAccessor>().Object, new Mock<IStoreService>().Object,
            new EmailAccountSettings());
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not an address")]
    public async Task SendTestEmail_NoValidAddress_SaysSoAndSendsNothing(string email)
    {
        var result = await _controller.SendTestEmail(new CampaignModel { Id = "c1", TestEmail = email });

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        var errors = _controller.TempData["grand.notifications.Error"] as List<string>;
        Assert.AreEqual("Admin.Common.WrongEmail", errors?.Single());
        _campaignService.Verify(x => x.SendCampaign(It.IsAny<Campaign>(), It.IsAny<EmailAccount>(), It.IsAny<string>(),
            It.IsAny<Grand.Domain.Directory.Currency>(), It.IsAny<Grand.Domain.Tax.TaxDisplayType>(),
            It.IsAny<Grand.Domain.Stores.DomainHost>()), Times.Never);
    }
}
