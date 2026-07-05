using Grand.Module.Api.ApiExplorer;
using Grand.SharedKernel.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;

namespace Grand.Module.Api.Tests.ApiExplorer;

[TestClass]
public class MetadataApiDescriptionProviderTests
{
    [TestMethod]
    public void OnProvidersExecuting_PostAction_IgnoresFromServicesParameterAndKeepsBodyParameter()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostAction))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], BindingSource.Services),
            CreateControllerParameter(methodInfo.GetParameters()[1], BindingSource.Body)
        ]);

        var context = new ApiDescriptionProviderContext([action]);

        provider.OnProvidersExecuting(context);

        Assert.AreEqual(1, context.Results.Count);
        var apiDescription = context.Results[0];
        Assert.AreEqual(1, apiDescription.ParameterDescriptions.Count);
        Assert.AreEqual("model", apiDescription.ParameterDescriptions[0].Name);
    }

    [TestMethod]
    public void OnProvidersExecuting_GetAction_IgnoresFromServicesParameterAndKeepsQueryParameter()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.GetAction))!;

        var action = CreateActionDescriptor(methodInfo, "GET", [
            CreateControllerParameter(methodInfo.GetParameters()[0], BindingSource.Services),
            CreateControllerParameter(methodInfo.GetParameters()[1], BindingSource.Query)
        ]);

        var context = new ApiDescriptionProviderContext([action]);

        provider.OnProvidersExecuting(context);

        Assert.AreEqual(1, context.Results.Count);
        var apiDescription = context.Results[0];
        Assert.AreEqual(1, apiDescription.ParameterDescriptions.Count);
        Assert.AreEqual("searchTerm", apiDescription.ParameterDescriptions[0].Name);
    }

    private static MetadataApiDescriptionProvider CreateProvider()
    {
        return new MetadataApiDescriptionProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider(),
            new Mock<IActionResultTypeMapper>().Object,
            Options.Create(new RouteOptions()));
    }

    private static ControllerActionDescriptor CreateActionDescriptor(
        System.Reflection.MethodInfo methodInfo,
        string httpMethod,
        IList<ParameterDescriptor> parameters)
    {
        return new ControllerActionDescriptor {
            ControllerName = "TestApi",
            ActionName = methodInfo.Name,
            ControllerTypeInfo = typeof(TestApiController).GetTypeInfo(),
            MethodInfo = methodInfo,
            Parameters = parameters,
            ActionConstraints = [new HttpMethodActionConstraint([httpMethod])]
        };
    }

    private static ControllerParameterDescriptor CreateControllerParameter(
        System.Reflection.ParameterInfo parameterInfo,
        BindingSource bindingSource)
    {
        return new ControllerParameterDescriptor {
            Name = parameterInfo.Name,
            ParameterType = parameterInfo.ParameterType,
            ParameterInfo = parameterInfo,
            BindingInfo = new BindingInfo { BindingSource = bindingSource }
        };
    }

    [ApiGroup("v2")]
    private class TestApiController : ControllerBase
    {
        [HttpPost]
        public IActionResult PostAction([FromServices] IServiceProvider serviceProvider, TestModel model)
        {
            return Ok();
        }

        [HttpGet]
        public IActionResult GetAction([FromServices] IServiceProvider serviceProvider, string searchTerm)
        {
            return Ok();
        }
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
