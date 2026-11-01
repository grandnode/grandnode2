using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Stores;
using Grand.Web.AdminShared.Validators.Stores;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class StoreValidatorOwnerTests
{
    private const string InvalidText = "owner not allowed";

    private StoreValidator _validator;

    [TestInitialize]
    public void Init()
    {
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);
        translation.Setup(x => x.GetResource("Admin.Configuration.Stores.Fields.OwnerCustomerId.Invalid"))
            .Returns(InvalidText);
        var storeService = new Mock<IStoreViewModelService>();
        storeService.Setup(x => x.IsOwnerCandidate("s1", "admin")).ReturnsAsync(true);

        _validator = new StoreValidator([], translation.Object, storeService.Object);
    }

    private static StoreModel Model(string ownerCustomerId)
    {
        return new StoreModel {
            Id = "s1", Name = "Store", Shortcut = "st", Url = "https://store.example/",
            OwnerCustomerId = ownerCustomerId
        };
    }

    [TestMethod]
    public async Task Validator_EmptyOwner_IsValid()
    {
        var result = await _validator.ValidateAsync(Model(null));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validator_Candidate_IsValid()
    {
        var result = await _validator.ValidateAsync(Model("admin"));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validator_OwnerNotACandidate_Fails()
    {
        var result = await _validator.ValidateAsync(Model("posted-id"));

        Assert.IsTrue(result.Errors.Any(x => x.ErrorMessage == InvalidText));
    }
}
