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
using System.Threading;

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

    [TestMethod]
    public void OnProvidersExecuting_PostAction_WithComplexAndSimpleParameter_UsesBodyAndQuerySources()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostActionWithReturnUrl))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], null),
            CreateControllerParameter(methodInfo.GetParameters()[1], null)
        ]);

        var context = new ApiDescriptionProviderContext([action]);

        provider.OnProvidersExecuting(context);

        Assert.AreEqual(1, context.Results.Count);
        var apiDescription = context.Results[0];
        Assert.AreEqual(2, apiDescription.ParameterDescriptions.Count);

        var modelParameter = apiDescription.ParameterDescriptions.Single(x => x.Name == "model");
        Assert.AreEqual(BindingSource.Body, modelParameter.Source);

        var returnUrlParameter = apiDescription.ParameterDescriptions.Single(x => x.Name == "returnUrl");
        Assert.AreEqual(BindingSource.Query, returnUrlParameter.Source);
    }

    [TestMethod]
    public void OnProvidersExecuting_PostAction_IgnoresFromServicesAttribute_WhenBindingSourceIsMissing()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostAction))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], null),
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
    public void OnProvidersExecuting_PostAction_WithModelBindingSource_UsesBodyAndQuerySources()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostActionWithReturnUrl))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], BindingSource.ModelBinding),
            CreateControllerParameter(methodInfo.GetParameters()[1], BindingSource.ModelBinding)
        ]);

        var context = new ApiDescriptionProviderContext([action]);

        provider.OnProvidersExecuting(context);

        Assert.AreEqual(1, context.Results.Count);
        var apiDescription = context.Results[0];
        Assert.AreEqual(2, apiDescription.ParameterDescriptions.Count);

        var modelParameter = apiDescription.ParameterDescriptions.Single(x => x.Name == "model");
        Assert.AreEqual(BindingSource.Body, modelParameter.Source);

        var returnUrlParameter = apiDescription.ParameterDescriptions.Single(x => x.Name == "returnUrl");
        Assert.AreEqual(BindingSource.Query, returnUrlParameter.Source);
    }

    [TestMethod]
    public void OnProvidersExecuting_PostAction_IgnoresSpecialBindingSourceParameter()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostActionWithCancellation))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], BindingSource.Special),
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
    public void OnProvidersExecuting_PostAction_IgnoresCancellationToken_WhenBindingSourceIsMissing()
    {
        var provider = CreateProvider();
        var methodInfo = typeof(TestApiController).GetMethod(nameof(TestApiController.PostActionWithCancellation))!;

        var action = CreateActionDescriptor(methodInfo, "POST", [
            CreateControllerParameter(methodInfo.GetParameters()[0], null),
            CreateControllerParameter(methodInfo.GetParameters()[1], null)
        ]);

        var context = new ApiDescriptionProviderContext([action]);

        provider.OnProvidersExecuting(context);

        Assert.AreEqual(1, context.Results.Count);
        var apiDescription = context.Results[0];
        Assert.AreEqual(1, apiDescription.ParameterDescriptions.Count);
        Assert.AreEqual("model", apiDescription.ParameterDescriptions[0].Name);
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
            BindingInfo = bindingSource == null ? null : new BindingInfo { BindingSource = bindingSource }
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

        [HttpPost]
        public IActionResult PostActionWithReturnUrl(TestModel model, string returnUrl)
        {
            return Ok();
        }

        [HttpPost]
        public IActionResult PostActionWithCancellation(CancellationToken cancellationToken, TestModel model)
        {
            return Ok();
        }
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
