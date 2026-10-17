using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.AdminShared.Validators.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class RobotsTxtValidatorTests
{
    private readonly RobotsTxtValidator _validator = new(new List<IValidatorConsumer<RobotsTxtModel>>(), Translations());

    private static IRequestTranslationService Translations()
    {
        var mock = new Mock<IRequestTranslationService>();
        mock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("message");
        return mock.Object;
    }

    [TestMethod]
    public async Task EmptyText_IsValid()
    {
        //an empty robots.txt allows everything; it has to be saveable to get back to the default
        var result = await _validator.ValidateAsync(new RobotsTxtModel { Name = "robots", Text = "" });
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task MissingName_IsInvalid()
    {
        var result = await _validator.ValidateAsync(new RobotsTxtModel { Name = "", Text = "User-agent: *" });
        Assert.IsFalse(result.IsValid);
    }
}
