using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Domain;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerContactFormViewModelServiceTests
{
    private Mock<IContactUsService> _contactUsServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private CustomerContactFormViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg => cfg.AddProfile<ContactUsProfile>()));

        _contactUsServiceMock = new Mock<IContactUsService>();
        _storeServiceMock = new Mock<IStoreService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns<DateTime, DateTimeKind>((d, _) => d);

        _service = new CustomerContactFormViewModelService(_contactUsServiceMock.Object, _storeServiceMock.Object,
            dateTimeServiceMock.Object);
    }

    [TestMethod]
    public async Task PrepareContactFormModel_QueriesTheCustomersFormsWithZeroBasedPage()
    {
        _contactUsServiceMock.Setup(c => c.GetAllContactUs(null, null, "", "", "c1", "", 1, 10))
            .ReturnsAsync(new PagedList<ContactUs>(new List<ContactUs>(), 1, 10, 0));

        var (models, totalCount) = await _service.PrepareContactFormModel("c1", 2, 10);

        Assert.AreEqual(0, models.Count());
        Assert.AreEqual(0, totalCount);
        _contactUsServiceMock.Verify(c => c.GetAllContactUs(null, null, "", "", "c1", "", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task PrepareContactFormModel_HidesTheEnquiry_AndNamesTheStore()
    {
        var known = new ContactUs {
            Id = "f1", StoreId = "s1", FullName = "Jan", Email = "jan@example.com", Enquiry = "secret"
        };
        var orphan = new ContactUs { Id = "f2", StoreId = "gone", FullName = "Ewa", Email = "ewa@example.com" };
        _contactUsServiceMock.Setup(c => c.GetAllContactUs(null, null, "", "", "c1", "", 0, 10))
            .ReturnsAsync(new PagedList<ContactUs>(new List<ContactUs> { known, orphan }, 0, 10, 2));
        _storeServiceMock.Setup(s => s.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1", Shortcut = "Main" });
        _storeServiceMock.Setup(s => s.GetStoreById("gone")).ReturnsAsync((Store)null);

        var (models, totalCount) = await _service.PrepareContactFormModel("c1", 1, 10);

        var list = models.ToList();
        Assert.AreEqual(2, totalCount);
        Assert.AreEqual("", list[0].Enquiry);
        Assert.AreEqual("Jan - jan@example.com", list[0].Email);
        Assert.AreEqual("Main", list[0].Store);
        Assert.AreEqual("-empty-", list[1].Store);
    }
}
