using Grand.Business.Core.Interfaces.Common.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.ViewRender;

/// <summary>
///     Allow to get Razor page content as string
/// </summary>
public class ViewRenderService : IViewRenderService
{
    private readonly ILogger<ViewRenderService> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITempDataProvider _tempDataProvider;


    public ViewRenderService(IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        ILogger<ViewRenderService> logger)
    {
        _razorViewEngine = razorViewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }


    public async Task<string> RenderToStringAsync<TModel>(string viewPath, TModel model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();
        var viewResult = _razorViewEngine.GetView(viewPath, viewPath, false);

        ArgumentNullException.ThrowIfNull(viewResult.View);

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        try
        {
            await viewResult.View.RenderAsync(viewContext);
        }
        catch (Exception ex)
        {
            var partialHtmlLength = sw.GetStringBuilder().Length;
            _logger.LogError(ex, "Error rendering view '{ViewPath}'. Partial HTML length: {PartialHtmlLength}",
                viewPath, partialHtmlLength);
            throw;
        }

        return sw.ToString();
    }
}