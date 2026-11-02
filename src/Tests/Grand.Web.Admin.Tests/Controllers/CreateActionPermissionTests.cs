using System.Reflection;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class CreateActionPermissionTests
{
    // Opening a create form and submitting it are one operation, so a form that requires
    // PermissionActionName.Create has to be submitted under the same action.
    [TestMethod]
    public void CreateFormPost_RequiresTheSameActionAsItsForm()
    {
        var forms = new[] { typeof(BaseProductController).Assembly, typeof(VendorController).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(Controller).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .GroupBy(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .Where(action => action.Any(method => !IsPost(method) && RequiredAction(method) == PermissionActionName.Create))
            .ToList();

        var mismatched = forms
            .Where(action => action.Any(method => IsPost(method) && RequiredAction(method) != PermissionActionName.Create))
            .Select(action => action.Key)
            .ToList();

        Assert.IsTrue(forms.Any(action => action.Key == "BaseProductController.Create"));
        Assert.AreEqual(0, mismatched.Count, string.Join(", ", mismatched));
    }

    private static bool IsPost(MethodInfo method) => method.IsDefined(typeof(HttpPostAttribute), false);

    private static string RequiredAction(MethodInfo method) =>
        method.GetCustomAttribute<PermissionAuthorizeActionAttribute>()?.PermissionAction;
}
