using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Grand.Web.Common.TagHelpers;

[HtmlTargetElement("script", Attributes = LocationAttributeName)]
[HtmlTargetElement("script", Attributes = SrcAttributeName)]
[HtmlTargetElement("script", Attributes = OrderAttributeName)]
public class ScriptTagHelper : TagHelper
{
    private const string LocationAttributeName = "asp-location";
    private const string SrcAttributeName = "asp-src";
    private const string OrderAttributeName = "asp-order";
    private const string AppendVersionAttributeName = "asp-append-version";
    private readonly IFileVersionProvider _fileVersionProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IResourceManager _resourceManager;

    public ScriptTagHelper(IResourceManager resourceManager, IHttpContextAccessor httpContextAccessor,
        IFileVersionProvider fileVersionProvider)
    {
        _resourceManager = resourceManager;
        _httpContextAccessor = httpContextAccessor;
        _fileVersionProvider = fileVersionProvider;
    }

    [HtmlAttributeName(LocationAttributeName)]
    public ScriptLocation Location { get; set; }

    [HtmlAttributeName(SrcAttributeName)] public string Src { get; set; }

    [HtmlAttributeName(OrderAttributeName)]
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Appends a content hash to the src so a changed script actually reaches
    ///     returning visitors. Without it a cached copy can go on running against
    ///     markup that has already moved on.
    /// </summary>
    /// <remarks>
    ///     Nullable to match the built-in ScriptTagHelper, which also binds this
    ///     attribute on &lt;script&gt; - a plain bool makes Razor fail to compile.
    /// </remarks>
    [HtmlAttributeName(AppendVersionAttributeName)]
    public bool? AppendVersion { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var isAjaxCall = _httpContextAccessor.HttpContext != null &&
                         _httpContextAccessor.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest";
        if (!isAjaxCall)
        {
            output.SuppressOutput();

            var childContent = await output.GetChildContentAsync();

            var builder = new TagBuilder("script");
            builder.InnerHtml.AppendHtml(childContent);
            builder.TagRenderMode = TagRenderMode.Normal;
            if (!string.IsNullOrEmpty(Src))
            {
                var src = Src;
                if (AppendVersion == true && _httpContextAccessor.HttpContext != null)
                    src = _fileVersionProvider.AddFileVersionToPath(
                        _httpContextAccessor.HttpContext.Request.PathBase, src);
                builder.Attributes.Add("src", src);
            }
            foreach (var attribute in output.Attributes)
                builder.Attributes.Add(attribute.Name, attribute.Value.ToString());

            switch (Location)
            {
                case ScriptLocation.Head:
                    _resourceManager.RegisterHeadScript(builder, DisplayOrder);
                    break;

                case ScriptLocation.Header:
                    _resourceManager.RegisterHeaderScript(builder, DisplayOrder);
                    break;

                case ScriptLocation.Footer:
                    _resourceManager.RegisterFootScript(builder, DisplayOrder);
                    break;
            }
        }
    }
}