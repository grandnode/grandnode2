using FluentValidation;
using Grand.Infrastructure.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Validators;

[TestClass]
public class ValidationFilterTests
{
    [TestMethod]
    public async Task OnActionExecutionAsyncTest_ValidModel()
    {
        //arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<SourceTest>, SourceTestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var actionFilter = new ValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext {
            Request = {
                Method = "POST"
            }
        };

        var actionContext = new ActionContext(httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var actionExecutedContext = new ActionExecutedContext(actionContext,
            new List<IFilterMetadata>(),
            null);

        var actionExecutingContext = new ActionExecutingContext(actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            null);

        var source = new SourceTest {
            FirstName = "ABCD",
            LastName = "ABCD"
        };
        actionExecutingContext.ActionArguments["model"] = source;

        //act
        var next = new ActionExecutionDelegate(() =>
            Task.FromResult(CreateActionExecutedContext(actionExecutingContext)));
        await actionFilter.OnActionExecutionAsync(actionExecutingContext, next);

        //assert
        Assert.IsTrue(actionExecutingContext.ModelState.IsValid);
    }

    [TestMethod]
    public async Task OnActionExecutionAsyncTest_NotValidModel()
    {
        //arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<SourceTest>, SourceTestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var actionFilter = new ValidationFilter(serviceProvider);

        var httpContext = new DefaultHttpContext {
            Request = {
                Method = "POST"
            }
        };

        var actionContext = new ActionContext(httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var actionExecutedContext = new ActionExecutedContext(actionContext,
            new List<IFilterMetadata>(),
            null);

        var actionExecutingContext = new ActionExecutingContext(actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            null);

        var source = new SourceTest {
            FirstName = "ABCD"
        };
        actionExecutingContext.ActionArguments["model"] = source;

        //act
        var next = new ActionExecutionDelegate(() =>
            Task.FromResult(CreateActionExecutedContext(actionExecutingContext)));
        await actionFilter.OnActionExecutionAsync(actionExecutingContext, next);

        //assert
        Assert.IsFalse(actionExecutingContext.ModelState.IsValid);
    }


    private static ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext context)
    {
        return new ActionExecutedContext(context, context.Filters, context.Controller) {
            Result = context.Result
        };
    }
}