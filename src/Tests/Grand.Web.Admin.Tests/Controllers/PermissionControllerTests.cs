using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PermissionControllerTests
{
    private const string GroupId = "group-1";
    private const string SystemName = "ManageProducts";

    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<IGroupService> _groupServiceMock = new();
    private PermissionController _controller;

    [TestInitialize]
    public void Init()
    {
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "lang-1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _permissionServiceMock.Setup(p => p.GetPermissionActions(SystemName, GroupId))
            .ReturnsAsync(new List<PermissionAction>());

        _controller = new PermissionController(contextAccessorMock.Object, _permissionServiceMock.Object,
            _groupServiceMock.Object, new Mock<ITranslationService>().Object);
    }

    private void GroupExists() =>
        _groupServiceMock.Setup(g => g.GetCustomerGroupById(GroupId))
            .ReturnsAsync(new CustomerGroup { Id = GroupId, Name = "Group" });

    private void PermissionExists() =>
        _permissionServiceMock.Setup(p => p.GetPermissionBySystemName(SystemName))
            .ReturnsAsync(new Permission { SystemName = SystemName, Actions = new List<string> { "List", "Edit" } });

    private void VerifyNothingWritten()
    {
        _permissionServiceMock.Verify(p => p.DeletePermissionAction(It.IsAny<PermissionAction>()), Times.Never);
        _permissionServiceMock.Verify(p => p.InsertPermissionAction(It.IsAny<PermissionAction>()), Times.Never);
    }

    [TestMethod]
    public async Task PermissionsActionGet_ShowsTheActionsOfAKnownGroupAndPermission()
    {
        GroupExists();
        PermissionExists();

        var result = await _controller.PermissionsAction(SystemName, GroupId);

        var model = (PermissionActionModel)((ViewResult)result).Model;
        Assert.AreEqual("Group", model.CustomerGroupName);
        CollectionAssert.AreEqual(new List<string> { "List", "Edit" }, model.AvailableActions.ToList());
    }

    [TestMethod]
    public async Task PermissionsActionGet_UnknownGroup_IsNotFound()
    {
        PermissionExists();

        var result = await _controller.PermissionsAction(SystemName, GroupId);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task PermissionsActionGet_UnknownPermission_IsNotFound()
    {
        GroupExists();

        var result = await _controller.PermissionsAction(SystemName, GroupId);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task PermissionsActionPost_DeniesTheActionsNotSelected()
    {
        GroupExists();
        PermissionExists();

        var result = await _controller.PermissionsAction(new PermissionActionSaveModel(SystemName, GroupId, new List<string> { "List" }));

        Assert.IsInstanceOfType<ViewResult>(result);
        _permissionServiceMock.Verify(p => p.InsertPermissionAction(It.Is<PermissionAction>(a =>
            a.Action == "Edit" && a.CustomerGroupId == GroupId && a.SystemName == SystemName)), Times.Once);
        _permissionServiceMock.Verify(p => p.InsertPermissionAction(It.IsAny<PermissionAction>()), Times.Once);
    }

    [TestMethod]
    public async Task PermissionsActionPost_UnknownGroup_IsNotFoundAndWritesNothing()
    {
        PermissionExists();

        var result = await _controller.PermissionsAction(new PermissionActionSaveModel(SystemName, GroupId, null));

        Assert.IsInstanceOfType<NotFoundResult>(result);
        VerifyNothingWritten();
    }

    [TestMethod]
    public async Task PermissionsActionPost_UnknownPermission_IsNotFoundAndWritesNothing()
    {
        GroupExists();
        _permissionServiceMock.Setup(p => p.GetPermissionActions(SystemName, GroupId))
            .ReturnsAsync(new List<PermissionAction> { new() { Action = "Edit" } });

        var result = await _controller.PermissionsAction(new PermissionActionSaveModel(SystemName, GroupId, null));

        Assert.IsInstanceOfType<NotFoundResult>(result);
        VerifyNothingWritten();
    }
}
