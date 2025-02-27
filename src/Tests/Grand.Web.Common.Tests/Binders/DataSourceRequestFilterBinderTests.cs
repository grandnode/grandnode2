using Grand.Web.Common.Binders;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace Grand.Web.Common.Tests.Binders;

[TestClass]
public class DataSourceRequestFilterBinderTests
{
    [TestMethod]
    public async Task BindModelAsync_BindsCorrectValues()
    {
        // Arrange
        var queryParams = new Dictionary<string, StringValues>
        {
            { "filter[logic]", "and" },
            { "filter[filters][0][field]", "Name" },
            { "filter[filters][0][operator]", "startswith" },
            { "filter[filters][0][value]", "hello" },
            { "filter[filters][0][ignoreCase]", "true" }
        };

        var queryCollection = new QueryCollection(queryParams);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = queryCollection;

        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var metadata = modelMetadataProvider.GetMetadataForType(typeof(DataSourceRequestFilter));

        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            queryCollection,
            CultureInfo.InvariantCulture);

        var bindingContext = new DefaultModelBindingContext {
            ModelMetadata = metadata,
            ModelName = "filter",
            ValueProvider = valueProvider,
            ActionContext = new Microsoft.AspNetCore.Mvc.ActionContext {
                HttpContext = httpContext
            }
        };

        var binder = new DataSourceRequestFilterBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.IsTrue(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as DataSourceRequestFilter;
        Assert.IsNotNull(result);
        Assert.AreEqual("and", result.Logic);
        Assert.IsNotNull(result.Filters);
        Assert.AreEqual(1, result.Filters.Count);

        var filter = result.Filters.First();
        Assert.AreEqual("Name", filter.Field);
        Assert.AreEqual("startswith", filter.Operator);
        Assert.AreEqual("hello", filter.Value);
        Assert.IsTrue(filter.IgnoreCase);
    }
}
