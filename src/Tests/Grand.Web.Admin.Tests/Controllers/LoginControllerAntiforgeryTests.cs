using Grand.Web.AdminShared.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class LoginControllerAntiforgeryTests
{
    [TestMethod]
    public void EveryPostAction_ValidatesTheAntiforgeryToken()
    {
        //the panel login controllers do not inherit the panel base controllers, so each POST carries the attribute itself
        var posts = typeof(BaseLoginController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.IsTrue(posts.Count > 0);
        foreach (var post in posts)
            Assert.IsNotNull(post.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(),
                $"{post.Name} is missing [AutoValidateAntiforgeryToken].");
    }
}
